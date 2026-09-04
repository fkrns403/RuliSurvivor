using UnityEngine;

[DisallowMultipleComponent]
public class EnemyStatsScaler : MonoBehaviour
{
    [Header("Time -> Multiplier (x)")]
    [Tooltip("x축=시간(초), y축=체력 배수")]
    [SerializeField] private AnimationCurve hpMulByTime = AnimationCurve.Linear(0, 1, 180, 3);

    [Tooltip("x축=시간(초), y축=이속 배수")]
    [SerializeField] private AnimationCurve speedMulByTime = AnimationCurve.Linear(0, 1, 180, 1.6f);

    public float GetHpMul(float gameTime) => Mathf.Max(0.1f, hpMulByTime.Evaluate(gameTime));
    public float GetSpeedMul(float gameTime) => Mathf.Max(0.1f, speedMulByTime.Evaluate(gameTime));
}
