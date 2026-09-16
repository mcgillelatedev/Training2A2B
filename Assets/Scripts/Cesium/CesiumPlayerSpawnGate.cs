using System.Collections;
using CesiumForUnity;
using UnityEngine;

public class CesiumPlayerSpawnGate : MonoBehaviour
{
    [Header("Cesium")]
    [SerializeField] private Cesium3DTileset tileset;

    [Header("Selected Player Rig")]
    [SerializeField] private DualModePlayerTransport playerTransport;
    [SerializeField] private Transform desiredSpawnPoint;

    [Header("Ground Detection")]
    [SerializeField, Min(0f)] private float groundRayStartHeight = 300f;
    [SerializeField, Min(1f)] private float groundRayDistance = 1000f;
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Loading")]
    [SerializeField, Range(0f, 100f)] private float requiredLoadProgress = 95f;
    [SerializeField, Min(0f)] private float minimumWaitTime = 1f;
    [SerializeField, Min(0.05f)] private float retryInterval = 0.25f;
    [SerializeField, Min(1f)] private float maximumWaitTime = 45f;
    [SerializeField] private bool enablePlayerIfTimedOut = false;

    [Header("Optional UI")]
    [SerializeField] private GameObject loadingPanel;

    private Coroutine teleportCoroutine;

    private IEnumerator Start()
    {
        if (tileset == null || playerTransport == null)
        {
            Debug.LogError(
                "CesiumPlayerSpawnGate requires both a Cesium3DTileset and DualModePlayerTransport.",
                this
            );
            yield break;
        }

        if (desiredSpawnPoint == null)
        {
            Debug.LogError("Assign PlayerSpawn to Desired Spawn Point.", this);
            yield break;
        }

        yield return PlacePlayerWhenReady(desiredSpawnPoint);
    }

    public void TeleportSafely(Transform destination)
    {
        if (destination == null)
            return;

        if (teleportCoroutine != null)
            StopCoroutine(teleportCoroutine);

        teleportCoroutine = StartCoroutine(PlacePlayerWhenReady(destination));
    }

    private IEnumerator PlacePlayerWhenReady(Transform destination)
    {
        playerTransport.SetPlayerControlEnabled(false);

        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        // Move the selected rig/camera near the destination while locomotion is gated.
        // The active camera remains enabled so Cesium knows what region to stream.
        playerTransport.TeleportTo(destination.position, destination.rotation);

        if (minimumWaitTime > 0f)
            yield return new WaitForSeconds(minimumWaitTime);

        float elapsed = 0f;
        bool surfaceFound = false;
        Vector3 groundedPosition = destination.position;

        while (elapsed < maximumWaitTime)
        {
            if (tileset.ComputeLoadProgress() >= requiredLoadProgress)
            {
                surfaceFound = TryFindGround(
                    destination.position,
                    out groundedPosition
                );

                if (surfaceFound)
                    break;
            }

            yield return new WaitForSeconds(retryInterval);
            elapsed += retryInterval;
        }

        if (surfaceFound)
        {
            playerTransport.TeleportTo(
                groundedPosition,
                destination.rotation
            );

            playerTransport.SetPlayerControlEnabled(true);
        }
        else
        {
            Debug.LogWarning(
                "Cesium ground was not detected before the timeout. " +
                "The selected player rig remains locomotion-gated at the destination.",
                this
            );

            if (enablePlayerIfTimedOut)
                playerTransport.SetPlayerControlEnabled(true);
        }

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        teleportCoroutine = null;
    }

    private bool TryFindGround(
        Vector3 approximatePosition,
        out Vector3 groundedPosition
    )
    {
        Vector3 rayOrigin = approximatePosition +
            Vector3.up * groundRayStartHeight;

        if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                groundRayDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore
            ))
        {
            groundedPosition = hit.point +
                Vector3.up * playerTransport.ActiveGroundOffset;

            return true;
        }

        groundedPosition = approximatePosition;
        return false;
    }
}
