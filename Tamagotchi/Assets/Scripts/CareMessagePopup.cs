using UnityEngine;
using UnityEngine.UI;

/// <summary>A short, unobtrusive pop whenever the pet says something new.</summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class CareMessagePopup : MonoBehaviour
{
    [SerializeField] private Text message;
    [SerializeField] private GameAudioController gameAudio;
    private string previous;
    private float elapsed = 1f;
    private CanvasGroup group;
    private void Awake() { group = GetComponent<CanvasGroup>(); }
    private void Update()
    {
        if (message == null) return;
        if (message.text != previous)
        {
            previous = message.text; elapsed = 0;
            if (gameAudio != null) gameAudio.PlayCue(GameAudioController.Cue.Message);
        }
        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsed / 0.16f);
        transform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, progress);
        group.alpha = Mathf.Lerp(0.6f, 1f, progress);
    }
}
