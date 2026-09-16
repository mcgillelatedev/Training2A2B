using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class RuntimeWorldSpaceUIUtility
{
    public static Canvas CreateWorldCanvas(
        string canvasName,
        Transform worldAnchor,
        Vector2 canvasSize,
        float worldScale,
        Camera eventCamera
    )
    {
        GameObject canvasObject = new GameObject(
            canvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(TrackedDeviceGraphicRaycaster)
        );

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = eventCamera;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = canvasSize;

        if (worldAnchor != null)
        {
            canvasObject.transform.SetPositionAndRotation(
                worldAnchor.position,
                worldAnchor.rotation
            );
        }

        canvasObject.transform.localScale = Vector3.one * worldScale;
        return canvas;
    }

    public static Image CreatePanel(
        Transform parent,
        string objectName,
        Color backgroundColor
    )
    {
        GameObject panelObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        panelObject.transform.SetParent(parent, false);

        Image image = panelObject.GetComponent<Image>();
        image.color = backgroundColor;

        Stretch(panelObject.GetComponent<RectTransform>());
        return image;
    }

    public static TextMeshProUGUI CreateText(
        Transform parent,
        string objectName,
        string value,
        float fontSize,
        TextAlignmentOptions alignment
    )
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.raycastTarget = false;

        return text;
    }

    public static Button CreateButton(
        Transform parent,
        string objectName,
        string label
    )
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        buttonObject.transform.SetParent(parent, false);

        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.16f, 0.18f, 0.22f, 1f);
        background.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();

        TextMeshProUGUI text = CreateText(
            buttonObject.transform,
            "Label",
            label,
            30f,
            TextAlignmentOptions.Center
        );

        Stretch(text.rectTransform);
        return button;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
