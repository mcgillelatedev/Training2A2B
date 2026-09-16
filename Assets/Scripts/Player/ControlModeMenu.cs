using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

public class ControlModeMenu : MonoBehaviour
{
    [SerializeField] private string trainingSceneName = "Training";
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private GameObject vrLoadingPanel;

    private bool launchInProgress;

    public void StartDesktop()
    {
        if (launchInProgress)
            return;

        launchInProgress = true;
        ControlModeSettings.SelectedMode = ControlMode.Desktop;
        SceneManager.LoadScene(trainingSceneName);
    }

    public void StartVR()
    {
        if (!launchInProgress)
            StartCoroutine(StartVRRoutine());
    }

    private IEnumerator StartVRRoutine()
    {
        launchInProgress = true;
        ControlModeSettings.SelectedMode = ControlMode.VR;

        if (menuRoot != null)
            menuRoot.SetActive(false);

        if (vrLoadingPanel != null)
            vrLoadingPanel.SetActive(true);

        XRManagerSettings manager = XRGeneralSettings.Instance.Manager;

        if (manager.activeLoader == null)
            yield return manager.InitializeLoader();

        if (manager.activeLoader == null)
        {
            Debug.LogError(
                "OpenXR could not initialize. Confirm the Vive is connected, " +
                "SteamVR is running/available, and SteamVR is the active OpenXR runtime.",
                this
            );

            launchInProgress = false;

            if (menuRoot != null)
                menuRoot.SetActive(true);

            if (vrLoadingPanel != null)
                vrLoadingPanel.SetActive(false);

            yield break;
        }

        manager.StartSubsystems();
        SceneManager.LoadScene(trainingSceneName);
    }

    public static void ShutdownXR()
    {
        XRManagerSettings manager = XRGeneralSettings.Instance.Manager;

        if (manager == null || manager.activeLoader == null)
            return;

        manager.StopSubsystems();
        manager.DeinitializeLoader();
    }
}
