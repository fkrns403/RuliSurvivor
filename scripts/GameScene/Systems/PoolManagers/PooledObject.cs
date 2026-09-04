using UnityEngine;

/// <summary>
/// 풀링 오브젝트 공통 베이스(권장)
/// - PoolManager가 스폰/디스폰 시점에 콜백을 통일해서 호출하기 위해 사용
/// - 필수는 아니지만, 붙이면 초기화/정리가 명확해져서 버그가 줄어듦
/// </summary>
public class PooledObject : MonoBehaviour
{
    [SerializeField] private int poolIndex = -1;
    public int PoolIndex => poolIndex;

    public void SetPoolIndex(int index) => poolIndex = index;

    /// <summary>풀에서 꺼내질 때(재사용 포함) 호출되는 초기화 훅</summary>
    public virtual void OnSpawned() { }

    /// <summary>풀로 반환될 때 호출되는 정리 훅</summary>
    public virtual void OnDespawned() { }
}