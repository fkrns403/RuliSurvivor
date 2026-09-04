using System.Collections;
using UnityEngine;

/// <summary>
/// 스윙 무기나 대쉬 연출에 사용하는 잔상 효과
/// 
/// 특징:
/// - 프리팹 없이 런타임에 GameObject를 생성
/// - StartGhostEffect() / StopGhostEffect()를 호출하는 동안만 생성
/// - 장착 검의 휘두르기 순간 연출에 적합
/// 
/// 주의:
/// - 회전무기처럼 계속 살아있는 오브젝트에 상시 켜두는 것은 비추천
/// </summary>
[DisallowMultipleComponent]
public class AfterImage1 : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("잔상을 복사할 원본 SpriteRenderer")]
    [SerializeField] private SpriteRenderer targetSprite;

    [Header("Spawn")]
    [SerializeField] private float spawnInterval = 0.05f;
    [SerializeField] private float lifeTime = 0.3f;

    [Header("Visual")]
    [SerializeField] private Color ghostColor = new Color(0.5f, 1f, 1f, 0.5f);
    [SerializeField] private int sortingOffset = -1;

    private bool isSpawning;
    private Coroutine routine;

    private void Awake()
    {
        if (targetSprite == null)
            targetSprite = GetComponent<SpriteRenderer>();
    }

    public void StartGhostEffect()
    {
        if (isSpawning) return;
        if (targetSprite == null) return;

        isSpawning = true;
        routine = StartCoroutine(SpawnGhostRoutine());
    }

    public void StopGhostEffect()
    {
        isSpawning = false;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private IEnumerator SpawnGhostRoutine()
    {
        while (isSpawning)
        {
            if (targetSprite != null && targetSprite.sprite != null)
            {
                GameObject ghost = new GameObject("AfterImage");
                ghost.layer = gameObject.layer;

                SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
                sr.sprite = targetSprite.sprite;
                sr.flipX = targetSprite.flipX;
                sr.color = ghostColor;
                sr.sortingLayerID = targetSprite.sortingLayerID;
                sr.sortingOrder = targetSprite.sortingOrder + sortingOffset;

                Transform t = ghost.transform;
                t.position = targetSprite.transform.position;
                t.rotation = targetSprite.transform.rotation;
                t.localScale = targetSprite.transform.lossyScale;

                Destroy(ghost, lifeTime);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }
}