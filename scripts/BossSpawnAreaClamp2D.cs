using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 스폰 위치 보정 컴포넌트.
/// 
/// 사용 위치:
/// - 보스 프리팹 루트에 붙인다.
/// 
/// 문제 상황:
/// - 보스가 화면 밖에 생성된다.
/// - 보스 등장 연출이 보스 위치를 기준으로 실행된다.
/// - 카메라가 보스나 UI 쪽으로 끌려가고 복귀가 꼬인다.
/// 
/// 해결:
/// - 보스가 활성화된 직후 몇 프레임 기다린다.
/// - MapBoundary와 Camera.main의 보이는 영역을 계산한다.
/// - 보스 위치를 맵 안쪽 + 카메라 가시 영역 근처로 보정한다.
/// 
/// 주의:
/// - 이 스크립트는 보스가 생성된 직후 위치를 한 번 보정하는 역할이다.
/// - 보스 AI 이동 제한은 별도 이동 코드에서 처리해야 한다.
/// </summary>
[DisallowMultipleComponent]
public class BossSpawnAreaClamp2D : MonoBehaviour
{
    [Header("Clamp Option")]
    [SerializeField] private bool clampOnEnable = true;

    [Tooltip("카메라 화면 안쪽 여백")]
    [SerializeField] private float cameraPadding = 1.5f;

    [Tooltip("맵 경계 안쪽 여백")]
    [SerializeField] private float mapPadding = 1f;

    [Tooltip("보스가 너무 화면 밖이면 플레이어 근처로 보정")]
    [SerializeField] private bool preferNearPlayer = true;

    [SerializeField] private Vector2 nearPlayerOffset = new Vector2(4f, 2f);

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private void OnEnable()
    {
        if (!clampOnEnable)
            return;

        StartCoroutine(ClampNextFrames());
    }

    private IEnumerator ClampNextFrames()
    {
        yield return null;
        yield return null;

        ClampNow();
    }

    public void ClampNow()
    {
        Vector3 current = transform.position;
        current.z = 0f;

        Vector3 desired = current;

        if (preferNearPlayer && GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
        {
            Vector3 playerPos = GameManager.Instance.PlayerTransform.position;
            desired = playerPos + new Vector3(nearPlayerOffset.x, nearPlayerOffset.y, 0f);
        }

        desired = ClampToCameraVisibleArea(desired);
        desired = ClampToMapBoundary(desired);
        desired.z = 0f;

        transform.position = desired;

        if (verboseLog)
            Debug.Log($"BossSpawnAreaClamp2D: 보스 위치 보정 {current} -> {desired}", this);
    }

    private Vector3 ClampToCameraVisibleArea(Vector3 position)
    {
        Camera cam = Camera.main;

        if (cam == null)
            return position;

        if (!cam.orthographic)
            return position;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        Vector3 camPos = cam.transform.position;

        float minX = camPos.x - halfWidth + cameraPadding;
        float maxX = camPos.x + halfWidth - cameraPadding;
        float minY = camPos.y - halfHeight + cameraPadding;
        float maxY = camPos.y + halfHeight - cameraPadding;

        if (minX > maxX)
        {
            float center = camPos.x;
            minX = center;
            maxX = center;
        }

        if (minY > maxY)
        {
            float center = camPos.y;
            minY = center;
            maxY = center;
        }

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }

    private Vector3 ClampToMapBoundary(Vector3 position)
    {
        GameManager gm = GameManager.Instance;

        if (gm == null || gm.CurrentMapBoundary == null)
            return position;

        BoxCollider2D box = gm.CurrentMapBoundary.GetComponent<BoxCollider2D>();

        if (box == null)
            return position;

        Bounds bounds = box.bounds;

        float minX = bounds.min.x + mapPadding;
        float maxX = bounds.max.x - mapPadding;
        float minY = bounds.min.y + mapPadding;
        float maxY = bounds.max.y - mapPadding;

        if (minX > maxX)
        {
            float center = bounds.center.x;
            minX = center;
            maxX = center;
        }

        if (minY > maxY)
        {
            float center = bounds.center.y;
            minY = center;
            maxY = center;
        }

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }
}