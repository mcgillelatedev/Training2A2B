using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum ActionType
{
    // Legacy name retained so existing CSV files do not need to change.
    // In this generic version, SimpleClick means "press Enter to continue".
    SimpleClick,
    ClickButton,
    ClosePopup,
    CustomMethod
}

public enum PostActionType
{
    ActivateObject,
    DeactivateObject,
    LERPObject,
    MoveObjectImmediate,
    OpenPopup,
    MakeInstructionInvisible,
    MakeInstructionVisible,
    PlaySound,
    ChangeCamera,
    CustomMethod
}

[System.Serializable]
public class PostAction
{
    public PostActionType type;

    [Header("CSV / Lookup Params")]
    public string param1;
    public string param2;

    [Header("Manual Overrides")]
    public GameObject targetObject;
    public Transform targetTransform;
    public Camera targetCamera;
    public AudioClip soundToPlay;
}

[System.Serializable]
public class InstructionStep
{
    [Header("Basic Info")]
    public string stepName;
    public AudioClip narrationClip;

    [TextArea(2, 5)] public string englishText;
    [TextArea(2, 5)] public string frenchText;

    [Header("Step Control")]
    public bool actionComplete = false;

    [Header("Action To Complete")]
    public ActionType actionType = ActionType.SimpleClick;
    public string actionParam1;
    public string actionParam2;

    [Header("Manual Overrides")]
    public Button targetButton;
    public GameObject targetObject;
    public UnityEvent customActionEvent;
    public bool completeAfterCustomMethod = true;

    [Header("After Action Completed")]
    public List<PostAction> postActions = new List<PostAction>();
}
