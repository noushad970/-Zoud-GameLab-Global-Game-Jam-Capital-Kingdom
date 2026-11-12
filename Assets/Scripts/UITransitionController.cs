
using UnityEngine;
using System.Collections;

public class UITransitionController : MonoBehaviour
{
    [Header("UI Images")]
    [SerializeField] private RectTransform downImage;
    [SerializeField] private RectTransform upImage;

    [Header("Animation Settings")]
    [SerializeField] private float startY = 434.6154f;
    [SerializeField] private float endY = -434.6154f;
    [SerializeField] private float duration = 3f;

    private void Start()
    {
        if (downImage == null || upImage == null)
        {
            Debug.LogError("Please assign both UI Images!");
            return;
        }

        // Set initial positions
        SetY(downImage, startY);
        SetY(upImage, endY);

        // Enable both images
        downImage.gameObject.SetActive(true);
        upImage.gameObject.SetActive(true);

        StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Move both images simultaneously
            SetY(downImage, Mathf.Lerp(startY, endY, t));
            SetY(upImage, Mathf.Lerp(endY, startY, t));

            yield return null;
        }

        // Ensure final positions
        SetY(downImage, endY);
        SetY(upImage, startY);

        // Disable both images
        downImage.gameObject.SetActive(false);
        upImage.gameObject.SetActive(false);
    }

    private void SetY(RectTransform rect, float y)
    {
        Vector2 pos = rect.anchoredPosition;
        pos.y = y;
        rect.anchoredPosition = pos;
    }
}
