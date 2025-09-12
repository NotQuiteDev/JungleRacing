using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// 온필드 축구 게임용 플레이어 컨트롤러
/// 레그돌 물리와 다이빙 공격을 지원하는 축구 선수 캐릭터
/// </summary>
public class JHJ_Soccer : MonoBehaviour
{
    #region Singleton Pattern
    /// <summary>
    /// 싱글톤 인스턴스 - 게임 내에서 하나의 JHJ_Soccer 인스턴스만 존재하도록 보장
    /// </summary>
    public static JHJ_Soccer Instance { get; private set; }
    #endregion

    #region Components
    /// <summary>캐릭터 애니메이션을 제어하는 컴포넌트</summary>
    private Animator anim;

    /// <summary>캐릭터의 물리 시뮬레이션을 담당하는 메인 Rigidbody</summary>
    private Rigidbody rb;

    /// <summary>레그돌 시뮬레이션을 위한 관절 연결 컴포넌트들</summary>
    private CharacterJoint[] joints;

    /// <summary>메인 캐릭터의 충돌 감지 컴포넌트</summary>
    private Collider col;

    /// <summary>레그돌 상태에서 사용되는 하위 콜라이더들 (팔, 다리 등의 개별 콜라이더)</summary>
    private Collider[] ragColls;

    /// <summary>레그돌 상태에서 사용되는 하위 Rigidbody들 (팔, 다리 등의 개별 물리체)</summary>
    private Rigidbody[] ragsRigid;

    /// <summary>공격(다이빙) 시 목표로 하는 위치 (보통 골대나 공의 위치)</summary>
    [SerializeField] private Transform targetPos;

    /// <summary>상체 다이빙 시 힘을 가할 척추 부위의 Rigidbody</summary>
    [SerializeField] private Rigidbody spineRigid;

    /// <summary>하체 다이빙 시 힘을 가할 다리 부위의 Rigidbody</summary>
    [SerializeField] private Rigidbody legRigid;
    #endregion

    #region Animation Constants
    /// <summary>걷기 애니메이션 파라미터 이름</summary>
    private const string WALKANIM = "isWalk";
    #endregion

    #region State Variables
    /// <summary>현재 레그돌 상태인지 확인하는 플래그</summary>
    private bool isRagDoll = false;

    /// <summary>현재 공격(다이빙) 액션 실행 중인지 확인하는 플래그</summary>
    private bool isAttack = false;

    /// <summary>외부에서 레그돌 상태를 확인할 수 있는 읽기 전용 프로퍼티</summary>
    public bool IsRagDoll => isRagDoll;

    /// <summary>현재 실제 플레이어 위치 (레그돌 상태일 때는 척추 위치, 일반 상태일 때는 transform 위치)</summary>
    public Vector3 ActualPosition => isRagDoll && spineRigid != null ? spineRigid.position : transform.position;
    #endregion

    #region Status & Settings
    /// <summary>캐릭터의 이동 속도</summary>
    [SerializeField] private float speed = 5f;

    /// <summary>현재 공격 쿨다운 시간 (초)</summary>
    private float curAttackDelay = 0f;

    /// <summary>공격 후 재사용까지 기다려야 하는 시간 (초)</summary>
    private float attackDelay = 2f;

    /// <summary>상체 다이빙 시 목표 위치에 추가할 오프셋 (위쪽 다이빙용)</summary>
    private Vector3 upDivingOffset = new Vector3(0, 1.4f, 0);

    /// <summary>하체 다이빙 시 목표 위치에 추가할 오프셋 (아래쪽 다이빙용)</summary>
    private Vector3 downDivingOffset = new Vector3(0, 0.34f, 0);
    #endregion

    #region Coroutine References
    /// <summary>레그돌 리셋을 처리하는 코루틴의 참조</summary>
    private Coroutine ragDollCoroutine;

    /// <summary>공격 액션을 처리하는 코루틴의 참조</summary>
    private Coroutine attackCoroutine;
    #endregion

    #region Unity Lifecycle Methods
    /// <summary>
    /// 오브젝트 초기화 시점에 호출
    /// 싱글톤 패턴 구현 및 필요한 컴포넌트들을 찾아서 캐싱
    /// </summary>
    private void Awake()
    {
        // 싱글톤 패턴: 첫 번째 인스턴스만 유지
        if (Instance == null) Instance = this;

        // 메인 캐릭터의 컴포넌트들 찾기
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // 레그돌용 하위 Rigidbody들 찾기 (메인 Rigidbody 제외)
        ragsRigid = GetComponentsInChildren<Rigidbody>()
            .Where(r => r != rb)  // 메인 Rigidbody는 제외
            .ToArray();

        // 레그돌용 하위 콜라이더들 찾기 (메인 콜라이더 제외)
        ragColls = GetComponentsInChildren<Collider>()
            .Where(c => c != col)  // 메인 콜라이더는 제외
            .ToArray();

        // 관절 연결 컴포넌트들 찾기
        joints = GetComponentsInChildren<CharacterJoint>();

        // 게임 시작 시 레그돌 비활성화 상태로 시작
        DisableRagdoll();
    }

    /// <summary>
    /// 게임 시작 시 호출 (다른 오브젝트들이 초기화된 후)
    /// 입력 매니저의 공격 이벤트에 구독
    /// </summary>
    private void Start()
    {
        // 입력 매니저의 공격 버튼 이벤트에 Attack 메서드 연결
        InputManager.Instance.OnAttack += (a, b) => Attack();
    }

    /// <summary>
    /// 물리 업데이트 주기마다 호출 (고정 시간 간격)
    /// 캐릭터의 이동과 회전을 처리
    /// </summary>
    private void FixedUpdate()
    {
        // 공격 중일 때는 이동 불가
        if (isAttack) return;

        // 입력 매니저에서 정규화된 이동 방향 가져오기
        Vector2 moveDir = InputManager.Instance.MoveDirNormalized();
        // 2D 입력을 3D 월드 좌표로 변환 (Y축은 0으로 고정)
        Vector3 dir = new Vector3(moveDir.x, 0, moveDir.y);

        if (dir == Vector3.zero)
        {
            // 입력이 없으면 걷기 애니메이션 중지
            anim.SetBool(WALKANIM, false);
        }
        else
        {
            // 입력이 있으면 걷기 애니메이션 재생
            anim.SetBool(WALKANIM, true);

            // Rigidbody를 사용한 물리 기반 이동
            rb.MovePosition(rb.position + dir * speed * Time.fixedDeltaTime);

            // 이동 방향으로 캐릭터 회전
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            rb.MoveRotation(targetRot);
        }
    }

    /// <summary>
    /// 매 프레임마다 호출되는 업데이트
    /// 공격 쿨다운 시간 관리
    /// </summary>
    private void Update()
    {
        // 공격 쿨다운 시간 감소
        curAttackDelay -= Time.deltaTime;

        // 쿨다운이 끝나면 공격 가능 상태로 변경
        if (curAttackDelay <= 0f)
        {
            isAttack = false;
        }
    }
    #endregion

    #region Ragdoll System
    /// <summary>
    /// 레그돌 비활성화 - 일반 캐릭터 컨트롤 모드로 전환
    /// 애니메이션을 다시 활성화하고 물리 시뮬레이션을 단순화
    /// </summary>
    private void DisableRagdoll()
    {
        isRagDoll = false;

        // 캐릭터 위치를 척추 위치 기준으로 보정 (레그돌에서 일반 모드로 전환 시 위치 동기화)
        Vector3 original = spineRigid.position + new Vector3(0, -0.1f, 0);
        transform.position = original;
        Debug.Log("스냅 처리됨");

        // 1. 애니메이션 시스템 재활성화
        anim.enabled = true;

        // 2. 관절 간 충돌 비활성화 (애니메이션 모드에서는 불필요)
        foreach (var j in joints)
        {
            j.enableCollision = false;
        }

        // 3. 레그돌용 개별 콜라이더들 비활성화
        foreach (var c in ragColls)
        {
            c.enabled = false;
        }

        // 4. 레그돌용 개별 Rigidbody들 물리 비활성화
        foreach (var r in ragsRigid)
        {
            r.isKinematic = true;        // 물리 영향 받지 않음
            r.detectCollisions = false;  // 충돌 감지 비활성화
            r.useGravity = false;        // 중력 비활성화
        }

        // 5. 메인 캐릭터 컨트롤러 활성화
        rb.isKinematic = false;      // 물리 영향 받음
        rb.detectCollisions = true;  // 충돌 감지 활성화
        rb.useGravity = true;        // 중력 활성화
        rb.linearVelocity = Vector3.zero;  // 속도 초기화
        col.enabled = true;          // 메인 콜라이더 활성화
    }

    /// <summary>
    /// 레그돌 활성화 - 물리 기반 인형 모드로 전환
    /// 애니메이션을 비활성화하고 개별 신체 부위가 독립적으로 물리 시뮬레이션
    /// </summary>
    private void EnableRagdoll()
    {
        isRagDoll = true;

        // 1. 애니메이션 시스템 비활성화 (물리가 애니메이션보다 우선)
        anim.enabled = false;

        // 2. 관절 간 충돌 활성화 (현실적인 신체 충돌)
        foreach (var j in joints)
        {
            j.enableCollision = true;
        }

        // 3. 레그돌용 개별 콜라이더들 활성화
        foreach (var c in ragColls)
        {
            c.enabled = true;
        }

        // 4. 레그돌용 개별 Rigidbody들 물리 활성화
        foreach (var r in ragsRigid)
        {
            r.isKinematic = false;       // 물리 영향 받음
            r.linearVelocity = Vector3.zero;  // 속도 초기화 (깔끔한 전환)
            r.detectCollisions = true;   // 충돌 감지 활성화
            r.useGravity = true;         // 중력 활성화
        }

        // 5. 메인 캐릭터 컨트롤러 비활성화 (개별 신체 부위가 제어)
        rb.isKinematic = true;       // 물리 영향 받지 않음
        rb.detectCollisions = false; // 충돌 감지 비활성화
        rb.useGravity = false;       // 중력 비활성화
        col.enabled = false;         // 메인 콜라이더 비활성화
    }

    /// <summary>
    /// 레그돌 상태를 일정 시간 후 자동으로 해제하는 코루틴
    /// </summary>
    /// <returns>코루틴 열거자</returns>
    private IEnumerator ResetRagDoll()
    {
        // 2초 대기
        yield return new WaitForSeconds(2f);

        // 레그돌 비활성화하여 일반 상태로 복귀
        DisableRagdoll();
    }
    #endregion

    #region Attack System
    /// <summary>
    /// 공격(다이빙) 액션 시작
    /// 쿨다운 체크 후 공격 코루틴 실행
    /// </summary>
    private void Attack()
    {
        // 이미 공격 중이면 무시
        if (isAttack) return;

        // 이전 공격 코루틴이 있다면 중지
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);

        // 새로운 공격 코루틴 시작
        attackCoroutine = StartCoroutine(AttackCoroutine());
    }

    /// <summary>
    /// 공격(다이빙) 액션의 전체 프로세스를 처리하는 코루틴
    /// 레그돌 활성화 → 물리력 적용 → 대기 → 레그돌 비활성화 순서로 진행
    /// </summary>
    /// <returns>코루틴 열거자</returns>
    private IEnumerator AttackCoroutine()
    {
        // 공격 상태 활성화 및 쿨다운 설정
        isAttack = true;
        curAttackDelay = attackDelay;

        // 다이빙을 위해 레그돌 활성화
        EnableRagdoll();

        Vector3 dir;

        // 입력에 따라 상체 다이빙 또는 하체 다이빙 선택
        if (InputManager.Instance.UpDiving())
        {
            // 상체 다이빙: 목표 위치 + 위쪽 오프셋
            dir = (upDivingOffset + targetPos.position);
            dir = (dir - transform.position).normalized;

            // 척추에 강한 힘 적용하여 상체로 다이빙
            spineRigid.AddForce(dir * 175f, ForceMode.Impulse);
        }
        else
        {
            // 하체 다이빙: 목표 위치 + 아래쪽 오프셋
            dir = (downDivingOffset + targetPos.position);
            dir = (dir - transform.position).normalized;

            // 다리에 강한 힘 적용하여 하체로 다이빙
            legRigid.AddForce(dir * 175f, ForceMode.Impulse);
        }

        // 다이빙 동작이 완료될 때까지 2초 대기
        yield return new WaitForSeconds(2f);

        // 공격 상태 해제
        isAttack = false;

        // 레그돌 비활성화하여 일반 상태로 복귀
        DisableRagdoll();
    }
    #endregion

    #region Collision Handling
    /// <summary>
    /// 다른 오브젝트와 충돌했을 때 호출되는 이벤트
    /// 공이나 적과 충돌 시 레그돌 효과 적용
    /// </summary>
    /// <param name="collision">충돌 정보</param>
    private void OnCollisionEnter(Collision collision)
    {
        // 공("Ball") 또는 적("Enemy") 태그와 충돌했는지 확인
        if (collision.gameObject.CompareTag("Ball") || collision.gameObject.CompareTag("Enemy"))
        {
            // 아직 레그돌 상태가 아니라면 레그돌 활성화
            if (!isRagDoll)
            {
                EnableRagdoll();
            }

            // 충돌 방향 계산 (충돌체로부터 멀어지는 방향)
            Vector3 dir = transform.position - collision.gameObject.transform.position;
            dir.Normalize();

            // 모든 레그돌 신체 부위에 밀려나는 힘 적용
            foreach (var r in ragsRigid)
            {
                r.AddForce(dir * 20, ForceMode.Impulse);
            }

            // 이전 레그돌 리셋 코루틴이 있다면 중지
            if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);

            // 새로운 레그돌 리셋 코루틴 시작
            ragDollCoroutine = StartCoroutine(ResetRagDoll());
        }
    }
    #endregion
}