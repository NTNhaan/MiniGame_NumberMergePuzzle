using UnityEngine;
using UnityEngine.UI;

public class TextEffect : MonoBehaviour
{
    [SerializeField] private Text textComponent;
    // [SerializeField] private Text LineComponent;
    public void Update()
    {
        PlayZoomEffect();
    }

    public void PlayZoomEffect(float duration = 1.5f, float zoomScale = 0.9f)
    {
        StartCoroutine(ZoomCoroutine(duration, zoomScale));
    }

    private System.Collections.IEnumerator ZoomCoroutine(float duration, float zoomScale)
    {
        float halfDuration = duration / 1f;
        Vector3 originalScale = textComponent.transform.localScale;
        Vector3 targetScale = originalScale * zoomScale;
        // Vector3 lineOriginalScale = LineComponent.transform.localScale;
        // Vector3 lineTargetScale = lineOriginalScale * zoomScale;
        float timer = 0f;
        // Zoom in
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            textComponent.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            // LineComponent.transform.localScale = Vector3.Lerp(lineOriginalScale, lineTargetScale, t);
            yield return null;
        }
        // Zoom out
        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            textComponent.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            // LineComponent.transform.localScale = Vector3.Lerp(lineTargetScale, lineOriginalScale, t);
            yield return null;
        }
        textComponent.transform.localScale = originalScale;
        // LineComponent.transform.localScale = lineOriginalScale;
    }
}
