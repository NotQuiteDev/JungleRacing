using System.Collections;
using UnityEngine;

// SoccerPlayerAI를 상속받아, 지정된 목표를 향해 태클하는 최종 컨트롤러
public class AIRagdollController : SoccerPlayerAI
{
    // 캐릭터 움직임 관련 컴포넌트
    private Rigidbody controllerRb;
    private Animator controllerAnim;

    [Header("2P Player Tackle Settings")]
    [Tooltip("태클할 목표(Target)를 지정합니다. 상대 플레이어 캐릭터를 연결하세요.")]
    [SerializeField] private Transform tackleTarget; // 사용자가 지정할 목표물!

    [Tooltip("상단 다이빙 시 조준 오프셋")]
    [SerializeField] private Vector3 upDivingOffset = new Vector3(0, 1.4f, 0);
    [Tooltip("하단 다이빙 시 조준 오프셋")]
    [SerializeField] private Vector3 downDivingOffset = new Vector3(0, 0.34f, 0);

    // 태클에 사용할 Rigidbody 부위들 (인스펙터에서 연결)
    [SerializeField] private Rigidbody spineRigidFor2P;
    [SerializeField] private Rigidbody legRigidFor2P;

    // 공격 딜레이
    private bool isAttack = false;
    private float curAttackDelay = 0f;
    private float attackDelay = 2f;


    // Awake는 부모의 것도 실행하고, 이 클래스에 필요한 것도 초기화
    void Awake()
    {
        base.Awake(); // 부모(SoccerPlayerAI)의 Awake 실행
        controllerRb = GetComponent<Rigidbody>();
        controllerAnim = GetComponent<Animator>();
    }

    // FixedUpdate는 AI 움직임 대신 플레이어 조작으로 덮어쓰기
    private new void FixedUpdate()
    {
        if (isAttack || GetIsRagdollState()) return;

        // IJKL 조작 로직
        Vector2 moveInput = Vector2.zero;
        if (Input.GetKey(KeyCode.I)) moveInput.y = 1;
        if (Input.GetKey(KeyCode.K)) moveInput.y = -1;
        if (Input.GetKey(KeyCode.J)) moveInput.x = -1;
        if (Input.GetKey(KeyCode.L)) moveInput.x = 1;
        Vector3 dir = new Vector3(moveInput.y, 0, -moveInput.x).normalized;

        if (dir.sqrMagnitude < 0.01f)
        {
            controllerAnim.SetBool("isWalk", false);
        }
        else
        {
            controllerAnim.SetBool("isWalk", true);
            controllerRb.MovePosition(controllerRb.position + dir * 5f * Time.fixedDeltaTime);
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            controllerRb.MoveRotation(targetRot);
        }
    }

    // Update도 AI 판단 로직 대신 플레이어 조작으로 덮어쓰기
    private new void Update()
    {
        if (curAttackDelay > 0) curAttackDelay -= Time.deltaTime;
        else isAttack = false;

        if (!isAttack && !GetIsRagdollState() && Input.GetKeyDown(KeyCode.RightShift))
        {
            Attack();
        }
    }

    /// <summary>
    /// 지정된 목표(TackleTarget)를 향해 점프/슬라이딩 태클을 실행합니다.
    /// </summary>
    private void Attack()
    {
        if (isAttack || GetIsRagdollState()) return;

        // 태클 목표가 설정되지 않았으면 경고를 출력하고 실행하지 않습니다.
        if (tackleTarget == null)
        {
            Debug.LogWarning("AIRagdollController의 Tackle Target이 지정되지 않았습니다!");
            return;
        }

        StartCoroutine(AttackCoroutine());
    }

    private IEnumerator AttackCoroutine()
    {
        isAttack = true;
        curAttackDelay = attackDelay;

        // 부모의 protected 함수를 호출하여 래그돌 상태로 전환
        EnableRagdoll();

        Vector3 dir;

        // 왼쪽 Shift 키로 점프/슬라이딩 구분
        if (Input.GetKey(KeyCode.LeftShift)) // 점프 태클 (상단 다이빙)
        {
            Debug.Log("2P JUMP TACKLE to " + tackleTarget.name);
            dir = (upDivingOffset + tackleTarget.position - transform.position).normalized;
            spineRigidFor2P.AddForce(dir * 150f, ForceMode.Impulse);
        }
        else // 슬라이딩 태클 (하단 다이빙)
        {
            Debug.Log("2P SLIDE TACKLE to " + tackleTarget.name);
            dir = (downDivingOffset + tackleTarget.position - transform.position).normalized;
            legRigidFor2P.AddForce(dir * 150f, ForceMode.Impulse);
        }

        // 부모의 ResetRagDoll 코루틴을 호출하여 일정 시간 뒤 일어나도록 함
        StartCoroutine(ResetRagDoll());
        yield return null;
    }
}