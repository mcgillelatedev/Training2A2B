using UnityEngine;
using UnityEngine.UI;

public class SimpleCrosshair : MonoBehaviour
{
    [SerializeField] private Vector2 size = new Vector2(6f, 6f);

    private void Start()
    {
        GameObject canvasObject = new GameObject(
            "Crosshair Canvas",
            typeof(Canvas),
            typeof(CanvasScaler)
        );

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        GameObject dot = new GameObject(
            "Crosshair",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        dot.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = dot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = dot.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
    }
}
