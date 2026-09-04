using System.Collections;
using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class AutoBindTargetGroup : MonoBehaviour
{
    [Header("TargetGroup 설정")]
    [SerializeField] private CinemachineTargetGroup targetGroup;

    [Header("플레이어 탐색 설정")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float retrySeconds = 0.2f;
    [SerializeField] private float playerWeight = 1f;
    [SerializeField] private float playerRadius = 0f;

    private CinemachineVirtualCamera vcam;
    private Transform player;
    private Coroutine bindRoutine;

    private void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
    }

    private void OnEnable()
    {
        BindFollowTarget();

        if (bindRoutine != null)
            StopCoroutine(bindRoutine);

        bindRoutine = StartCoroutine(BindPlayerWhenReady());
    }

    private void OnDisable()
    {
        if (bindRoutine != null)
        {
            StopCoroutine(bindRoutine);
            bindRoutine = null;
        }
    }

    private void BindFollowTarget()
    {
        if (vcam == null)
            vcam = GetComponent<CinemachineVirtualCamera>();

        if (targetGroup == null)
            targetGroup = FindObjectOfType<CinemachineTargetGroup>(true);

        if (vcam == null || targetGroup == null)
            return;

        vcam.Follow = targetGroup.transform;
        vcam.LookAt = null;
    }

    private IEnumerator BindPlayerWhenReady()
    {
        while (true)
        {
            if (player == null)
            {
                GameObject obj = GameObject.FindGameObjectWithTag(playerTag);

                if (obj != null)
                    player = obj.transform;
            }

            if (player != null)
            {
                EnsureSinglePlayerMember(player, playerWeight, playerRadius);
                yield break;
            }

            yield return new WaitForSeconds(retrySeconds);
        }
    }

    private void EnsureSinglePlayerMember(Transform target, float weight, float radius)
    {
        if (targetGroup == null || target == null)
            return;

        for (int i = targetGroup.m_Targets.Length - 1; i >= 0; i--)
        {
            Transform t = targetGroup.m_Targets[i].target;

            if (t == null || t.CompareTag(playerTag))
                targetGroup.RemoveMember(t);
        }

        targetGroup.AddMember(target, weight, radius);
    }
}