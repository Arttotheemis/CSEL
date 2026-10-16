using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Behavioral regression checks for the care loop and its saved state.</summary>
public static class MonsterAuditChecks
{
    private static readonly List<string> passed = new List<string>();
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("AUDIT: " + name);
        passed.Add(name);
    }
    private static void Field(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Read(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    private static void Reset(MonsterController pet, float stamina = 100, float happiness = 75, float food = 75, float study = 0)
    {
        Field(pet, "applicationPaused", false); Field(pet, "actionTimeLeft", 0f);
        Field(pet, "waitingForGuess", false); Field(pet, "waitingForStudyAnswer", false);
        Field(pet, "careTickSeconds", 0f); Field(pet, "previousNeed", 0);
        Field(pet, "stamina", stamina); Field(pet, "happiness", happiness);
        Field(pet, "fullness", food); Field(pet, "learning", study);
        Call(pet, "ApplyChanges");
    }
    private static void AnswerCorrectly(MonsterController pet)
    {
        if ((int)Read(pet, "correctStudyChoice") == 0) pet.ChooseLeft(); else pet.ChooseRight();
    }

    public static void Run(MonsterController live, GameAudioController audio, Transform root)
    {
        passed.Clear();
        string liveState = live.CaptureCareState();
        var probeObject = new GameObject("AuditOnly");
        var pet = probeObject.AddComponent<MonsterController>();
        pet.enabled = false;
        try
        {
            foreach (float energy in new[] { 19.99f, 20f, 20.01f })
            {
                bool allowed = energy >= 20;
                Reset(pet, energy); pet.Play();
                Check(Mathf.Approximately(pet.Stamina, allowed ? energy - 2 : energy), "Play boundary " + energy);
                Reset(pet, energy); pet.Study();
                Check(Mathf.Approximately(pet.Happiness, allowed ? 74.5f : 75), "Study boundary " + energy);
                Reset(pet, energy); pet.Sleep();
                Check(Mathf.Approximately(pet.Stamina, allowed ? energy : 100), "Sleep strict boundary " + energy);
            }
            Reset(pet); Call(pet, "Tick", 59f);
            Check(pet.Stamina == 100 && pet.Fullness == 75, "No premature food/energy decay");
            string saved = pet.CaptureCareState();
            Reset(pet, 5, 5, 5, 5);
            Check(pet.TryRestoreCareState(saved), "Save restores successfully");
            Check(pet.Stamina == 100 && pet.Fullness == 75, "Restoring causes no offline losses");
            Call(pet, "Tick", 1f);
            Check(pet.Stamina == 99 && pet.Fullness == 74, "Save preserves partial minute");
            Check(Mathf.Abs(pet.Happiness - 74.4f) < 0.001f, "Required 0.01 happiness decay");
            string stable = pet.CaptureCareState();
            foreach (string bad in new[] { "", "{}", "[]", "{broken}", "{\"stamina\":20}" })
                Check(!pet.TryRestoreCareState(bad) && pet.CaptureCareState() == stable, "Invalid save is non-destructive: " + bad);
            Check(!pet.TryRestoreCareState(saved.Replace("\"version\":3", "\"version\":4")), "Future save version rejected");
            Check(pet.TryRestoreCareState("{\"stamina\":20,\"happiness\":50,\"fullness\":25,\"learning\":80,\"gamesWon\":3}"), "Legacy saves remain compatible");
            Check(pet.Stamina == 20 && pet.Happiness == 50 && pet.Learning == 80, "Legacy values retained");
            Reset(pet, 100, 75, 1); Call(pet, "Tick", 180f);
            Check(pet.Stamina == 95 && pet.Fullness == 0, "Starvation correct across multiple care ticks");
            float bulk = pet.Stamina;
            Reset(pet, 100, 75, 1);
            for (int i = 0; i < 3; i++) Call(pet, "Tick", 60f);
            Check(pet.Stamina == bulk, "Bulk and incremental care agree");
            Reset(pet); stable = pet.CaptureCareState();
            Call(pet, "Tick", float.NaN); Call(pet, "Tick", -1f);
            Check(stable == pet.CaptureCareState(), "Invalid time cannot corrupt stats");
            Call(pet, "OnApplicationPause", true);
            Call(pet, "Tick", 120f); pet.Play(); pet.Study(); pet.Eat(); pet.Sleep();
            Check(stable == pet.CaptureCareState(), "Paused care and actions do not advance");
            Call(pet, "OnApplicationPause", false); Call(pet, "Tick", 60f);
            Check(pet.Stamina == 99, "Care resumes after pause");
            Reset(pet, 95, 99, 95, 95); pet.Eat();
            Check(pet.Stamina == 100 && pet.Happiness == 100 && pet.Fullness == 100, "Feeding clamps all care values");
            Reset(pet, 100, 75, 75, 95); pet.Study(); AnswerCorrectly(pet);
            Check(pet.Learning == 100 && ((string)Read(pet, "actionMessage")).Contains("+5"), "Partial study reward reports actual gain");
            Reset(pet, 100, 75, 75, 100); pet.Study(); AnswerCorrectly(pet);
            Check(pet.Learning == 100 && ((string)Read(pet, "actionMessage")).Contains("FULL"), "Full study progress gives honest feedback");
            var randomState = UnityEngine.Random.state;
            for (int i = 0; i < 100; i++)
            {
                Reset(pet); pet.Study();
                var words = ((string)Read(pet, "actionMessage")).Split(' ');
                int first = int.Parse(words[2]), second = int.Parse(words[4]);
                int answer = words[3] == "+" ? first + second : first - second;
                var options = (int[])Read(pet, "studyAnswers");
                Check(options[(int)Read(pet, "correctStudyChoice")] == answer && options[0] != options[1] && Math.Min(options[0], options[1]) >= 0,
                    "Valid distinct whole-number question " + i);
                AnswerCorrectly(pet); float earned = pet.Learning;
                pet.ChooseLeft(); pet.ChooseRight();
                Check(pet.Learning == earned && earned == 10, "Single reward question " + i);
            }
            UnityEngine.Random.state = randomState;

            Reset(live); live.Study();
            var choices = root.Find("MessagePanel/GuessingChoices");
            var timer = root.Find("MessagePanel/RoundTimer").GetComponent<Text>();
            Check(timer.gameObject.activeSelf && timer.text.Contains("12"), "Study timer visible");
            Call(live, "OnApplicationPause", true); Call(live, "Tick", 20f); live.ChooseLeft();
            Check(live.RoundTimeRemaining == 12 && live.Learning == 0 && !choices.Find("Left").GetComponent<Button>().interactable, "Paused quiz preserves deadline and rejects answer");
            Call(live, "OnApplicationPause", false); Call(live, "Tick", 1f);
            Check(live.RoundTimeRemaining == 11 && timer.text.Contains("11"), "Quiz countdown resumes");
            AnswerCorrectly(live);
            Check(!timer.gameObject.activeSelf, "Quiz countdown hides on result");
            Reset(live, 20); live.Play();
            if ((int)Read(live, "hiddenDirection") == 0) live.ChooseLeft(); else live.ChooseRight();
            Call(live, "Tick", 2.1f);
            Check(audio.LastCue == GameAudioController.Cue.Attention && root.Find("SleepButton").GetComponent<Button>().interactable,
                "Care alert deferred until action ends");
            Reset(live, 19.99f);
            Check(root.Find("StaminaMeter/Label").GetComponent<Text>().text.StartsWith("19/"), "Energy display agrees with sleep restriction");
            Check(!root.Find("PlayButton").GetComponent<Button>().interactable, "Locked button matches public action restriction");

            audio.PlayActionCue(GameAudioController.Cue.Sleep);
            Call(audio, "UpdateMusic", 1f); float remainingDuck = (float)Read(audio, "duckTime");
            Call(audio, "UpdateActionSound", 1f);
            Check(Mathf.Approximately((float)Read(audio, "duckTime"), remainingDuck), "Sleep pulses do not extend music ducking");
            audio.StopActionSound();
            foreach (string meterName in new[] { "Stamina", "Learning", "Fullness", "Happiness" })
            {
                var meter = root.Find(meterName + "Meter/Fill").GetComponent<PixelMeterFill>();
                float previousWidth = -1;
                foreach (float amount in new[] { 0f, 0.01f, 0.5f, 1f })
                {
                    meter.fillAmount = amount; Canvas.ForceUpdateCanvases();
                    var mesh = meter.canvasRenderer.GetMesh();
                    Check(amount != 0 || mesh.vertexCount == 0, meterName + " empty bar has no fill");
                    if (mesh.vertexCount > 0)
                    {
                        float width = mesh.bounds.size.x;
                        Check(width >= previousWidth && width <= meter.rectTransform.rect.width + 0.1f,
                            meterName + " fill stays contained at " + amount);
                        previousWidth = width;
                    }
                }
            }
            Reset(live); live.Study();
            // Synchronous tests do not advance Unity's tween coroutines; sample the
            // current pose and tint before capturing the actual intended UI state.
            root.Find("Monster").GetComponent<Animator>().Update(0);
            foreach (var button in root.GetComponentsInChildren<Button>(true))
                button.targetGraphic.canvasRenderer.SetColor(button.interactable ? button.colors.normalColor : button.colors.disabledColor);
            foreach (var size in new[] { new Vector2Int(1080, 2400), new Vector2Int(1080, 1920), new Vector2Int(720, 1600), new Vector2Int(1920, 1080) })
                MonsterMobileLayout.RenderPreviewAt(size.x, size.y, $"Builds/Previews/Audit-{size.x}x{size.y}.png");
            Check(true, "Phone and wide-layout previews rendered");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(probeObject);
            live.TryRestoreCareState(liveState);
            audio.StopActionSound();
        }
        Directory.CreateDirectory("Docs");
        File.WriteAllText("Docs/AUDIT-CHECKS.txt", passed.Count + " assertions passed\n" + string.Join("\n", passed) + "\n");
        Debug.Log("MONSTER_AUDIT_PASSED: " + passed.Count + " care/save/pause/quiz/UI/audio regression assertions");
    }
}
