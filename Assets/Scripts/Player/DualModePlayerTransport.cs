using StarterAssets;
using UnityEngine;

public class DualModePlayerTransport : MonoBehaviour
{
    [Header("Desktop Rig")]
    [SerializeField] private GameObject desktopRig;
    [SerializeField] private StarterAssetsPlayerTransport desktopTransport;
    [SerializeField] private float desktopGroundOffset = 0.15f;

    [Header("VR Rig")]
    [SerializeField] private GameObject vrRig;
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Camera xrCamera;
    [Tooltip("Assign VR locomotion behaviours that must be disabled while Cesium ground is loading.")]
    [SerializeField] private Behaviour[] vrLocomotionBehaviours;
    [SerializeField] private float vrGroundOffset = 0.0f;

    public bool IsVR => ControlModeSettings.SelectedMode == ControlMode.VR;

    public float ActiveGroundOffset => IsVR ? vrGroundOffset : desktopGroundOffset;

    public Transform ActiveRigTransform
    {
        get
        {
            if (IsVR)
                return xrOrigin;

            return desktopRig != null ? desktopRig.transform : null;
        }
    }

    public Camera ActiveCamera
    {
        get
        {
            if (IsVR)
                return xrCamera;

            if (desktopRig == null)
                return null;

            return desktopRig.GetComponentInChildren<Camera>(true);
        }
    }

    public void SetPlayerControlEnabled(bool enabled)
    {
        if (!IsVR)
        {
            if (desktopTransport != null)
                desktopTransport.SetPlayerControlEnabled(enabled);

            return;
        }

        if (vrLocomotionBehaviours == null)
            return;

        foreach (Behaviour behaviour in vrLocomotionBehaviours)
        {
            if (behaviour != null)
                behaviour.enabled = enabled;
        }
    }

    public void TeleportTo(Transform destination)
    {
        if (destination == null)
            return;

        TeleportTo(destination.position, destination.rotation);
    }

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        if (!IsVR)
        {
            if (desktopTransport != null)
                desktopTransport.TeleportTo(position, rotation);

            return;
        }

        if (xrOrigin == null)
            return;

        // The destination represents a floor-level target. Rotate the XR Origin
        // around Y, then offset the Origin so the tracked HMD's X/Z position
        // lands on the requested destination while preserving physical head height.
        Vector3 euler = rotation.eulerAngles;
        xrOrigin.rotation = Quaternion.Euler(0f, euler.y, 0f);

        if (xrCamera != null)
        {
            Vector3 cameraOffset = xrCamera.transform.position - xrOrigin.position;
            Vector3 planarOffset = Vector3.ProjectOnPlane(cameraOffset, Vector3.up);
            xrOrigin.position = position - planarOffset;
        }
        else
        {
            xrOrigin.position = position;
        }
    }
}
