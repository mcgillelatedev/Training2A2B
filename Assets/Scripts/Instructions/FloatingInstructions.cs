using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FloatingInstructions : MonoBehaviour
{
    public enum Language
    {
        English,
        French
    }

    [Header("Language")]
    public Language currentLanguage = Language.English;

    [Header("Player & Positioning")]
    public Transform playerCamera;
    public GameObject instructionUIObject;
    public float distanceFromPlayer = 2.0f;
    public float heightOffset = -0.5f;
    public float horizontalOffset = 0.5f;
    public float smoothSpeed = 8f;
    public KeyCode toggleKey = KeyCode.H;

    [Header("UI Elements")]
    public TextMeshProUGUI instructionText;

    [Header("Instruction Settings")]
    public float nonActionStepDelay = 0.2f;
    public KeyCode advanceKey = KeyCode.Return;

    [Header("Grouped Instruction Lists")]
    public List<InstructionGroup> instructionGroups = new List<InstructionGroup>();

    [Header("Narration")]
    public AudioSource audioSource;

    [Header("Typing Text")]
    public bool useTypewriterText = true;
    public float charactersPerSecond = 45f;
    public AudioSource typingAudioSource;
    public AudioClip typingLoopClip;

    private InstructionStep currentStep;
    private bool canAdvance = true;
    private bool isHidden;
    private bool isTextBuilding;
    private string fullCurrentText = "";
    private Coroutine typewriterCoroutine;

    private void Start()
    {
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        ShowNextStep();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            SetInstructionVisible(isHidden);

        if (currentStep == null)
            return;

        if (Input.GetKeyDown(advanceKey) && canAdvance)
        {
            if (isTextBuilding)
            {
                FinishTypewriterText();
                return;
            }

            TryAdvanceStep();
        }

        if (currentStep.actionType == ActionType.ClosePopup)
            CheckPopupClosed();
    }

    private void LateUpdate()
    {
        if (playerCamera == null || instructionUIObject == null)
            return;

        Vector3 targetPosition =
            playerCamera.position +
            playerCamera.forward * distanceFromPlayer +
            playerCamera.right * horizontalOffset +
            playerCamera.up * heightOffset;

        instructionUIObject.transform.position = Vector3.Lerp(
            instructionUIObject.transform.position,
            targetPosition,
            1f - Mathf.Exp(-smoothSpeed * Time.deltaTime)
        );

        Vector3 towardCamera =
            instructionUIObject.transform.position - playerCamera.position;

        if (towardCamera.sqrMagnitude > 0.001f)
        {
            instructionUIObject.transform.rotation = Quaternion.LookRotation(
                towardCamera.normalized,
                playerCamera.up
            );
        }
    }

    private void TryAdvanceStep()
    {
        switch (currentStep.actionType)
        {
            case ActionType.SimpleClick:
                CompleteStep(currentStep);
                break;

            case ActionType.CustomMethod:
                currentStep.customActionEvent?.Invoke();
                if (currentStep.completeAfterCustomMethod)
                    CompleteStep(currentStep);
                break;

            // ClickButton and ClosePopup complete automatically.
        }
    }

    private void PrepareStep(InstructionStep step)
    {
        canAdvance = true;

        if (step.actionType == ActionType.ClickButton)
        {
            Button button = step.targetButton;

            if (button == null)
            {
                GameObject buttonObject = FindByName(step.actionParam1);
                if (buttonObject != null)
                    button = buttonObject.GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.RemoveListener(CompleteCurrentStepFromButton);
                button.onClick.AddListener(CompleteCurrentStepFromButton);
            }
            else
            {
                Debug.LogWarning(
                    "ClickButton instruction could not find Button: " +
                    step.actionParam1,
                    this
                );
            }
        }
    }

    private void CompleteCurrentStepFromButton()
    {
        if (currentStep != null && currentStep.actionType == ActionType.ClickButton)
            CompleteStep(currentStep);
    }

    private void CheckPopupClosed()
    {
        GameObject popup = ResolveGameObject(
            currentStep.actionParam1,
            currentStep.targetObject
        );

        if (popup != null && !popup.activeInHierarchy)
            CompleteStep(currentStep);
    }

    private void CompleteStep(InstructionStep step)
    {
        if (step == null || step.actionComplete)
            return;

        step.actionComplete = true;
        HandlePostActions(step);
        ShowNextStep();
    }

    private void HandlePostActions(InstructionStep step)
    {
        foreach (PostAction action in step.postActions)
        {
            switch (action.type)
            {
                case PostActionType.ActivateObject:
                case PostActionType.OpenPopup:
                    SetObjectActive(action, true);
                    break;

                case PostActionType.DeactivateObject:
                    SetObjectActive(action, false);
                    break;

                case PostActionType.LERPObject:
                    LerpObjectAction(action);
                    break;

                case PostActionType.MoveObjectImmediate:
                    MoveObjectImmediate(action);
                    break;

                case PostActionType.MakeInstructionInvisible:
                    SetInstructionVisible(false);
                    break;

                case PostActionType.MakeInstructionVisible:
                    SetInstructionVisible(true);
                    break;

                case PostActionType.PlaySound:
                    PlaySound(action);
                    break;

                case PostActionType.ChangeCamera:
                    ChangeCamera(action);
                    break;

                case PostActionType.CustomMethod:
                    InvokePostActionMethod(action);
                    break;
            }
        }
    }

    private void InvokePostActionMethod(PostAction action)
    {
        string methodName = action.param1?.Trim();
        string receiverName = action.param2?.Trim();

        if (string.IsNullOrWhiteSpace(methodName))
        {
            Debug.LogWarning("CustomMethod post-action has no method name.", this);
            return;
        }

        GameObject receiverObject = string.IsNullOrWhiteSpace(receiverName)
            ? gameObject
            : FindByName(receiverName);

        if (receiverObject == null)
        {
            Debug.LogWarning(
                $"CustomMethod could not find receiver '{receiverName}'.",
                this
            );
            return;
        }

        foreach (MonoBehaviour component in receiverObject.GetComponents<MonoBehaviour>())
        {
            if (component == null)
                continue;

            MethodInfo method = component.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null
            );

            if (method == null)
                continue;

            method.Invoke(component, null);
            return;
        }

        Debug.LogWarning(
            $"No parameterless method named '{methodName}' was found on '{receiverObject.name}'.",
            receiverObject
        );
    }

    private void SetObjectActive(PostAction action, bool active)
    {
        GameObject obj = ResolveGameObject(action.param1, action.targetObject);
        if (obj != null)
            obj.SetActive(active);
    }

    private void LerpObjectAction(PostAction action)
    {
        GameObject obj = ResolveGameObject(action.param1, action.targetObject);
        Transform destination = ResolveTransform(action.param2, action.targetTransform);

        if (obj != null && destination != null)
            StartCoroutine(LerpObject(obj, destination, 2f));
    }

    private void MoveObjectImmediate(PostAction action)
    {
        GameObject obj = ResolveGameObject(action.param1, action.targetObject);
        Transform destination = ResolveTransform(action.param2, action.targetTransform);

        if (obj == null || destination == null)
            return;

        obj.transform.SetPositionAndRotation(
            destination.position,
            destination.rotation
        );
    }

    private void PlaySound(PostAction action)
    {
        AudioClip clip = action.soundToPlay;

        if (clip == null && !string.IsNullOrWhiteSpace(action.param1))
            clip = Resources.Load<AudioClip>(action.param1);

        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    private void ChangeCamera(PostAction action)
    {
        Camera cam = action.targetCamera;

        if (cam == null)
        {
            GameObject obj = FindByName(action.param1);
            if (obj != null)
                cam = obj.GetComponent<Camera>();
        }

        if (cam == null)
            return;

        foreach (Camera sceneCam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            sceneCam.enabled = false;

        cam.enabled = true;
    }

    private IEnumerator LerpObject(GameObject obj, Transform target, float speed)
    {
        while (Vector3.Distance(obj.transform.position, target.position) > 0.02f)
        {
            obj.transform.position = Vector3.MoveTowards(
                obj.transform.position,
                target.position,
                speed * Time.deltaTime
            );

            obj.transform.rotation = Quaternion.RotateTowards(
                obj.transform.rotation,
                target.rotation,
                speed * 100f * Time.deltaTime
            );

            yield return null;
        }

        obj.transform.SetPositionAndRotation(target.position, target.rotation);
    }

    public void ShowNextStep()
    {
        currentStep = GetNextAvailableStep();

        if (currentStep == null)
        {
            if (instructionText != null)
                instructionText.text = "";

            SetInstructionVisible(false);
            return;
        }

        fullCurrentText = currentLanguage == Language.French
            ? currentStep.frenchText
            : currentStep.englishText;

        StartTypewriterText(fullCurrentText);
        PlayNarration(currentStep.narrationClip);
        PrepareStep(currentStep);

        if (currentStep.actionType == ActionType.SimpleClick)
            StartCoroutine(EnableAdvanceAfterDelay());
    }

    private InstructionStep GetNextAvailableStep()
    {
        foreach (InstructionGroup group in instructionGroups)
        {
            if (!group.isEnabled)
                continue;

            InstructionStep step = group.steps.FirstOrDefault(s => !s.actionComplete);
            if (step != null)
                return step;
        }

        return null;
    }

    public void CompleteCurrentWaitingStep()
    {
        if (currentStep != null)
            CompleteStep(currentStep);
    }

    public void NotifyStepCompletedFromEvent(string stepName)
    {
        InstructionStep step = FindStepByName(stepName);
        if (step != null)
            CompleteStep(step);
    }

    public void EnableGroup(string groupName)
    {
        InstructionGroup group = instructionGroups.FirstOrDefault(
            g => g.groupName == groupName
        );

        if (group != null)
        {
            group.isEnabled = true;
            ShowNextStep();
        }
    }

    public void DisableGroup(string groupName)
    {
        InstructionGroup group = instructionGroups.FirstOrDefault(
            g => g.groupName == groupName
        );

        if (group != null)
            group.isEnabled = false;
    }

    public void MarkStepAsCompleted(string stepName)
    {
        InstructionStep step = FindStepByName(stepName);
        if (step != null)
        {
            step.actionComplete = true;
            ShowNextStep();
        }
    }

    public void MarkStepAsNotCompleted(string stepName)
    {
        InstructionStep step = FindStepByName(stepName);
        if (step != null)
        {
            step.actionComplete = false;
            ShowNextStep();
        }
    }

    private InstructionStep FindStepByName(string stepName)
    {
        foreach (InstructionGroup group in instructionGroups)
        {
            InstructionStep step = group.steps.FirstOrDefault(
                s => s.stepName == stepName
            );

            if (step != null)
                return step;
        }

        return null;
    }

    private IEnumerator EnableAdvanceAfterDelay()
    {
        canAdvance = false;
        yield return new WaitForSeconds(nonActionStepDelay);
        canAdvance = true;
    }

    private void StartTypewriterText(string text)
    {
        if (instructionText == null)
            return;

        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        if (!useTypewriterText)
        {
            instructionText.text = text;
            isTextBuilding = false;
            StopTypingSound();
            return;
        }

        typewriterCoroutine = StartCoroutine(TypewriterRoutine(text ?? ""));
    }

    private IEnumerator TypewriterRoutine(string text)
    {
        isTextBuilding = true;
        instructionText.text = "";
        StartTypingSound();

        float delay = 1f / Mathf.Max(1f, charactersPerSecond);

        for (int i = 0; i < text.Length; i++)
        {
            instructionText.text += text[i];
            yield return new WaitForSeconds(delay);
        }

        isTextBuilding = false;
        StopTypingSound();
    }

    private void FinishTypewriterText()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        if (instructionText != null)
            instructionText.text = fullCurrentText;

        isTextBuilding = false;
        StopTypingSound();
    }

    private void StartTypingSound()
    {
        if (typingAudioSource == null)
            return;

        if (typingLoopClip != null)
            typingAudioSource.clip = typingLoopClip;

        typingAudioSource.loop = true;

        if (typingAudioSource.clip != null && !typingAudioSource.isPlaying)
            typingAudioSource.Play();
    }

    private void StopTypingSound()
    {
        if (typingAudioSource != null && typingAudioSource.isPlaying)
            typingAudioSource.Stop();
    }

    private void PlayNarration(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private GameObject ResolveGameObject(string objectName, GameObject fallback)
    {
        if (fallback != null)
            return fallback;

        return FindByName(objectName);
    }

    private Transform ResolveTransform(string transformName, Transform fallback)
    {
        if (fallback != null)
            return fallback;

        GameObject obj = FindByName(transformName);
        return obj != null ? obj.transform : null;
    }

    private GameObject FindByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        GameObject found = GameObject.Find(objectName);
        if (found != null)
            return found;

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform t in allTransforms)
        {
            if (t != null && t.name == objectName && t.gameObject.scene.IsValid())
                return t.gameObject;
        }

        return null;
    }

    private void SetInstructionVisible(bool visible)
    {
        GameObject obj = instructionUIObject != null
            ? instructionUIObject
            : instructionText != null ? instructionText.gameObject : null;

        if (obj == null)
            return;

        CanvasGroup group = obj.GetComponent<CanvasGroup>();
        if (group == null)
            group = obj.AddComponent<CanvasGroup>();

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
        isHidden = !visible;
    }
}
