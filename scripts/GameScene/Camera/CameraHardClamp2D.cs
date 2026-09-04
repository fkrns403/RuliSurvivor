using UnityEngine;
using Cinemachine;

/// <summary>
/// 2D 카메라 하드 클램프.
/// 
/// 기준:
/// - Virtual Camera Body = Transposer
/// - BoxCollider2D bounds 기준
/// - 카메라 화면 끝이 맵 경계 밖으로 나가지 않게 제한
/// - 튜토리얼 / 일반맵 상단 UI 보정값 분리
/// 
/// 주의:
/// - GameManager.selectedMap 같은 필드에 직접 의존하지 않는다.
/// - PlayerPrefs의 SelectedMapIndex 값을 기준으로 맵을 판단한다.
/// </summary>
[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CameraHardClamp2D : CinemachineExtension
{
    [Header("Map Boundary")]
    [SerializeField] private BoxCollider2D boundaryCollider;

    [Header("Map Index Rule")]
    [SerializeField] private int tutorialMapIndex = 0;

    [Header("UI View Offset")]
    [Tooltip("튜토리얼 맵 상단 보정값")]
    [SerializeField] private float tutorialTopViewOffset = 0.8f;

    [Tooltip("일반 맵 상단 보정값")]
    [SerializeField] private float normalTopViewOffset = 1.5f;

    public void SetBoundary(BoxCollider2D boundary)
    {
        boundaryCollider = boundary;
    }

    public void SetBoundary(MapBoundary boundary)
    {
        if (boundary == null)
        {
            boundaryCollider = null;
            return;
        }

        boundaryCollider = boundary.BoundaryCollider;

        if (boundaryCollider == null)
            boundaryCollider = boundary.GetComponent<BoxCollider2D>();

        if (boundaryCollider == null)
            boundaryCollider = boundary.GetComponentInChildren<BoxCollider2D>(true);
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize)
            return;

        if (boundaryCollider == null)
            return;

        Bounds b = boundaryCollider.bounds;

        if (b.size == Vector3.zero)
            return;

        LensSettings lens = state.Lens;

        if (!lens.Orthographic)
            return;

        Vector3 pos = state.RawPosition;

        float halfH = lens.OrthographicSize;

        float aspect = lens.Aspect;

        if (aspect <= 0f && Camera.main != null)
            aspect = Camera.main.aspect;

        if (aspect <= 0f)
            aspect = 16f / 9f;

        float halfW = halfH * aspect;

        float minX = b.min.x + halfW;
        float maxX = b.max.x - halfW;

        float minY = b.min.y + halfH;

        float currentTopOffset = GetCurrentTopViewOffset();

        float maxY = b.max.y - halfH + currentTopOffset;

        if (minX > maxX)
            minX = maxX = (minX + maxX) * 0.5f;

        if (minY > maxY)
            minY = maxY = (minY + maxY) * 0.5f;

        float clampedX = Mathf.Clamp(pos.x, minX, maxX);
        float clampedY = Mathf.Clamp(pos.y, minY, maxY);

        state.RawPosition = new Vector3(clampedX, clampedY, pos.z);
    }

    private float GetCurrentTopViewOffset()
    {
        int selectedMapIndex = PlayerPrefs.GetInt("SelectedMapIndex", 0);

        if (selectedMapIndex == tutorialMapIndex)
            return tutorialTopViewOffset;

        return normalTopViewOffset;
    }
}