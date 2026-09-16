using UnityEngine;

public class PlayerModeInitializer : MonoBehaviour
{
    [SerializeField] private GameObject desktopPlayerRig;
    [SerializeField] private GameObject vrPlayerRig;

    private void Awake()
    {
        bool useVR = ControlModeSettings.SelectedMode == ControlMode.VR;

        if (desktopPlayerRig != null)
            desktopPlayerRig.SetActive(!useVR);

        if (vrPlayerRig != null)
            vrPlayerRig.SetActive(useVR);
    }
}
