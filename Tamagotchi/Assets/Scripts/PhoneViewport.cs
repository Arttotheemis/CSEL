using UnityEngine;

/// <summary>Fits a fixed phone design into the available display, including safe areas.</summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public sealed class PhoneViewport : MonoBehaviour
{
    private void LateUpdate()
    {
        var rect = (RectTransform)transform;
        var parent = transform.parent as RectTransform;
        if (parent == null || parent.rect.width <= 0 || parent.rect.height <= 0) return;
        Rect safe = Application.isPlaying ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);
        float widthFraction = Screen.width > 0 ? safe.width / Screen.width : 1;
        float heightFraction = Screen.height > 0 ? safe.height / Screen.height : 1;
        float scale = Mathf.Min(parent.rect.width * widthFraction / 1080f, parent.rect.height * heightFraction / 2400f);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1080, 2400);
        rect.localScale = new Vector3(scale, scale, 1);
        rect.anchoredPosition = Application.isPlaying && Screen.width > 0 && Screen.height > 0
            ? new Vector2((safe.center.x / Screen.width - 0.5f) * parent.rect.width,
                (safe.center.y / Screen.height - 0.5f) * parent.rect.height) : Vector2.zero;
    }
}
