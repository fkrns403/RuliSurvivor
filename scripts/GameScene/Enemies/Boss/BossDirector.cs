using System.Collections;
using UnityEngine;
using Cinemachine;

/// <summary>
/// 보스 등장 연출 컨트롤러.
/// 
/// 담당 기능:
/// - 보스 등장 시 CinemachineTargetGroup에 보스를 임시 추가한다.
/// - focusDuration 동안 보스를 카메라에 잡는다.
/// - 연출 종료 후 보스를 TargetGroup에서 제거한다.
/// - 카메라 Follow / LookAt을 원래 TargetGroup으로 복구한다.
/// - TargetGroup에 실수로 들어간 UI Transform을 제거한다.
/// - 보스 연출이 중간에 끊겨도 Cleanup을 수행한다.
/// 
/// 이번 수정 핵심:
/// - 보스 체력바나 Canvas UI가 TargetGroup에 들어가면 카메라가 UI 쪽을 바라볼 수 있다.
/// - 따라서 TargetGroup 정리 시 Canvas / RectTransform 계열 타겟을 제거한다.
/// - 연출 종료 후 vcam.Follow / vcam.LookAt을 targetGroup.transform으로 확실히 복구한다.
/// </summary>
[DisallowMultipleComponent]
public class BossDirector : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private CinemachineVirtualCamera vcam;
    [SerializeField] private CinemachineTargetGroup targetGroup;

    [Header("Focus")]
    [SerializeField] private float focusDuration = 2.5f;
    [SerializeField] private float bossWeight = 1f;
    [SerializeField] private float bossRadius = 2f;

    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Lens")]
    [SerializeField] private bool changeOrthoSizeDuringFocus = false;
    [SerializeField] private float bossFocusOrthoSize = 9f;

    [Header("Effects")]
    [SerializeField] private GameObject flameEffectPrefab;
    [SerializeField] private GameObject fireFieldObject;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private Coroutine currentSequence;
    private Transform currentBossTarget;
    private float originalOrthoSize;
    private bool hasOriginalOrthoSize;

    private void Awake()
    {
        ResolveReferences();
        CleanupTargetGroup();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CleanupTargetGroup();
    }

    private void OnDisable()
    {
        StopCurrentSequence();
        CleanupTargetGroup();
    }

    public void StartBossSequence(GameObject boss, BossDifficulty difficulty)
    {
        if (boss == null)
            return;

        ResolveReferences();

        StopCurrentSequence();

        currentSequence = StartCoroutine(BossFocusSequence(boss, difficulty));
    }

    private IEnumerator BossFocusSequence(GameObject boss, BossDifficulty difficulty)
    {
        Transform bossTransform = boss != null ? boss.transform : null;

        if (bossTransform == null)
            yield break;

        currentBossTarget = bossTransform;

        ResolveReferences();
        CleanupTargetGroup();
        EnsureCameraUsesTargetGroup();

        if (targetGroup == null)
        {
            SetupBossControllerDirectly(boss, difficulty);
            currentSequence = null;
            yield break;
        }

        AddOrRefreshPlayerTarget();
        AddBossTarget(bossTransform);

        if (changeOrthoSizeDuringFocus && vcam != null)
        {
            originalOrthoSize = vcam.m_Lens.OrthographicSize;
            hasOriginalOrthoSize = true;
            vcam.m_Lens.OrthographicSize = bossFocusOrthoSize;
        }

        if (flameEffectPrefab != null)
            Instantiate(flameEffectPrefab, bossTransform.position, Quaternion.identity);

        if (fireFieldObject != null)
            fireFieldObject.SetActive(true);

        yield return new WaitForSeconds(Mathf.Max(0f, focusDuration));

        RemoveBossTarget(bossTransform);
        RestoreCameraState();
        CleanupTargetGroup();

        if (boss != null && boss.activeInHierarchy)
            SetupBossControllerDirectly(boss, difficulty);

        currentBossTarget = null;
        currentSequence = null;
    }

    public void RemoveBossTarget(Transform boss)
    {
        if (boss == null)
            return;

        if (targetGroup == null)
            return;

        targetGroup.RemoveMember(boss);

        if (currentBossTarget == boss)
            currentBossTarget = null;

        RestoreCameraState();
    }

    public void CleanupTargetGroup()
    {
        if (targetGroup == null)
            return;

        RemoveUiTargetsFromTargetGroup();
        RemoveNullTargetsFromTargetGroup();
    }

    private void ResolveReferences()
    {
        if (vcam == null)
            vcam = FindObjectOfType<CinemachineVirtualCamera>(true);

        if (targetGroup == null)
            targetGroup = FindObjectOfType<CinemachineTargetGroup>(true);

        if (player == null && GameManager.Instance != null)
            player = GameManager.Instance.PlayerTransform;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    private void EnsureCameraUsesTargetGroup()
    {
        if (vcam == null || targetGroup == null)
            return;

        vcam.Follow = targetGroup.transform;
        vcam.LookAt = targetGroup.transform;
    }

    private void RestoreCameraState()
    {
        EnsureCameraUsesTargetGroup();

        if (hasOriginalOrthoSize && vcam != null)
        {
            vcam.m_Lens.OrthographicSize = originalOrthoSize;
            hasOriginalOrthoSize = false;
        }
    }

    private void AddOrRefreshPlayerTarget()
    {
        if (targetGroup == null || player == null)
            return;

        targetGroup.RemoveMember(player);
        targetGroup.AddMember(player, 1f, 0f);
    }

    private void AddBossTarget(Transform boss)
    {
        if (targetGroup == null || boss == null)
            return;

        targetGroup.RemoveMember(boss);
        targetGroup.AddMember(boss, bossWeight, bossRadius);
    }

    private void RemoveUiTargetsFromTargetGroup()
    {
        if (targetGroup == null)
            return;

        CinemachineTargetGroup.Target[] targets = targetGroup.m_Targets;

        for (int i = targets.Length - 1; i >= 0; i--)
        {
            Transform target = targets[i].target;

            if (target == null)
                continue;

            if (IsUiTransform(target))
            {
                Log($"TargetGroup에서 UI 타겟 제거: {target.name}");
                targetGroup.RemoveMember(target);
            }
        }
    }

    private void RemoveNullTargetsFromTargetGroup()
    {
        if (targetGroup == null)
            return;

        CinemachineTargetGroup.Target[] targets = targetGroup.m_Targets;

        for (int i = targets.Length - 1; i >= 0; i--)
        {
            Transform target = targets[i].target;

            if (target == null)
                continue;
        }
    }

    private bool IsUiTransform(Transform target)
    {
        if (target == null)
            return false;

        if (target.GetComponent<RectTransform>() != null)
            return true;

        if (target.GetComponentInParent<Canvas>() != null)
            return true;

        return false;
    }

    private void StopCurrentSequence()
    {
        if (currentSequence == null)
            return;

        StopCoroutine(currentSequence);
        currentSequence = null;

        if (currentBossTarget != null)
            RemoveBossTarget(currentBossTarget);

        currentBossTarget = null;
        RestoreCameraState();
    }

    private void SetupBossControllerDirectly(GameObject boss, BossDifficulty difficulty)
    {
        if (boss == null)
            return;

        BossController controller = boss.GetComponent<BossController>();

        if (controller != null)
            controller.Setup(difficulty);
    }

    private void Log(string message)
    {
        if (!verboseLog)
            return;

        Debug.Log($"BossDirector: {message}", this);
    }
}