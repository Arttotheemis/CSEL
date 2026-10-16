using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Runs the real scene in Play Mode, including animation sampling and UI events.</summary>
[InitializeOnLoad]
public static class MonsterPlayModeChecks
{
    private const string Phase = "Monster.Checks.Phase";
    private static double enteredAt;
    private static readonly List<string> runtimeErrors = new List<string>();
    static MonsterPlayModeChecks()
    {
        EditorApplication.update += Poll;
        Application.logMessageReceived += (message, stack, type) => {
            string phase = SessionState.GetString(Phase, "");
            if ((phase == "start" || phase == "running") && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                runtimeErrors.Add(message);
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Monster.unity");
        SessionState.SetString(Phase, "start");
        EditorApplication.isPlaying = true;
    }
    private static void Poll()
    {
        string phase = SessionState.GetString(Phase, "");
        if (phase == "exit")
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetString(Phase, "");
                EditorApplication.Exit(SessionState.GetInt("Monster.Checks.Exit", 0));
            }
            return;
        }
        if (phase != "start" || !EditorApplication.isPlaying) return;
        if (enteredAt == 0) { enteredAt = EditorApplication.timeSinceStartup; return; }
        if (EditorApplication.timeSinceStartup - enteredAt < 1) return;
        SessionState.SetString(Phase, "running");
        int exit = 0;
        try { Check(); if (runtimeErrors.Count > 0) throw new Exception("Unexpected runtime log errors: " + string.Join("; ", runtimeErrors)); Debug.Log("MONSTER_PLAYMODE_PASSED: play/study choices, correct/wrong/timeout study outcomes, seven animated poses, slow ticks, 4 pixel action assets, supplied error MP3, verified live clip names, increased background music, no audio header controls, action sound pulses and music/crossfade"); }
        catch (Exception exception) { Debug.LogException(exception); exit = 1; }
        SessionState.SetInt("Monster.Checks.Exit", exit);
        SessionState.SetString(Phase, "exit");
        EditorApplication.isPlaying = false;
    }
    private static void Check()
    {
        var pet = UnityEngine.Object.FindFirstObjectByType<MonsterController>();
        pet.enabled = false; // Deterministic assertions while Animator and scene are running.
        var animator = UnityEngine.Object.FindFirstObjectByType<Animator>();
        var root = GameObject.Find("PhoneViewport").transform;
        var audio = UnityEngine.Object.FindFirstObjectByType<GameAudioController>();
        if (audio == null || UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length != 1)
            throw new Exception("Audio manager/listener wiring failed");
        var audioData = new SerializedObject(audio);
        var clips = audioData.FindProperty("effects");
        if (clips.arraySize != 9) throw new Exception("Expected nine sound effects");
        for (int i = 0; i < clips.arraySize; i++)
        {
            var clip = clips.GetArrayElementAtIndex(i).objectReferenceValue as AudioClip;
            if (clip == null || clip.length <= 0) throw new Exception("Missing/empty sound effect");
            var cue = (GameAudioController.Cue)i;
            bool replaced = cue == GameAudioController.Cue.Play || cue == GameAudioController.Cue.Study || cue == GameAudioController.Cue.Eat || cue == GameAudioController.Cue.Sleep || cue == GameAudioController.Cue.Wrong;
            if (replaced && clip.name != (cue == GameAudioController.Cue.Wrong ? "soundshelfstudio-ui-error-pop-515668" : "Pixel" + cue)) throw new Exception("Old clip still assigned: " + cue + " -> " + clip.name);
            var samples = new float[clip.samples * clip.channels];
            if (!clip.GetData(samples, 0) || !Array.Exists(samples, sample => Mathf.Abs(sample) > 0.001f))
                throw new Exception("Silent or unreadable sound effect: " + clip.name);
            if (replaced && cue != GameAudioController.Cue.Wrong)
            {
                double energy = 0; foreach (float sample in samples) energy += sample * sample;
                if (Math.Sqrt(energy / samples.Length) < 0.1) throw new Exception("Pixel clip is too quiet: " + clip.name);
            }
        }
        var musicA = (AudioSource)audioData.FindProperty("musicA").objectReferenceValue;
        var musicB = (AudioSource)audioData.FindProperty("musicB").objectReferenceValue;
        if (musicA.clip == null || musicA.clip.length < 30 || !musicA.isPlaying)
            throw new Exception("Background music did not start");
        if (root.Find("MusicVolume") != null || root.Find("EffectsVolume") != null)
            throw new Exception("Audio header controls should be removed");
        if (audio.MusicVolume < 0.45f || audio.EffectsVolume < 0.9f)
            throw new Exception("Updated mix defaults were not applied");
        Action<float, float> reset = (stamina, happiness) => {
            Field(pet, "actionTimeLeft", 0f); Field(pet, "waitingForGuess", false); Field(pet, "waitingForStudyAnswer", false); Field(pet, "careTickSeconds", 0f);
            Field(pet, "stamina", stamina); Field(pet, "happiness", happiness); Field(pet, "fullness", 75f);
            typeof(MonsterController).GetMethod("ApplyChanges", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(pet, null);
        };
        Action<string> pose = state => {
            for (int frame = 0; frame < 3; frame++)
            {
                animator.Update(1f / 6f);
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer." + state)) throw new Exception("Wrong animated state: " + state);
                var image = animator.GetComponent<Image>();
                Vector4 expected = MonsterSceneBuilder.NormalizedFrame(image.sprite);
                var rect = image.rectTransform;
                if (Mathf.Abs(rect.sizeDelta.x - expected.x) > 0.1f || Mathf.Abs(rect.sizeDelta.y - expected.y) > 0.1f ||
                    Mathf.Abs(rect.anchoredPosition.y - expected.w) > 0.1f)
                    throw new Exception("Sprite sizing/floor mismatch: " + state);
            }
        };
        reset(100, 75); pose("Idle");
        root.Find("PlayButton").GetComponent<Button>().onClick.Invoke(); pose("Playing");
        if (audio.LastCue != GameAudioController.Cue.Play || audio.LastClipName != "PixelPlay") throw new Exception("Play sound event failed");
        if (!Mathf.Approximately(pet.Happiness, 75.5f) || !Mathf.Approximately(pet.Stamina, 98)) throw new Exception("Play rules failed");
        var choices = root.Find("MessagePanel/GuessingChoices");
        if (!choices.gameObject.activeSelf) throw new Exception("Guessing choices not visible");
        int answer = (int)Get(pet, "hiddenDirection");
        choices.Find(answer == 0 ? "Left" : "Right").GetComponent<Button>().onClick.Invoke();
        if (audio.LastCue != GameAudioController.Cue.Correct) throw new Exception("Guess sound event failed");
        if (choices.gameObject.activeSelf || (int)Get(pet, "gamesWon") != 1) throw new Exception("Guessing UI result failed");
        if (animator.transform.localScale.x != (answer == 0 ? -1 : 1)) throw new Exception("Guess direction was not revealed visually");
        reset(100, 75); root.Find("StudyButton").GetComponent<Button>().onClick.Invoke(); pose("Studying");
        if (audio.LastCue != GameAudioController.Cue.Study || audio.LastClipName != "PixelStudy") throw new Exception("Study sound failed");
        if (!choices.gameObject.activeSelf) throw new Exception("Study answer buttons missing");
        if (!int.TryParse(choices.Find("Left/Text").GetComponent<Text>().text, out _) ||
            !int.TryParse(choices.Find("Right/Text").GetComponent<Text>().text, out _)) throw new Exception("Study must show number choices");
        string mainPreview = "Builds/Previews/Monster-1080x2400.png";
        byte[] originalPreview = File.ReadAllBytes(mainPreview);
        MonsterMobileLayout.RenderPreview();
        File.Copy(mainPreview, "Builds/Previews/Study-Quiz-1080x2400.png", true);
        File.WriteAllBytes(mainPreview, originalPreview);
        float learningBefore = pet.Learning;
        int studyAnswer = (int)Get(pet, "correctStudyChoice");
        choices.Find(studyAnswer == 0 ? "Left" : "Right").GetComponent<Button>().onClick.Invoke();
        if (pet.Learning != learningBefore + 10 || choices.gameObject.activeSelf || audio.LastCue != GameAudioController.Cue.Correct)
            throw new Exception("Correct study answer failed");
        pet.ChooseLeft(); pet.ChooseRight();
        if (pet.Learning != learningBefore + 10) throw new Exception("Study awarded twice");
        reset(100, 75); pet.Study(); learningBefore = pet.Learning;
        studyAnswer = (int)Get(pet, "correctStudyChoice");
        choices.Find(studyAnswer == 0 ? "Right" : "Left").GetComponent<Button>().onClick.Invoke();
        if (pet.Learning != learningBefore || audio.LastClipName != "soundshelfstudio-ui-error-pop-515668" || audio.LastCue != GameAudioController.Cue.Wrong || !Mathf.Approximately(pet.Happiness, 74.5f))
            throw new Exception("Wrong study answer changed the required stats or awarded progress");
        reset(100, 75); pet.Study();
        typeof(MonsterController).GetMethod("Tick", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(pet, new object[] { 13f });
        if ((bool)Get(pet, "waitingForStudyAnswer") || choices.gameObject.activeSelf || pet.Learning != learningBefore)
            throw new Exception("Study timeout failed");

        reset(50, 75); root.Find("EatButton").GetComponent<Button>().onClick.Invoke(); pose("Eating");
        if (audio.LastCue != GameAudioController.Cue.Eat || audio.LastClipName != "PixelEat") throw new Exception("Eat sound failed");
        reset(19, 75); root.Find("SleepButton").GetComponent<Button>().onClick.Invoke(); pose("Sleeping");
        if (audio.LastCue != GameAudioController.Cue.Sleep || audio.LastClipName != "PixelSleep") throw new Exception("Sleep sound failed");
        if (pet.Stamina != 100) throw new Exception("Sleep did not restore stamina");
        reset(100, 100); pose("Happy");
        reset(100, 50); pose("Sad");
        reset(100, 75);
        var tick = typeof(MonsterController).GetMethod("Tick", BindingFlags.NonPublic | BindingFlags.Instance);
        tick.Invoke(pet, new object[] { 10f });
        if (pet.Stamina != 100 || pet.Fullness != 75) throw new Exception("Care drained too early");
        tick.Invoke(pet, new object[] { 50f });
        if (pet.Stamina != 99 || pet.Fullness != 74) throw new Exception("Minute care tick failed");
        float savedMusic = audio.MusicVolume, savedEffects = audio.EffectsVolume;
        try
        {
            var musicTick = typeof(GameAudioController).GetMethod("UpdateMusic", BindingFlags.NonPublic | BindingFlags.Instance);
            audio.SetMusicVolume(0);
            musicTick.Invoke(audio, new object[] { 0.01f });
            if (musicA.volume != 0 || musicB.volume != 0) throw new Exception("Music mute failed");
            audio.SetEffectsVolume(0); int played = audio.EffectsPlayed;
            audio.PlayCue(GameAudioController.Cue.Select);
            if (audio.EffectsPlayed != played) throw new Exception("Effects mute failed");
            audio.SetMusicVolume(0.25f); audio.SetEffectsVolume(0.75f);
            audio.PlayActionCue(GameAudioController.Cue.Eat);
            int beforePulse = audio.EffectsPlayed;
            typeof(GameAudioController).GetMethod("UpdateActionSound", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(audio, new object[] { 0.4f });
            if (audio.EffectsPlayed != beforePulse + 1 || audio.LastCue != GameAudioController.Cue.Eat)
                throw new Exception("Eating sound did not follow the animation");
            audio.PlayCue(GameAudioController.Cue.Correct);
            if ((int)Get(audio, "actionPulsesLeft") != 0) throw new Exception("Result sound did not stop action pulses");
            musicA.time = musicA.clip.length - 0.5f;
            musicTick.Invoke(audio, new object[] { 0.01f });
            if (!musicB.isPlaying) throw new Exception("Music crossfade did not start");
            musicTick.Invoke(audio, new object[] { 1.6f });
            if (!musicB.isPlaying || musicA.isPlaying) throw new Exception("Music crossfade handoff failed");
        }
        finally { audio.SetMusicVolume(savedMusic); audio.SetEffectsVolume(savedEffects); }
        MonsterAuditChecks.Run(pet, audio, root);
        foreach (string meter in new[] { "Stamina", "Happiness", "Fullness", "Learning" })
            if (root.Find(meter + "Meter/Label").GetComponent<Text>().text.Contains(".")) throw new Exception("Decimal UI");
    }
    private static void Field(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static object Get(object target, string name) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
}
