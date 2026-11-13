using System;
using UnityEditor;
using UnityEngine;

public static class MonsterAudioSetup
{
    public static void Apply()
    {
        var root = GameObject.Find("PhoneViewport").transform;
        var controller = UnityEngine.Object.FindFirstObjectByType<MonsterController>();
        var audio = UnityEngine.Object.FindFirstObjectByType<GameAudioController>();
        if (audio == null) audio = new GameObject("GameAudio").AddComponent<GameAudioController>();
        if (Camera.main.GetComponent<AudioListener>() == null) Camera.main.gameObject.AddComponent<AudioListener>();
        var data = new SerializedObject(audio);
        data.FindProperty("backgroundMusic").objectReferenceValue = Import("Assets/Audio/Music/ArcadeVibe.mp3", true);
        string[] names = Enum.GetNames(typeof(GameAudioController.Cue));
        var clips = data.FindProperty("effects"); clips.arraySize = names.Length;
        for (int i = 0; i < names.Length; i++)
        {
            bool pixel = names[i] == "Play" || names[i] == "Study" || names[i] == "Eat" || names[i] == "Sleep" || names[i] == "Wrong";
            string path = "Assets/Audio/SFX/" + (pixel ? "Pixel" + names[i] + ".wav" : names[i] + ".ogg");
            if (names[i] == "Wrong") path = "Assets/Audio/SFX/soundshelfstudio-ui-error-pop-515668.mp3";
            clips.GetArrayElementAtIndex(i).objectReferenceValue = Import(path, false);
        }
        foreach (string name in new[] { "musicA", "musicB", "effectsSource" })
        {
            var child = audio.transform.Find(name);
            var source = child != null ? child.GetComponent<AudioSource>() : new GameObject(name).AddComponent<AudioSource>();
            source.transform.SetParent(audio.transform, false);
            source.playOnAwake = false; source.loop = false; source.spatialBlend = 0;
            source.volume = 0; // Effects use a fixed source gain; music fades in at runtime.
            if (name == "effectsSource") source.volume = 1;
            data.FindProperty(name).objectReferenceValue = source;
        }
        foreach (string name in new[] { "MusicVolume", "EffectsVolume" })
        {
            var control = root.Find(name);
            if (control != null) UnityEngine.Object.DestroyImmediate(control.gameObject);
        }
        data.FindProperty("musicLevel").floatValue = 0.45f;
        data.FindProperty("effectsLevel").floatValue = 0.9f;
        data.ApplyModifiedPropertiesWithoutUndo();
        var pet = new SerializedObject(controller);
        pet.FindProperty("gameAudio").objectReferenceValue = audio; pet.ApplyModifiedPropertiesWithoutUndo();
        var popup = new SerializedObject(root.Find("MessagePanel").GetComponent<CareMessagePopup>());
        popup.FindProperty("gameAudio").objectReferenceValue = audio; popup.ApplyModifiedPropertiesWithoutUndo();
    }
    private static AudioClip Import(string path, bool music)
    {
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null) throw new InvalidOperationException("Missing audio asset " + path);
        var settings = importer.defaultSampleSettings;
        settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
        settings.quality = 0.75f;
        settings.preloadAudioData = !music;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = !music;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
