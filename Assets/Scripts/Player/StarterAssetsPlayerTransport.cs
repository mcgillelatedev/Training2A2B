using StarterAssets;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class StarterAssetsPlayerTransport : MonoBehaviour
{
    [SerializeField] private FirstPersonController firstPersonController;
    [SerializeField] private StarterAssetsInputs starterAssetsInputs;

#if ENABLE_INPUT_SYSTEM
    [SerializeField] private PlayerInput playerInput;
#endif

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (firstPersonController == null)
            firstPersonController = GetComponent<FirstPersonController>();

        if (starterAssetsInputs == null)
            starterAssetsInputs = GetComponent<StarterAssetsInputs>();

#if ENABLE_INPUT_SYSTEM
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();
#endif
    }

    public void SetPlayerControlEnabled(bool enabled)
    {
        ClearInput();

        if (firstPersonController != null)
            firstPersonController.enabled = enabled;

        if (characterController != null)
            characterController.enabled = enabled;
    }

    public void TeleportTo(Transform destination)
    {
        if (destination == null)
            return;

        TeleportTo(destination.position, destination.rotation);
    }

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        bool controllerWasEnabled =
            characterController != null && characterController.enabled;

        if (characterController != null)
            characterController.enabled = false;

        transform.SetPositionAndRotation(position, rotation);
        ClearInput();

        if (characterController != null)
            characterController.enabled = controllerWasEnabled;
    }

    private void ClearInput()
    {
        if (starterAssetsInputs == null)
            return;

        starterAssetsInputs.MoveInput(Vector2.zero);
        starterAssetsInputs.LookInput(Vector2.zero);
        starterAssetsInputs.JumpInput(false);
        starterAssetsInputs.SprintInput(false);
    }
}
