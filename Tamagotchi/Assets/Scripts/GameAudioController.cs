using UnityEngine;

/// <summary>Non-spatial retro effects and a crossfaded music bed with saved volume levels.</summary>
[DefaultExecutionOrder(-100)]
public sealed class GameAudioController : MonoBehaviour
{
    public enum Cue { Play, Study, Eat, Sleep, Correct, Wrong, Attention, Message, Select }
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip[] effects = new AudioClip[9];
    [SerializeField] private AudioSource musicA;
    [SerializeField] private AudioSource musicB;
    [SerializeField] private AudioSource effectsSource;

    private const string MusicKey = "Tamagotchi.Audio.Music.v4";
    private const string EffectsKey = "Tamagotchi.Audio.Effects.v3";
    private const float CrossfadeSeconds = 1.5f;
    [SerializeField, Range(0, 1)] private float musicLevel = 0.45f;
    [SerializeField, Range(0, 1)] private float effectsLevel = 0.9f;
    private float blend, fadeIn, duckTime, duckGain = 1;
    private float lastEffectTime = -100;
    private bool crossfading, usingA = true, paused;
    private Cue actionCue;
    private int actionPulsesLeft;
    private float pulseTimer, pulseInterval;
    public float MusicVolume => musicLevel;
    public float EffectsVolume => effectsLevel;
    public Cue LastCue { get; private set; }
    public int EffectsPlayed { get; private set; }
    public string LastClipName { get; private set; }

    private void Awake()
    {
        musicLevel = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, musicLevel));
        effectsLevel = Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsKey, effectsLevel));
    }
    private void Start()
    {
        if (backgroundMusic == null || musicA == null || musicB == null) return;
        musicA.clip = musicB.clip = backgroundMusic;
        musicA.volume = musicB.volume = 0;
        musicA.Play();
    }
    private void Update()
    {
        float seconds = Time.unscaledDeltaTime;
        UpdateMusic(seconds);
        UpdateActionSound(seconds);
    }
    public void PlayActionCue(Cue cue)
    {
        PlayCue(cue);
        actionCue = cue;
        actionPulsesLeft = cue == Cue.Eat ? 3 : cue == Cue.Sleep ? 2 : 0;
        pulseInterval = cue == Cue.Eat ? 0.35f : cue == Cue.Study ? 0.55f : 1f;
        pulseTimer = pulseInterval;
    }
    private void UpdateActionSound(float seconds)
    {
        if (paused || actionPulsesLeft <= 0) return;
        pulseTimer -= seconds;
        if (pulseTimer > 0) return;
        EmitCue(actionCue, false);
        actionPulsesLeft--;
        pulseTimer = pulseInterval;
    }
    public void StopActionSound() { actionPulsesLeft = 0; }

    private void UpdateMusic(float seconds)
    {
        if (paused || backgroundMusic == null || musicA == null || musicB == null) return;
        fadeIn = Mathf.Min(1, fadeIn + seconds);
        duckTime = Mathf.Max(0, duckTime - seconds);
        duckGain = Mathf.MoveTowards(duckGain, duckTime > 0 ? 0.4f : 1f, seconds * 1.5f);
        // Keep the music below effects, even with its user level at 100%.
        float gain = musicLevel * 0.42f * fadeIn * duckGain;
        AudioSource active = usingA ? musicA : musicB;
        AudioSource next = usingA ? musicB : musicA;
        if (!crossfading && active.isPlaying && backgroundMusic.length - active.time <= CrossfadeSeconds)
        {
            next.time = 0; next.volume = 0; next.Play();
            crossfading = true; blend = 0;
        }
        if (crossfading)
        {
            blend = Mathf.Min(1, blend + seconds / CrossfadeSeconds);
            active.volume = gain * (1 - blend); next.volume = gain * blend;
            if (blend >= 1) { active.Stop(); usingA = !usingA; crossfading = false; }
        }
        else
        {
            active.volume = gain; next.volume = 0;
        }
    }
    public void PlayCue(Cue cue) { EmitCue(cue, true); }
    private void EmitCue(Cue cue, bool restartDucking)
    {
        int index = (int)cue;
        if (effectsSource == null || effects == null || index >= effects.Length || effects[index] == null) return;
        // A message pop must not double up with an action cue in the same frame.
        if (cue == Cue.Message && Time.unscaledTime - lastEffectTime < 0.15f) return;
        if (cue == Cue.Correct || cue == Cue.Wrong) StopActionSound();
        LastCue = cue;
        LastClipName = effects[index].name;
        if (restartDucking && (cue == Cue.Play || cue == Cue.Study || cue == Cue.Eat || cue == Cue.Sleep || cue == Cue.Correct || cue == Cue.Wrong))
        {
            // Pull the music back immediately so short action sounds stay intelligible.
            duckTime = Mathf.Max(duckTime, cue == Cue.Sleep ? 3 : 0.8f);
            duckGain = Mathf.Min(duckGain, 0.4f);
        }
        if (effectsLevel > 0)
        {
            float gain = cue == Cue.Message ? 0.14f : cue == Cue.Attention ? 0.4f : cue == Cue.Correct || cue == Cue.Wrong ? 0.9f : 0.78f;
            effectsSource.PlayOneShot(effects[index], effectsLevel * gain);
            EffectsPlayed++;
        }
        lastEffectTime = Time.unscaledTime;
    }
    public void SetMusicVolume(float value)
    {
        musicLevel = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MusicKey, musicLevel);
        PlayerPrefs.Save();
    }
    public void SetEffectsVolume(float value)
    {
        effectsLevel = Mathf.Clamp01(value);
        if (effectsLevel == 0 && effectsSource != null) effectsSource.Stop();
        PlayerPrefs.SetFloat(EffectsKey, effectsLevel);
        PlayerPrefs.Save();
    }
    private void OnApplicationPause(bool isPaused)
    {
        paused = isPaused;
        foreach (var source in new[] { musicA, musicB, effectsSource })
        {
            if (source == null) continue;
            if (paused) source.Pause(); else source.UnPause();
        }
    }
}
