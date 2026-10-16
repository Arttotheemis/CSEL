using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Updates the existing scene without replacing its gameplay connections.</summary>
[InitializeOnLoad]
public static class MonsterMobileLayout
{
    private const string Marker = "Library/MonsterMobileLayout-v10.done";
    static MonsterMobileLayout() { EditorApplication.delayCall += AutoApply; }

    private static void AutoApply()
    {
        if (Application.isBatchMode || File.Exists(Marker)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += AutoApply; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.isPlaying = false; EditorApplication.delayCall += AutoApply; return; }
        if (UnityEngine.Object.FindFirstObjectByType<MonsterController>() == null)
        {
            // During startup the scene can finish loading after InitializeOnLoad.
            if (EditorApplication.timeSinceStartup < 30) EditorApplication.delayCall += AutoApply;
            return;
        }
        Apply();
        RenderPreview();
        File.WriteAllText(Marker, "1080 x 2400 layout applied");
    }

    [MenuItem("Monster Simulator/Apply 1080 x 2400 Phone Layout")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before applying layout.");
        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) throw new InvalidOperationException("Open Assets/Scenes/Monster.unity first.");
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1080, 2400);
        scaler.matchWidthOrHeight = 0.5f;
        var backdrop = canvas.GetComponent<Image>() ?? canvas.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.075f, 0.045f, 0.035f); backdrop.raycastTarget = false;
        var root = canvas.transform.Find("PhoneViewport") as RectTransform;
        if (root == null)
        {
            root = new GameObject("PhoneViewport", typeof(RectTransform), typeof(RectMask2D), typeof(PhoneViewport)).GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            foreach (Transform child in canvas.transform.Cast<Transform>().ToArray())
                if (child != root) child.SetParent(root, false);
        }
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(1080, 2400);
        root.localScale = Vector3.one;
        root.anchoredPosition = Vector2.zero;
        var bg = root.Find("Background").GetComponent<Image>();
        Position(bg.rectTransform, Vector2.zero, new Vector2(1111, 2400));
        bg.preserveAspect = true;
        // Envelope the portrait viewport, preserving the source art's proportions.
        var fitter = bg.GetComponent<AspectRatioFitter>() ?? bg.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 853f / 1844f;
        var title = root.Find("Title");
        if (title != null) UnityEngine.Object.DestroyImmediate(title.gameObject);
        ArrangeMeter(root, "Stamina", "StaminaFrame", new Vector2(-250, 1000), new Color(1f, 0.71f, 0.20f));
        ArrangeMeter(root, "Learning", "LearningFrame", new Vector2(250, 1000), new Color(0.25f, 0.6f, 1f));
        ArrangeMeter(root, "Fullness", "FullnessFrame", new Vector2(-250, 750), new Color(0.52f, 0.79f, 0.29f));
        ArrangeMeter(root, "Happiness", "HappinessFrame", new Vector2(250, 750), new Color(1f, 0.34f, 0.48f));
        var panel = root.Find("MessagePanel") as RectTransform;
        if (panel == null) panel = new GameObject("MessagePanel", typeof(RectTransform), typeof(CanvasGroup), typeof(CareMessagePopup)).GetComponent<RectTransform>();
        panel.SetParent(root, false);
        Position(panel, new Vector2(0, 410), new Vector2(960, 230));
        // Layered pixel rectangles echo the wood, gold trim and parchment in the supplied art.
        Plate(panel, "OuterWood", new Vector2(952, 222), new Color(0.22f, 0.09f, 0.04f));
        Plate(panel, "CarvedEdge", new Vector2(940, 210), new Color(0.62f, 0.29f, 0.12f));
        Plate(panel, "GoldTrim", new Vector2(916, 186), new Color(0.96f, 0.67f, 0.32f));
        Plate(panel, "InnerShadow", new Vector2(904, 174), new Color(0.39f, 0.18f, 0.08f));
        Plate(panel, "Parchment", new Vector2(892, 162), new Color(1f, 0.89f, 0.66f));
        var messageTransform = root.Find("CareMessage") ?? panel.Find("CareMessage");
        var message = messageTransform != null ? messageTransform.GetComponent<Text>() :
            new GameObject("CareMessage", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        message.transform.SetParent(panel, false);
        message.transform.SetAsLastSibling();
        Position(message.rectTransform, Vector2.zero, new Vector2(850, 90));
        message.font = PixelFont(); message.fontSize = 28; message.alignment = TextAnchor.MiddleCenter;
        message.color = new Color(0.2f, 0.1f, 0.05f); message.raycastTarget = false;
        message.text = "WHAT SHALL WE DO TODAY?";
        var outline = message.GetComponent<Outline>() ?? message.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1, 0.9f, 0.7f); outline.effectDistance = new Vector2(2, -2);
        var popupData = new SerializedObject(panel.GetComponent<CareMessagePopup>());
        popupData.FindProperty("message").objectReferenceValue = message; popupData.ApplyModifiedPropertiesWithoutUndo();
        var controller = UnityEngine.Object.FindFirstObjectByType<MonsterController>();
        var choices = panel.Find("GuessingChoices") as RectTransform;
        if (choices == null) choices = new GameObject("GuessingChoices", typeof(RectTransform)).GetComponent<RectTransform>();
        choices.SetParent(panel, false); Position(choices, new Vector2(0, -48), new Vector2(800, 64));
        Choice(choices, "Left", -190, controller.ChooseLeft);
        Choice(choices, "Right", 190, controller.ChooseRight);
        choices.gameObject.SetActive(false);
        var timerObject = panel.Find("RoundTimer");
        var timer = timerObject != null ? timerObject.GetComponent<Text>() : new GameObject("RoundTimer", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        timer.transform.SetParent(panel, false);
        Position(timer.rectTransform, new Vector2(0, 5), new Vector2(260, 24));
        timer.font = PixelFont(); timer.fontSize = 20; timer.alignment = TextAnchor.MiddleCenter;
        timer.color = new Color(0.43f, 0.20f, 0.08f); timer.raycastTarget = false;
        timer.gameObject.SetActive(false);
        var data = new SerializedObject(controller);
        data.FindProperty("eatButton").objectReferenceValue = root.Find("EatButton").GetComponent<Button>();
        data.FindProperty("staminaFill").objectReferenceValue = root.Find("StaminaMeter/Fill").GetComponent<Image>();
        data.FindProperty("happinessFill").objectReferenceValue = root.Find("HappinessMeter/Fill").GetComponent<Image>();
        data.FindProperty("fullnessFill").objectReferenceValue = root.Find("FullnessMeter/Fill").GetComponent<Image>();
        data.FindProperty("learningFill").objectReferenceValue = root.Find("LearningMeter/Fill").GetComponent<Image>();
        data.FindProperty("fullnessLabel").objectReferenceValue = root.Find("FullnessMeter/Label").GetComponent<Text>();
        data.FindProperty("learningLabel").objectReferenceValue = root.Find("LearningMeter/Label").GetComponent<Text>();
        data.FindProperty("careMessage").objectReferenceValue = message;
        data.FindProperty("roundTimerLabel").objectReferenceValue = timer;
        data.FindProperty("guessingChoices").objectReferenceValue = choices.gameObject;
        data.FindProperty("leftChoiceLabel").objectReferenceValue = choices.Find("Left/Text").GetComponent<Text>();
        data.FindProperty("rightChoiceLabel").objectReferenceValue = choices.Find("Right/Text").GetComponent<Text>();
        data.ApplyModifiedPropertiesWithoutUndo();
        var petImage = root.Find("Monster").GetComponent<Image>();
        var normalized = MonsterSceneBuilder.NormalizedFrame(petImage.sprite);
        Position(petImage.rectTransform, new Vector2(normalized.z, normalized.w), new Vector2(normalized.x, normalized.y));
        Position(root.Find("PlayButton") as RectTransform, new Vector2(-245, -620), new Vector2(450, 340));
        Position(root.Find("StudyButton") as RectTransform, new Vector2(245, -620), new Vector2(450, 340));
        Position(root.Find("SleepButton") as RectTransform, new Vector2(-245, -990), new Vector2(450, 340));
        Position(root.Find("EatButton") as RectTransform, new Vector2(245, -990), new Vector2(450, 340));
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.defaultScreenWidth = 1080;
        PlayerSettings.defaultScreenHeight = 2400;
        PlayerSettings.defaultIsNativeResolution = false;
        MonsterAudioSetup.Apply();
        SelectPhoneGameView();
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("MONSTER_MOBILE_LAYOUT_READY: 1080 x 2400; all four actions within viewport.");
    }

    private static Font PixelFont()
    {
        return AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PressStart2P-Regular.ttf");
    }

    private static void ArrangeMeter(RectTransform root, string name, string spriteName, Vector2 position, Color color)
    {
        var meter = root.Find(name + "Meter") as RectTransform;
        if (meter == null)
        {
            meter = UnityEngine.Object.Instantiate(root.Find("StaminaMeter").gameObject, root).GetComponent<RectTransform>();
            meter.name = name + "Meter";
        }
        Position(meter, position, new Vector2(900, 275));
        meter.localScale = Vector3.one * 0.53f;
        var frame = meter.Find("Frame").GetComponent<Image>();
        Position(frame.rectTransform, Vector2.zero, new Vector2(900, 275));
        frame.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Frame-Bar.png").OfType<Sprite>().First(s => s.name == spriteName);
        var fillObject = meter.Find("Fill").gameObject;
        var fill = fillObject.GetComponent<PixelMeterFill>();
        if (fill == null)
        {
            UnityEngine.Object.DestroyImmediate(fillObject.GetComponent<Image>());
            fill = fillObject.AddComponent<PixelMeterFill>();
        }
        // Calibrated in original sheet pixels; map through the frame's aspect-fit scale.
        Rect slot = name == "Stamina" ? new Rect(523, 225, 496, 75) :
            name == "Learning" ? new Rect(1604, 225, 477, 75) :
            name == "Fullness" ? new Rect(531, 544, 496, 64) : new Rect(1596, 548, 480, 59);
        float cropX = name == "Learning" || name == "Happiness" ? 1110 : 25;
        float cropY = name == "Stamina" || name == "Learning" ? 35 : 365;
        var spriteRect = frame.sprite.rect;
        float scale = Mathf.Min(900 / spriteRect.width, 275 / spriteRect.height);
        Vector2 trackCenter = new Vector2((slot.center.x - cropX - spriteRect.width * 0.5f) * scale,
            (spriteRect.height * 0.5f - (slot.center.y - cropY)) * scale);
        Vector2 trackSize = slot.size * scale;
        Position(fill.rectTransform, trackCenter, trackSize);
        fill.raycastTarget = false;
        fill.sprite = WhiteSprite(); fill.color = color; fill.preserveAspect = false;
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        float value = name == "Stamina" ? 100 : name == "Learning" ? 0 : 75;
        fill.fillAmount = value / 100f;
        var label = meter.Find("Label").GetComponent<Text>();
        Position(label.rectTransform, trackCenter, new Vector2(trackSize.x - 12, trackSize.y + 6));
        label.font = PixelFont(); label.fontSize = 52; label.color = Color.white;
        label.text = $"{Mathf.RoundToInt(value)}/100";
        var shadow = label.GetComponent<Shadow>() ?? label.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f); shadow.effectDistance = new Vector2(1, -2);
        var nameTransform = meter.Find("Name");
        var nameLabel = nameTransform != null ? nameTransform.GetComponent<Text>() :
            new GameObject("Name", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        nameLabel.transform.SetParent(meter, false);
        Position(nameLabel.rectTransform, new Vector2(180, 48), new Vector2(450, 60));
        var badge = Plate(meter, "NameBadge", new Vector2(450, 64), new Color(1, 0.88f, 0.64f));
        badge.rectTransform.anchoredPosition = new Vector2(180, 48);
        nameLabel.transform.SetAsLastSibling();
        nameLabel.font = PixelFont(); nameLabel.fontSize = 38;
        nameLabel.text = name == "Fullness" ? "FOOD" : name == "Learning" ? "STUDY" : name.ToUpperInvariant();
        nameLabel.alignment = TextAnchor.MiddleCenter; nameLabel.color = new Color(0.2f, 0.1f, 0.05f);
        nameLabel.raycastTarget = false;
    }

    private static Image Plate(RectTransform parent, string name, Vector2 size, Color color)
    {
        var child = parent.Find(name);
        var image = child != null ? child.GetComponent<Image>() : new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        Position(image.rectTransform, Vector2.zero, size);
        image.color = color; image.raycastTarget = false;
        return image;
    }

    private static void Choice(RectTransform parent, string name, float x, UnityEngine.Events.UnityAction action)
    {
        var image = Plate(parent, name, new Vector2(340, 60), new Color(0.43f, 0.2f, 0.08f));
        image.rectTransform.anchoredPosition = new Vector2(x, 0); image.raycastTarget = true;
        var button = image.GetComponent<Button>() ?? image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, i);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, action);
        var child = image.transform.Find("Text");
        var text = child != null ? child.GetComponent<Text>() : new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(image.transform, false); Position(text.rectTransform, Vector2.zero, new Vector2(330, 56));
        text.font = PixelFont(); text.fontSize = 26; text.text = name.ToUpperInvariant();
        text.alignment = TextAnchor.MiddleCenter; text.color = new Color(1, 0.9f, 0.65f); text.raycastTarget = false;
    }

    private static Sprite WhiteSprite()
    {
        const string path = "Assets/MeterFill.png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(4, 4);
            texture.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray()); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void Position(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }

    // Unity exposes custom Game view sizes only through its Editor internals.
    // Guard this optional convenience so future Editor changes cannot block scene setup.
    public static void SelectPhoneGameView()
    {
        if (Application.isBatchMode) return;
        try
        {
            var assembly = typeof(Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { 0 });
            var groupType = group.GetType();
            int total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            int selected = -1;
            for (int i = 0; i < total; i++)
            {
                var size = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                if ((int)size.GetType().GetProperty("width").GetValue(size) == 1080 &&
                    (int)size.GetType().GetProperty("height").GetValue(size) == 2400) selected = i;
            }
            if (selected < 0)
            {
                var sizeType = assembly.GetType("UnityEditor.GameViewSize");
                var enumType = assembly.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(sizeType, Enum.ToObject(enumType, 1), 1080, 2400, "Phone 1080 x 2400");
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                selected = total;
            }
            var gameView = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            gameView.GetType().GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(gameView, selected);
            var zoomField = gameView.GetType().GetField("m_ZoomArea", BindingFlags.NonPublic | BindingFlags.Instance);
            var zoom = zoomField?.GetValue(gameView);
            var scaleProperty = zoom?.GetType().GetProperty("scale", BindingFlags.Public | BindingFlags.Instance);
            float fit = Mathf.Min((gameView.position.width - 20) / 1080f, (gameView.position.height - 60) / 2400f);
            if (scaleProperty != null && scaleProperty.CanWrite)
                scaleProperty.SetValue(zoom, Vector2.one * Mathf.Clamp(fit, 0.1f, 1f));
            gameView.Repaint();
        }
        catch (Exception ex) { Debug.LogWarning("Select Phone 1080 x 2400 in Game view manually: " + ex.Message); }
    }

    public static void RenderPreview() => RenderPreviewAt(1080, 2400, "Builds/Previews/Monster-1080x2400.png");

    public static void RenderPreviewAt(int width, int height, string path)
    {
        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        var camera = Camera.main;
        var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; float oldDistance = canvas.planeDistance;
        var oldTarget = camera.targetTexture; float oldAspect = camera.aspect;
        var previous = RenderTexture.active;
        var target = new RenderTexture(width, height, 24);
        Texture2D pixels = null;
        try
        {
            camera.targetTexture = target; camera.aspect = (float)width / height;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            canvas.transform.Find("PhoneViewport").SendMessage("LateUpdate");
            Canvas.ForceUpdateCanvases();
            var bounds = new Vector3[4];
            var content = (RectTransform)canvas.transform.Find("PhoneViewport");
            var canvasRect = (RectTransform)canvas.transform;
            content.GetWorldCorners(bounds);
            foreach (var corner in bounds)
            {
                var local = canvasRect.InverseTransformPoint(corner);
                if (local.x < canvasRect.rect.xMin - 0.1f || local.x > canvasRect.rect.xMax + 0.1f ||
                    local.y < canvasRect.rect.yMin - 0.1f || local.y > canvasRect.rect.yMax + 0.1f)
                    throw new InvalidOperationException("Phone content exceeds preview bounds at " + width + "x" + height);
            }
            camera.Render();
            RenderTexture.active = target;
            pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = oldTarget; camera.aspect = oldAspect;
            canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldDistance;
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(target);
            Canvas.ForceUpdateCanvases(); canvas.transform.Find("PhoneViewport").SendMessage("LateUpdate");
        }
        Debug.Log("MONSTER_PREVIEW_READY: " + path);
    }

    public static void BuildAndValidate()
    {
        MonsterSceneBuilder.Build();
        Apply();
        RenderPreview();
        MonsterSceneBuilder.Validate();
    }
}
