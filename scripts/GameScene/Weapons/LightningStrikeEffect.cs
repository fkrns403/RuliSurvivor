using UnityEngine;

/// <summary>
/// 코드만으로 생성하는 낙뢰 이펙트
/// 
/// 역할:
/// - start -> end를 잇는 짧은 LineRenderer 번개
/// - 짧은 시간 후 자동 제거
/// 
/// 장점:
/// - 별도 이펙트 프리팹 없이 바로 사용 가능
/// - 시제품 단계에서 빠르게 붙이기 좋다.
/// </summary>
public class LightningStrikeEffect : MonoBehaviour
{
    private LineRenderer line;
    private float lifeTime;
    private float timer;

    /// <summary>
    /// 외부에서 간단히 생성할 수 있도록 정적 생성 함수 제공
    /// </summary>
    public static void Create(Vector3 start, Vector3 end, float duration, float width)
    {
        GameObject go = new GameObject("LightningStrikeEffect");
        LightningStrikeEffect fx = go.AddComponent<LightningStrikeEffect>();
        fx.Initialize(start, end, duration, width);
    }

    private void Initialize(Vector3 start, Vector3 end, float duration, float width)
    {
        lifeTime = Mathf.Max(0.02f, duration);

        line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.SetPosition(0, start);
        line.SetPosition(1, end);

        line.startWidth = width;
        line.endWidth = width * 0.35f;
        line.numCapVertices = 4;
        line.material = new Material(Shader.Find("Sprites/Default"));

        line.startColor = new Color(0.8f, 0.95f, 1f, 1f);
        line.endColor = new Color(0.6f, 0.9f, 1f, 0.2f);

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (line == null)
            return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / lifeTime);

        Color start = line.startColor;
        Color end = line.endColor;

        start.a = Mathf.Lerp(1f, 0f, t);
        end.a = Mathf.Lerp(0.2f, 0f, t);

        line.startColor = start;
        line.endColor = end;
    }
}