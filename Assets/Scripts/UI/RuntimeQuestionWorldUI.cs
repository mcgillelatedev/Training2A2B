using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RuntimeQuestionWorldUI : MonoBehaviour
{
    [Serializable]
    public class AnswerSelectedEvent : UnityEvent<string> { }

    [Header("Question Content")]
    [SerializeField, TextArea(2, 6)] private string question;
    [SerializeField] private string answerA;
    [SerializeField] private string answerB;
    [SerializeField] private string answerC;
    [SerializeField] private string answerD;
    [SerializeField] private Sprite image;

    [Header("World Placement")]
    [SerializeField] private Transform uiSpawnTransform;
    [SerializeField] private Camera eventCamera;
    [SerializeField] private Vector2 canvasSize = new Vector2(1000f, 820f);
    [SerializeField, Min(0.0001f)] private float worldScale = 0.0015f;

    [Header("Events")]
    [SerializeField] private AnswerSelectedEvent onAnswerSelected;

    private Canvas createdCanvas;

    private void Start()
    {
        CreateQuestionUI();
    }

    public void CreateQuestionUI()
    {
        if (createdCanvas != null)
            return;

        if (uiSpawnTransform == null)
        {
            Debug.LogError("Question UI requires a spawn transform.", this);
            return;
        }

        if (eventCamera == null)
            eventCamera = Camera.main;

        createdCanvas = RuntimeWorldSpaceUIUtility.CreateWorldCanvas(
            "Runtime Question Canvas",
            uiSpawnTransform,
            canvasSize,
            worldScale,
            eventCamera
        );

        Image panel = RuntimeWorldSpaceUIUtility.CreatePanel(
            createdCanvas.transform,
            "Question Panel",
            new Color(0.05f, 0.06f, 0.08f, 0.95f)
        );

        BuildLayout(panel.transform);
    }

    private void BuildLayout(Transform parent)
    {
        VerticalLayoutGroup layout =
            parent.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(35, 35, 35, 35);
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI questionText = RuntimeWorldSpaceUIUtility.CreateText(
            parent,
            "Question",
            question,
            42f,
            TextAlignmentOptions.Center
        );
        SetPreferredHeight(questionText.gameObject, 130f);

        if (image != null)
        {
            GameObject imageObject = new GameObject(
                "Question Image",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );

            imageObject.transform.SetParent(parent, false);

            Image imageComponent = imageObject.GetComponent<Image>();
            imageComponent.sprite = image;
            imageComponent.preserveAspect = true;
            imageComponent.raycastTarget = false;

            SetPreferredHeight(imageObject, 240f);
        }

        CreateAnswerButton(parent, "A", answerA);
        CreateAnswerButton(parent, "B", answerB);
        CreateAnswerButton(parent, "C", answerC);
        CreateAnswerButton(parent, "D", answerD);
    }

    private void CreateAnswerButton(
        Transform parent,
        string answerKey,
        string answerText
    )
    {
        if (string.IsNullOrWhiteSpace(answerText))
            return;

        Button button = RuntimeWorldSpaceUIUtility.CreateButton(
            parent,
            $"Answer {answerKey}",
            $"{answerKey}. {answerText}"
        );

        SetPreferredHeight(button.gameObject, 82f);
        button.onClick.AddListener(() => SelectAnswer(answerKey));
    }

    private void SelectAnswer(string answerKey)
    {
        Debug.Log($"Selected answer: {answerKey}", this);
        onAnswerSelected?.Invoke(answerKey);
    }

    private static void SetPreferredHeight(
        GameObject target,
        float preferredHeight
    )
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>();

        if (layoutElement == null)
            layoutElement = target.AddComponent<LayoutElement>();

        layoutElement.preferredHeight = preferredHeight;
    }

    private void OnDestroy()
    {
        if (createdCanvas != null)
            Destroy(createdCanvas.gameObject);
    }
}
