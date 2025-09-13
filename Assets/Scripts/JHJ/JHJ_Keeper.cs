// 다이빙 초안

using System.Collections;
using System.Linq;
using UnityEngine;

public class JHJ_Keeper : MonoBehaviour
{
    // Component
    private Animator anim;
    private Rigidbody rb;
    private Rigidbody[] ragsRigid;
    private CharacterJoint[] joints;
    private Collider col;
    private Collider[] ragColls;

    [SerializeField] private Rigidbody spineRigid;
    [SerializeField] private Rigidbody legRigid;
    [SerializeField] private Transform targetPos;

    // Const
    private const string WALKANIM = "isWalk";
    private string[] DanceANIM = { "isTwerk", "isSilly", "isBreakDance", "isSlide" };
    private const string ENDANIM = "end";

    // Status
    private Vector3 upDivingOffset = new Vector3(0, 0.6f, 0);
    private Vector3 downDivingOffset = new Vector3(0, 0.14f, 0);

    // State - 골키퍼 전용
    private bool isRagDoll = false;
    private bool isDiving = false;
    private Coroutine ragDollCoroutine;
    private Coroutine divingCoroutine;

    // 골키퍼 세레머니만 유지
    private bool isCeremony = false;
    private int danceCount = 0;

    // JHJ 크로스/킥 시스템용 설정
    [Header("== JHJ 크로스/킥 시스템 연동 ==")]
    [Tooltip("공의 KickSystem 컴포넌트 참조")]
    [SerializeField] private KickSystem ballKickSystem;

    [Tooltip("공의 CrossSystem 컴포넌트 참조")]
    [SerializeField] private CrossSystem ballCrossSystem;

    [Tooltip("다이빙 반응 시간 (초) - 킥 후 이 시간 후에 다이빙")]
    [SerializeField] private float divingReactionTime = 0.3f;

    [Tooltip("다이빙 할 확률 (0~1)")]
    [SerializeField] private float divingChance = 0.8f;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        ragsRigid = GetComponentsInChildren<Rigidbody>()
          .Where(r => r != rb)
          .ToArray();

        col = GetComponent<Collider>();
        ragColls = GetComponentsInChildren<Collider>()
          .Where(c => c != col)
          .ToArray();

        joints = GetComponentsInChildren<CharacterJoint>();

        DisableRagdoll();
        danceCount = System.Enum.GetValues(typeof(DanceType)).Length;
    }

    private void Start()
    {
        // PenaltyManager 사용 씬인지 확인
        if (PenaltyManager.Instance != null)
        {
            // PKH 페널티킥 게임 씬
            PenaltyManager.Instance.ChangeKickerEvent += PenaltyManager_ChangeKickerEvent;
            InitializeAsKeeper();
        }
        else
        {
            // JHJ 크로스/킥 시스템 씬
            Debug.Log("[JHJ_Keeper] JHJ 크로스/킥 시스템 씬에서 실행 중");
            InitializeForCrossKickSystem();
        }
    }

    private void InitializeForCrossKickSystem()
    {
        // 공의 KickSystem과 CrossSystem 자동 찾기
        if (ballKickSystem == null)
        {
            ballKickSystem = FindFirstObjectByType<KickSystem>();
        }
        if (ballCrossSystem == null)
        {
            ballCrossSystem = FindFirstObjectByType<CrossSystem>();
        }

        // 킥 이벤트 구독
        if (ballKickSystem != null)
        {
            ballKickSystem.OnKickExecuted += OnPlayerKicked;
            Debug.Log("[JHJ_Keeper] KickSystem 이벤트 구독 완료");
        }
        else
        {
            Debug.LogWarning("[JHJ_Keeper] KickSystem을 찾을 수 없습니다!");
        }

        // 골키퍼 기본 상태로 초기화
        StartCoroutine(InitBasic());
    }

    private void OnPlayerKicked(Vector3 kickTarget)
    {
        Debug.Log($"[JHJ_Keeper] 플레이어가 킥했습니다! 목표: {kickTarget}");

        // 다이빙 확률 체크
        if (Random.Range(0f, 1f) > divingChance)
        {
            Debug.Log("[JHJ_Keeper] 다이빙하지 않기로 결정");
            return;
        }

        // 반응 시간 후 다이빙 시작
        StartCoroutine(DelayedDiving(kickTarget));
    }

    private IEnumerator DelayedDiving(Vector3 kickTarget)
    {
        yield return new WaitForSeconds(divingReactionTime);

        if (!isDiving)
        {
            Debug.Log("[JHJ_Keeper] 다이빙 시작!");
            StartDiving(kickTarget);
        }
    }

    private void StartDiving(Vector3 kickTarget)
    {
        if (isDiving) return;

        if (divingCoroutine != null) StopCoroutine(divingCoroutine);
        divingCoroutine = StartCoroutine(DivingCoroutine(kickTarget));
    }

    private void PenaltyManager_ChangeKickerEvent(object sender, bool e)
    {
        if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
        if (divingCoroutine != null) StopCoroutine(divingCoroutine);

        InitializeAsKeeper();
    }

    private void InitializeAsKeeper()
    {
        StartCoroutine(Init());
    }

    private IEnumerator InitBasic()
    {
        DisableRagdoll();
        yield return null;

        isDiving = false;
        rb.isKinematic = false;
        isRagDoll = false;
        anim.enabled = true;

        if (isCeremony)
        {
            isCeremony = false;
            anim.SetTrigger(ENDANIM);
        }

        anim.SetBool(WALKANIM, false);
    }

    private IEnumerator Init()
    {
        DisableRagdoll();
        yield return null;

        anim.enabled = false;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;

        // PenaltyManager가 null이 아닌지 확인
        if (PenaltyManager.Instance != null)
        {
            // 항상 골키퍼 포지션으로 설정
            rb.position = PenaltyManager.Instance.goalKeeperPos;
            rb.rotation = Quaternion.Euler(PenaltyManager.Instance.goalKeeperRotate);
            transform.position = PenaltyManager.Instance.goalKeeperPos;
            transform.rotation = Quaternion.Euler(PenaltyManager.Instance.goalKeeperRotate);
        }

        yield return null;

        isDiving = false;
        rb.isKinematic = false;
        isRagDoll = false;
        anim.enabled = true;

        if (isCeremony)
        {
            isCeremony = false;
            anim.SetTrigger(ENDANIM);
        }

        anim.SetBool(WALKANIM, false);
    }

    private void FixedUpdate()
    {
        // PenaltyManager가 있는 씬에서만 기존 로직 실행
        if (PenaltyManager.Instance != null)
        {
            if (PenaltyManager.Instance.isGameEnd) return;
            if (PenaltyManager.Instance.isComplete) return;
        }

        if (isCeremony) return;

        // JHJ 크로스/킥 시스템에서는 자동 다이빙 비활성화
        // 플레이어 킥 이벤트에 의해서만 다이빙 실행
    }

    private void Update()
    {
        // PenaltyManager가 있는 씬에서만 세레머니 체크
        if (PenaltyManager.Instance != null)
        {
            if (PenaltyManager.Instance.isGameEnd) return;
            if (isCeremony) return;

            if (PenaltyManager.Instance.isCeremonyTime)
            {
                Ceremony();
                return;
            }
        }
    }

    private void DisableRagdoll()
    {
        isRagDoll = false;

        // spineRigid null 체크 추가
        if (spineRigid != null)
        {
            Vector3 original = spineRigid.position + new Vector3(0, -0.1f, 0);
            transform.position = original;
        }

        anim.enabled = true;
        anim.Rebind();
        anim.Update(0f);

        foreach (var j in joints)
        {
            j.enableCollision = false;
        }

        foreach (var c in ragColls)
        {
            c.enabled = false;
        }

        foreach (var r in ragsRigid)
        {
            r.isKinematic = true;
            r.detectCollisions = false;
            r.useGravity = false;
        }

        rb.isKinematic = false;
        rb.detectCollisions = true;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        col.enabled = true;
    }

    private void EnableRagdoll()
    {
        isRagDoll = true;

        anim.enabled = false;

        foreach (var j in joints)
        {
            j.enableCollision = true;
        }

        foreach (var c in ragColls)
        {
            c.enabled = true;
        }

        foreach (var r in ragsRigid)
        {
            r.isKinematic = false;
            r.linearVelocity = Vector3.zero;
            r.detectCollisions = true;
            r.useGravity = true;
        }

        rb.isKinematic = true;
        rb.detectCollisions = false;
        rb.useGravity = false;
        col.enabled = false;
    }

    private IEnumerator ResetRagDoll()
    {
        yield return new WaitForSeconds(2.5f);
        DisableRagdoll();
    }

    private IEnumerator DivingCoroutine(Vector3 kickTarget)
    {
        isDiving = true;
        EnableRagdoll();

        // kickTarget을 기준으로 다이빙 방향 계산
        Vector3 goalKeeperPos = transform.position;
        Vector3 ballTargetPos = kickTarget;

        bool isCenter = Mathf.Abs(goalKeeperPos.x - ballTargetPos.x) <= 0.3f;
        bool isLeft = ballTargetPos.x > goalKeeperPos.x;
        bool upDiving = Random.Range(0, 2) == 0;

        Vector3 dir = Vector3.zero;
        float power = 175f;

        if (!isCenter)
        {
            if (isLeft) dir = -transform.right;  // 왼쪽으로 다이빙
            else dir = transform.right;          // 오른쪽으로 다이빙
        }
        else
        {
            dir = (ballTargetPos - goalKeeperPos).normalized;
            power = 125f;
        }

        if (upDiving && spineRigid != null)
        {
            dir = (upDivingOffset + dir).normalized;
            spineRigid.AddForce(dir * power, ForceMode.Impulse);
        }
        else if (legRigid != null)
        {
            dir = (downDivingOffset + dir).normalized;
            legRigid.AddForce(dir * power, ForceMode.Impulse);
        }

        yield return new WaitForSeconds(2.5f);
        isDiving = false;
        DisableRagdoll();
    }

    // 골키퍼 세레머니만 유지 (PenaltyManager 있는 씬에서만)
    public void Ceremony()
    {
        // PenaltyManager null 체크 추가
        if (PenaltyManager.Instance == null) return;

        // 골을 막았을 때만 세레머니 실행
        if (!PenaltyManager.Instance.isGoal && PenaltyManager.Instance.isPlayerKick)
        {
            isCeremony = true;
            CeremonyStart();
        }
    }

    private void CeremonyStart()
    {
        Debug.Log("골키퍼 세레머니 시작 - 선방!");
        StartCoroutine(CeremonyCoroutine());
    }

    private IEnumerator CeremonyCoroutine()
    {
        if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
        if (divingCoroutine != null) StopCoroutine(divingCoroutine);

        DisableRagdoll();
        anim.enabled = false;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;

        yield return null;

        rb.isKinematic = false;
        isRagDoll = false;
        anim.enabled = true;

        int num = Random.Range(0, danceCount);
        anim.SetTrigger(DanceANIM[num]);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (PenaltyManager.Instance != null && PenaltyManager.Instance.isComplete) return;
        if (isRagDoll || isDiving) return;

        if (collision.gameObject.CompareTag("Ball") || collision.gameObject.CompareTag("Player"))
        {
            EnableRagdoll();
            Vector3 dir = (transform.position - collision.gameObject.transform.position).normalized;

            foreach (var r in ragsRigid)
            {
                r.AddForce(dir * 20, ForceMode.Impulse);
            }

            if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
            ragDollCoroutine = StartCoroutine(ResetRagDoll());
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (PenaltyManager.Instance != null)
        {
            PenaltyManager.Instance.ChangeKickerEvent -= PenaltyManager_ChangeKickerEvent;
        }

        if (ballKickSystem != null)
        {
            ballKickSystem.OnKickExecuted -= OnPlayerKicked;
        }
    }
}