using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraBoundaryBinder : MonoBehaviour
{
    [SerializeField] private CameraHardClamp2D cameraClamp;
    [SerializeField] private float retryInterval = 0.1f;
    [SerializeField] private int retryCount = 30;

    private IEnumerator Start()
    {
        if (cameraClamp == null)
            cameraClamp = FindObjectOfType<CameraHardClamp2D>(true);

        for (int i = 0; i < retryCount; i++)
        {
            MapBoundary boundary = FindObjectOfType<MapBoundary>(true);

            if (cameraClamp != null && boundary != null)
            {
                boundary.RefreshBounds();
                cameraClamp.SetBoundary(boundary);
                yield break;
            }

            yield return new WaitForSeconds(retryInterval);
        }

        Debug.LogWarning("CameraBoundaryBinder: CameraHardClamp2D 또는 MapBoundary를 찾지 못했습니다.", this);
    }
}