using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Creates the assignment scene with persistent, Inspector-visible wiring.</summary>
public static class MonsterSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Monster.unity";

    [MenuItem("Monster Simulator/Build Scene (replaces Monster scene)")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Animations");
        Slice("Assets/Action-Buttons.png", new[] {
            Cut("PlayIcon", 70, 80, 600, 520, 1200),
            Cut("SleepIcon", 695, 80, 600, 520, 1200),
            Cut("StudyIcon", 70, 625, 600, 525, 1200),
            Cut("EatIcon", 695, 625, 600, 525, 1200)
        });
        var frames = new SpriteMetaData[21];
        // Fixed-sized canvases and bottom pivots keep the feet aligned across frames.
        int[] starts = { 30, 280, 530, 830, 1080, 1330, 1610, 1860, 2110 };
        for (int i = 0; i < 9; i++)
            frames[i] = Cut((i < 3 ? "Idle_" : i < 6 ? "Happy_" : "Sad_") + i % 3,
                starts[i], 50, 250, 300, 1309);
        string[] actions = { "Eating", "Playing", "Studying", "Sleeping" };
        for (int group = 0; group < actions.Length; group++)
            for (int frame = 0; frame < 3; frame++)
            {
                int column = group == 0 ? frame + 3 : group == 1 ? frame : group == 2 ? frame + 3 : frame + 6;
                frames[9 + group * 3 + frame] = Cut(actions[group] + "_" + frame,
                    starts[column], group == 0 ? 500 : 955, 250, group == 0 ? 300 : 340, 1309);
            }
        Slice("Assets/Animal.png", frames);
        Slice("Assets/Frame-Bar.png", new[] {
            Cut("StaminaFrame", 25, 35, 1060, 325, 724),
            Cut("HappinessFrame", 1110, 365, 1040, 310, 724),
            Cut("LearningFrame", 1110, 35, 1040, 325, 724),
            Cut("FullnessFrame", 25, 365, 1060, 310, 724),
            Cut("MeterFill", 600, 260, 16, 16, 724)
        });
        Slice("Assets/Background.png", null);
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera));
        camera.tag = "MainCamera";
        camera.GetComponent<Camera>().orthographic = true;
        camera.transform.position = new Vector3(0, 0, -10);
        var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 2400);
        scaler.matchWidthOrHeight = 0.5f;
        var background = Picture("Background", canvas.transform, SpriteAt("Assets/Background.png"), Vector2.zero, scaler.referenceResolution);
        background.preserveAspect = false;
        var bgRect = background.rectTransform;
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        Label("Title", canvas.transform, "MONSTER SIMULATOR", new Vector2(0, 830), new Vector2(950, 100), 54);
        Image staminaFill, happinessFill;
        Text staminaLabel, happinessLabel;
        Meter(canvas.transform, "Stamina", "StaminaFrame", new Vector2(0, 650), new Color(0.45f, 0.82f, 0.22f), out staminaFill, out staminaLabel);
        Meter(canvas.transform, "Happiness", "HappinessFrame", new Vector2(0, 440), new Color(1f, 0.4f, 0.55f), out happinessFill, out happinessLabel);
        var monster = Picture("Monster", canvas.transform, SpriteAt("Assets/Animal.png", "Idle_0"), new Vector2(0, 10), new Vector2(430, 516));
        var animator = monster.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = MakeAnimator();
        var manager = new GameObject("MonsterManager").AddComponent<MonsterController>();
        var play = ActionButton(canvas.transform, "Play", "PlayIcon", new Vector2(-245, -420), manager.Play);
        var study = ActionButton(canvas.transform, "Study", "StudyIcon", new Vector2(245, -420), manager.Study);
        var sleep = ActionButton(canvas.transform, "Sleep", "SleepIcon", new Vector2(-245, -770), manager.Sleep);
        ActionButton(canvas.transform, "Eat", "EatIcon", new Vector2(245, -770), manager.Eat);
        var serialized = new SerializedObject(manager);
        Wire(serialized, "playButton", play); Wire(serialized, "studyButton", study);
        Wire(serialized, "sleepButton", sleep); Wire(serialized, "staminaFill", staminaFill);
        Wire(serialized, "happinessFill", happinessFill); Wire(serialized, "staminaLabel", staminaLabel);
        Wire(serialized, "happinessLabel", happinessLabel); Wire(serialized, "monsterAnimator", animator);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        PlayerSettings.companyName = "Student";
        PlayerSettings.productName = "Monster Simulator";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.defaultScreenWidth = 1080;
        PlayerSettings.defaultScreenHeight = 2400;
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        MonsterMobileLayout.Apply();
        Selection.activeGameObject = manager.gameObject;
        Debug.Log("MONSTER_SCENE_READY: " + ScenePath);
    }

    private static SpriteMetaData Cut(string name, int x, int top, int width, int height, int sheetHeight)
    {
        return new SpriteMetaData { name = name, rect = new Rect(x, sheetHeight - top - height, width, height),
            alignment = (int)SpriteAlignment.BottomCenter, pivot = new Vector2(0.5f, 0) };
    }

    private static void Slice(string path, SpriteMetaData[] slices)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = slices == null ? SpriteImportMode.Single : SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.mipmapEnabled = false;
        importer.isReadable = path == "Assets/Animal.png";
        if (slices != null)
        {
#pragma warning disable 0618
            importer.spritesheet = slices;
#pragma warning restore 0618
        }
        importer.SaveAndReimport();
    }

    private static Sprite SpriteAt(string path, string name = null)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First(s => name == null || s.name == name);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    private static Image Picture(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
    {
        var picture = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
        picture.sprite = sprite; picture.preserveAspect = true; picture.raycastTarget = false;
        return picture;
    }

    private static Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize)
    {
        var label = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize; label.text = value; label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.22f, 0.10f, 0.05f); label.raycastTarget = false;
        return label;
    }

    private static void Meter(Transform parent, string name, string sprite, Vector2 position, Color tint, out Image fill, out Text label)
    {
        var root = Rect(name + "Meter", parent, position, new Vector2(900, 200));
        Picture("Frame", root, SpriteAt("Assets/Frame-Bar.png", sprite), Vector2.zero, new Vector2(900, 275));
        fill = Picture("Fill", root, SpriteAt("Assets/Frame-Bar.png", "MeterFill"), new Vector2(220, -33), new Vector2(400, 46));
        fill.preserveAspect = false; fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.color = tint;
        label = Label("Label", root, name, new Vector2(155, -102), new Vector2(600, 60), 36);
    }

    private static Button ActionButton(Transform parent, string name, string sprite, Vector2 position, UnityAction action)
    {
        var image = Picture(name + "Button", parent, SpriteAt("Assets/Action-Buttons.png", sprite), position, new Vector2(450, 340));
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors; colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.65f); button.colors = colors;
        UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }

    private static RuntimeAnimatorController MakeAnimator()
    {
        const string path = "Assets/Animations/Monster.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        foreach (var state in machine.states) machine.RemoveState(state.state);
        foreach (string mood in new[] { "Idle", "Happy", "Sad", "Eating", "Playing", "Studying", "Sleeping" })
        {
            string clipPath = "Assets/Animations/" + mood + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, clipPath); }
            clip.frameRate = 6;
            var keys = new ObjectReferenceKeyframe[4];
            for (int i = 0; i < 4; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / 6f, value = SpriteAt("Assets/Animal.png", mood + "_" + i % 3) };
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(Image), "m_Sprite"), keys);
            // Normalize visible head width, not the full canvas (which includes props).
            // Each frame also shares a floor and head center, avoiding pose-size jumps.
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale.y"), null);
            var widths = new AnimationCurve(); var heights = new AnimationCurve();
            var positionsX = new AnimationCurve(); var positionsY = new AnimationCurve();
            for (int i = 0; i < keys.Length; i++)
            {
                var sprite = (Sprite)keys[i].value;
                Vector4 normalized = NormalizedFrame(sprite);
                widths.AddKey(keys[i].time, normalized.x); heights.AddKey(keys[i].time, normalized.y);
                positionsX.AddKey(keys[i].time, normalized.z); positionsY.AddKey(keys[i].time, normalized.w);
            }
            SetSteppedCurve(clip, "m_SizeDelta.x", widths); SetSteppedCurve(clip, "m_SizeDelta.y", heights);
            SetSteppedCurve(clip, "m_AnchoredPosition.x", positionsX); SetSteppedCurve(clip, "m_AnchoredPosition.y", positionsY);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            var state = machine.AddState(mood); state.motion = clip;
            if (mood == "Idle") machine.defaultState = state;
        }
        return controller;
    }

    /// <summary>Frame size and center offset at a shared 300-unit head width and floor.</summary>
    public static Vector4 NormalizedFrame(Sprite sprite)
    {
        Rect rect = sprite.rect;
        var pixels = sprite.texture.GetPixels32();
        int textureWidth = sprite.texture.width;
        int width = (int)rect.width, height = (int)rect.height;
        int longest = 0; float headCenter = width * 0.5f; int bottom = height;
        for (int y = 0; y < height; y++)
        {
            int run = 0;
            for (int x = 0; x < width; x++)
            {
                bool opaque = pixels[((int)rect.y + y) * textureWidth + (int)rect.x + x].a > 100;
                if (opaque) bottom = Mathf.Min(bottom, y);
                run = opaque ? run + 1 : 0;
                // Only upper head rows: exclude the ball, desk and feet.
                if (y > height * 0.35f && y < height * 0.8f && run > longest)
                { longest = run; headCenter = x - (run - 1) * 0.5f; }
            }
        }
        float scale = 300f / Mathf.Max(longest, 1);
        float frameWidth = width * scale, frameHeight = height * scale;
        return new Vector4(frameWidth, frameHeight, (width * 0.5f - headCenter) * scale,
            -320f + frameHeight * 0.5f - bottom * scale);
    }

    private static void SetSteppedCurve(AnimationClip clip, string property, AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(RectTransform), property), curve);
    }

    // Run through Unity's CLI to verify the saved scene and gameplay boundaries.
    public static void Validate()
    {
        Selection.activeObject = null;
        EditorSceneManager.OpenScene(ScenePath);
        var manager = UnityEngine.Object.FindFirstObjectByType<MonsterController>();
        if (manager == null) throw new Exception("Scene controller missing");
        var saved = new SerializedObject(manager);
        foreach (string field in new[] { "playButton", "studyButton", "sleepButton", "staminaFill", "happinessFill", "staminaLabel", "happinessLabel", "monsterAnimator", "eatButton", "fullnessFill", "learningFill", "fullnessLabel", "learningLabel", "careMessage", "guessingChoices", "leftChoiceLabel", "rightChoiceLabel", "roundTimerLabel" })
            if (saved.FindProperty(field).objectReferenceValue == null) throw new Exception("Missing reference: " + field);
        foreach (string action in new[] { "Play", "Study", "Sleep", "Eat" })
        {
            var button = GameObject.Find(action + "Button").GetComponent<Button>();
            if (button.onClick.GetPersistentEventCount() != 1 || button.onClick.GetPersistentTarget(0) != manager || button.onClick.GetPersistentMethodName(0) != action)
                throw new Exception("Incorrect listener: " + action);
        }
        foreach (string animation in new[] { "Idle", "Happy", "Sad", "Eating", "Playing", "Studying", "Sleeping" })
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/" + animation + ".anim");
            if (clip == null) throw new Exception("Missing animation: " + animation);
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).First();
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keys.Length < 3 || keys.Select(k => k.value).Distinct().Count() < 3)
                throw new Exception("Animation must contain three distinct sprites: " + animation);
        }
        if (GameObject.Find("Title") != null) throw new Exception("Old title was not removed");
        var testObject = new GameObject("ValidationOnly");
        var pet = testObject.AddComponent<MonsterController>();
        var values = new SerializedObject(pet);
        Action<float, float> set = (energy, mood) => {
            typeof(MonsterController).GetField("actionTimeLeft", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(pet, 0f);
            typeof(MonsterController).GetField("waitingForGuess", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(pet, false);
            typeof(MonsterController).GetField("waitingForStudyAnswer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(pet, false);
            typeof(MonsterController).GetField("careTickSeconds", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(pet, 0f);
            values.Update(); values.FindProperty("stamina").floatValue = energy;
            values.FindProperty("happiness").floatValue = mood; values.ApplyModifiedPropertiesWithoutUndo();
        };
        Action<float, float> expect = (energy, mood) => {
            if (!Mathf.Approximately(pet.Stamina, energy) || !Mathf.Approximately(pet.Happiness, mood))
                throw new Exception($"Unexpected stats: {pet.Stamina}, {pet.Happiness}; expected {energy}, {mood}");
        };
        set(20, 75); pet.Sleep(); expect(20, 75); pet.Play(); expect(18, 75.5f);
        pet.Play(); pet.Study(); expect(18, 75.5f);
        set(18, 75.5f); pet.Sleep(); expect(100, 75.5f);
        set(20, 75); pet.Study(); expect(18, 74.5f);
        set(19.99f, 75); pet.Play(); pet.Study(); expect(19.99f, 75);
        set(95, 98); pet.Eat(); expect(100, 100);
        set(100, 0); pet.Study(); expect(98, 0);
        // Added care mechanics: food, learning, cooldown and time-based decay.
        set(100, 75); float beforeLearning = pet.Learning; pet.Study();
        int studyAnswer = (int)typeof(MonsterController).GetField("correctStudyChoice", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(pet);
        if (studyAnswer == 0) pet.ChooseLeft(); else pet.ChooseRight();
        if (!Mathf.Approximately(pet.Learning, beforeLearning + 10)) throw new Exception("Study progress failed");
        pet.Eat(); expect(98, 74.5f); // Busy: cannot spam another action.
        set(100, 75);
        typeof(MonsterController).GetMethod("Tick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(pet, new object[] { 10f });
        expect(100, 74.9f);
        set(100, 75);
        typeof(MonsterController).GetMethod("Tick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(pet, new object[] { 60f });
        expect(99, 74.4f);
        set(50, 75); float food = pet.Fullness; pet.Eat();
        if (!Mathf.Approximately(pet.Fullness, Mathf.Min(food + 25, 100))) throw new Exception("Feeding failed");
        set(100, 100);
        if (pet.CurrentAnimationState != "Happy") throw new Exception("Happy threshold failed");
        set(100, 50);
        if (pet.CurrentAnimationState != "Sad") throw new Exception("Sad threshold failed");
        set(100, 50.01f);
        if (pet.CurrentAnimationState != "Idle") throw new Exception("Idle threshold failed");
        set(100, 75); pet.Play();
        if (pet.CurrentAnimationState != "Playing") throw new Exception("Action animation failed");
        int answer = (int)typeof(MonsterController).GetField("hiddenDirection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(pet);
        if (answer == 0) pet.ChooseLeft(); else pet.ChooseRight();
        int wins = (int)typeof(MonsterController).GetField("gamesWon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(pet);
        pet.ChooseLeft(); pet.ChooseRight();
        if ((int)typeof(MonsterController).GetField("gamesWon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(pet) != wins || wins != 1)
            throw new Exception("Guess rewards must resolve exactly once");
        var label = GameObject.Find("Canvas").transform.Find("PhoneViewport/StaminaMeter/Label").GetComponent<Text>();
        if (label.text.Contains(".")) throw new Exception("Decimal meter label");
        if (GameObject.Find("MessagePanel") == null) throw new Exception("Missing framed message panel");
        UnityEngine.Object.DestroyImmediate(testObject);
        // Reopen so test objects and dirty flags are not persisted.
        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("MONSTER_VALIDATION_PASSED: 4 meters, 4 listeners, 7 sprite clips, mood thresholds, cooldown, slow care ticks, guessing round, whole-number UI, action deltas, and clamps");
    }

    private static void Wire(SerializedObject obj, string name, UnityEngine.Object value)
    {
        obj.FindProperty(name).objectReferenceValue = value;
    }
}
