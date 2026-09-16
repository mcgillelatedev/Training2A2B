using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuntimeTeleportAreasUI : MonoBehaviour
{
    [Header("Player / Safe Teleport")]
    [SerializeField] private CesiumPlayerSpawnGate spawnGate;
    [SerializeField] private DualModePlayerTransport playerTransport;

    [Header("World Placement")]
    [SerializeField] private Transform uiSpawnTransform;
    [SerializeField] private Camera eventCamera;
    [SerializeField] private Vector2 canvasSize = new Vector2(1250f, 720f);
    [SerializeField, Min(0.0001f)] private float worldScale = 0.0015f;

    [Header("Area 1")]
    [SerializeField] private string area1Name = "Area 1";
    [SerializeField] private Sprite area1Image;
    [SerializeField] private Transform area1Transform;

    [Header("Area 2")]
    [SerializeField] private string area2Name = "Area 2";
    [SerializeField] private Sprite area2Image;
    [SerializeField] private Transform area2Transform;

    [Header("Area 3")]
    [SerializeField] private string area3Name = "Area 3";
    [SerializeField] private Sprite area3Image;
    [SerializeField] private Transform area3Transform;

    [Header("Area 4")]
    [SerializeField] private string area4Name = "Area 4";
    [SerializeField] private Sprite area4Image;
    [SerializeField] private Transform area4Transform;

    [Header("Area 5")]
    [SerializeField] private string area5Name = "Area 5";
    [SerializeField] private Sprite area5Image;
    [SerializeField] private Transform area5Transform;

    private Canvas createdCanvas;

    private void Start()
    {
        CreateTeleportUI();
    }

    public void CreateTeleportUI()
    {
        if (createdCanvas != null)
            return;

        if (uiSpawnTransform == null)
        {
            Debug.LogError("Teleport UI requires a spawn transform.", this);
            return;
        }

        if (eventCamera == null)
            eventCamera = Camera.main;

        if (playerTransport == null)
            playerTransport = FindFirstObjectByType<DualModePlayerTransport>();

        createdCanvas = RuntimeWorldSpaceUIUtility.CreateWorldCanvas(
            "Runtime Teleport Areas Canvas",
            uiSpawnTransform,
            canvasSize,
            worldScale,
            eventCamera
        );

        Image panel = RuntimeWorldSpaceUIUtility.CreatePanel(
            createdCanvas.transform,
            "Teleport Areas Panel",
            new Color(0.05f, 0.06f, 0.08f, 0.95f)
        );

        BuildLayout(panel.transform);
    }

    private void BuildLayout(Transform parent)
    {
        VerticalLayoutGroup rootLayout =
            parent.gameObject.AddComponent<VerticalLayoutGroup>();

        rootLayout.padding = new RectOffset(35, 35, 35, 35);
        rootLayout.spacing = 25f;
        rootLayout.childAlignment = TextAnchor.UpperCenter;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        TextMeshProUGUI title = RuntimeWorldSpaceUIUtility.CreateText(
            parent,
            "Title",
            "Teleport Areas",
            52f,
            TextAlignmentOptions.Center
        );
        SetPreferredHeight(title.gameObject, 90f);

        GameObject gridObject = new GameObject(
            "Area Grid",
            typeof(RectTransform),
            typeof(GridLayoutGroup),
            typeof(LayoutElement)
        );

        gridObject.transform.SetParent(parent, false);

        GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(215f, 265f);
        grid.spacing = new Vector2(20f, 20f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.MiddleCenter;

        SetPreferredHeight(gridObject, 300f);

        CreateAreaButton(gridObject.transform, area1Name, area1Image, area1Transform);
        CreateAreaButton(gridObject.transform, area2Name, area2Image, area2Transform);
        CreateAreaButton(gridObject.transform, area3Name, area3Image, area3Transform);
        CreateAreaButton(gridObject.transform, area4Name, area4Image, area4Transform);
        CreateAreaButton(gridObject.transform, area5Name, area5Image, area5Transform);
    }

    private void CreateAreaButton(
        Transform parent,
        string areaName,
        Sprite areaImage,
        Transform destination
    )
    {
        Button button = RuntimeWorldSpaceUIUtility.CreateButton(
            parent,
            areaName,
            string.Empty
        );

        VerticalLayoutGroup buttonLayout =
            button.gameObject.AddComponent<VerticalLayoutGroup>();

        buttonLayout.padding = new RectOffset(10, 10, 10, 10);
        buttonLayout.spacing = 10f;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = false;
        buttonLayout.childForceExpandWidth = true;
        buttonLayout.childForceExpandHeight = false;

        Transform existingLabel = button.transform.Find("Label");
        if (existingLabel != null)
            Destroy(existingLabel.gameObject);

        if (areaImage != null)
        {
            GameObject imageObject = new GameObject(
                "Image",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );

            imageObject.transform.SetParent(button.transform, false);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = areaImage;
            image.preserveAspect = true;
            image.raycastTarget = false;

            SetPreferredHeight(imageObject, 180f);
        }

        TextMeshProUGUI label = RuntimeWorldSpaceUIUtility.CreateText(
            button.transform,
            "Area Name",
            areaName,
            28f,
            TextAlignmentOptions.Center
        );

        SetPreferredHeight(label.gameObject, 55f);

        button.onClick.AddListener(() => Teleport(destination));
    }

    private void Teleport(Transform destination)
    {
        if (destination == null)
        {
            Debug.LogWarning("Teleport destination is not assigned.", this);
            return;
        }

        if (spawnGate != null)
        {
            spawnGate.TeleportSafely(destination);
            return;
        }

        if (playerTransport != null)
        {
            playerTransport.TeleportTo(destination);
            return;
        }

        Debug.LogError("No player transport or Cesium spawn gate is assigned.", this);
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
