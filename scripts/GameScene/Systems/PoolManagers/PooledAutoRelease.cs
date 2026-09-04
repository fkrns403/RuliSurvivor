using UnityEngine;

/// <summary>
/// 일정 시간이 지나면 자동으로 SpecialPool로 반환되는 컴포넌트
/// - 경고 타일, 원형 경고, 폭발 이펙트, 링 이펙트 등에 사용
/// - SpecialAttack 전용으로 사용하는 것을 권장
/// </summary>
public class PooledAutoRelease : PooledObject
{
    [SerializeField] private float lifeTime = 1f;

    private float timer;
    private bool running;

    public void SetLifeTime(float seconds)
    {
        lifeTime = Mathf.Max(0.01f, seconds);
    }

    public override void OnSpawned()
    {
        timer = 0f;
        running = true;
    }

    public override void OnDespawned()
    {
        running = false;
        timer = 0f;
    }

    private void Update()
    {
        if (!running)
            return;

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.isPaused)
            return;

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            if (SpecialBulletPoolManager.Instance != null)
                SpecialBulletPoolManager.Instance.ReleaseSpecial(gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}