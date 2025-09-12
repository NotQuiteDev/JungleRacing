using System.Collections;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; 
#endif

public class SoccerPlayerAI : MonoBehaviour
{
    // ... 기존 변수들은 동일 ...
    public enum AIState { ATTACKING, DEFENDING }
    [Header("AI Soccer Brain")]
    public AIState currentState;
    public Transform ball;
    public Transform opponent;
    public Transform myGoal;
    public Transform opponentGoal;

    [Header("AI General Settings (일반 설정)")]
    [SerializeField] private float speed = 5f;

    // [핵심 로직 추가] =======================================================
    [Header("Intelligent Attack Settings (지능형 공격 설정)")]
    [Tooltip("상대 골대를 기준으로 이 반경(원) 안에 슛 라인이 걸리면 공격을 결정합니다.")]
    public float shotTargetRadius = 4.0f;
    [Tooltip("슛 각도를 잡기 위해 공 주변을 맴돌 때 유지할 거리입니다.")]
    public float orbitDistance = 4.0f;
    // ====================================================================

    [Header("Attack Settings (공격 실행 설정)")]
    [Tooltip("슛 각도가 나왔을 때, 이 거리 안으로 공이 들어오면 태클을 실행합니다.")]
    public float tackleDistance = 2.0f;
    [Tooltip("태클 시 몸을 날리는 힘의 크기입니다.")]
    public float tackleForce = 150f;
    [Tooltip("태클 시 공보다 얼마나 앞을 조준할지 결정합니다.")]
    public float tackleAimLeadDistance = 2.0f;

    [Header("Ragdoll Settings (래그돌 설정)")]
    public float ballSpeedRagdollThreshold = 5f;
    public float ballCollisionForce = 5f;
    public float ragdollResetTime = 2.5f;

    // ... 내부 시스템 변수들은 동일 ...
    private Animator anim;
    private Rigidbody rb;
    private Rigidbody[] ragsRigid;
    [SerializeField] private Rigidbody spineRigid; 
    private CharacterJoint[] joints;
    private Collider col;
    private Collider[] ragColls;
    private const string WALKANIM = "isWalk";
    private bool isRagDoll = false;
    private Coroutine ragDollCoroutine;
    private Vector3 strategicTargetPosition; // 기즈모 표시를 위해 목표 위치를 멤버 변수로 저장

    void Awake()
    {
        // ... Awake 함수 내용은 동일 ...
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        ragsRigid = GetComponentsInChildren<Rigidbody>().Where(r => r != rb).ToArray();
        col = GetComponent<Collider>();
        ragColls = GetComponentsInChildren<Collider>().Where(c => c != col).ToArray();
        joints = GetComponentsInChildren<CharacterJoint>();
        DisableRagdoll();
    }

    private void FixedUpdate()
    {
        if (isRagDoll) return;
        // [수정] GetStrategicPosition() 대신 멤버 변수 사용
        Vector3 dir = (strategicTargetPosition - transform.position).normalized;
        if (dir.sqrMagnitude < 0.01f) 
        { 
            anim.SetBool(WALKANIM, false); 
        }
        else
        {
            anim.SetBool(WALKANIM, true);
            rb.MovePosition(rb.position + dir * speed * Time.fixedDeltaTime);
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            rb.MoveRotation(targetRot);
        }
    }

    private void Update()
    {
        DecideState();
        // [수정] 목표 위치 계산을 Update로 이동하여 매 프레임 최신 정보 반영
        GetStrategicPosition(); 

        // [핵심 로직 변경] 슛 각도와 태클 거리를 모두 만족할 때만 태클 실행
        if (!isRagDoll && currentState == AIState.ATTACKING)
        {
            float distanceToBall = Vector3.Distance(transform.position, ball.position);
            
            // 슛 각도가 확보되었고, 공이 태클 가능 거리 안에 있을 때만 태클!
            if (IsShotAligned() && distanceToBall <= tackleDistance)
            {
                Tackle();
            }
        }
    }

    void DecideState()
    {
        currentState = AIState.ATTACKING;
    }

    void GetStrategicPosition()
    {
        switch (currentState)
        {
            case AIState.ATTACKING:
                strategicTargetPosition = GetAttackingPosition();
                break;
            case AIState.DEFENDING:
                strategicTargetPosition = GetDefendingPosition();
                break;
            default:
                strategicTargetPosition = transform.position;
                break;
        }
    }

    // [핵심 로직 변경] =======================================================
    Vector3 GetAttackingPosition()
    {
        // 1. 슛 각도가 확보되었는지 확인
        if (IsShotAligned())
        {
            // 슛 각도 확보! 공을 향해 돌진하여 태클 준비
            Debug.Log("슛 각도 확보! 공으로 돌진!");
            return ball.position;
        }
        else
        {
            // 슛 각도 미확보. 공 주변을 맴돌며 최적의 위치 탐색
            Debug.Log("슛 각도 조준 중... 공 주변을 맴돕니다.");
            
            // 상대 골대 방향을 기준으로, 공 뒤쪽의 '이상적인 슛 위치'를 계산
            Vector3 attackDirection = (opponentGoal.position - ball.position).normalized;
            Vector3 orbitPosition = ball.position - attackDirection * orbitDistance;
            
            return orbitPosition;
        }
    }
    // ====================================================================
    
    Vector3 GetDefendingPosition()
    {
        return transform.position;
    }

    // [핵심 로직 추가] =======================================================
    /// <summary>
    /// AI-공을 잇는 직선이 상대 골대 영역(원)을 통과하는지 확인합니다.
    /// </summary>
    /// <returns>슛 각도가 나오면 true, 그렇지 않으면 false</returns>
    private bool IsShotAligned()
    {
        if (opponentGoal == null || ball == null) return false;

        // 1. AI 위치, 공 위치, 골대 위치를 편의상 변수에 저장 (Y축은 무시하여 2D 평면처럼 계산)
        Vector3 aiPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 ballPos = new Vector3(ball.position.x, 0, ball.position.z);
        Vector3 goalPos = new Vector3(opponentGoal.position.x, 0, opponentGoal.position.z);

        // 2. AI에서 공을 향하는 '슛 라인(방향)'을 계산
        Vector3 shotDirection = (ballPos - aiPos).normalized;

        // 3. AI 위치에서 골대를 향하는 벡터 계산
        Vector3 vectorToGoal = goalPos - aiPos;

        // 4. 골대를 향하는 벡터를 슛 라인에 투영(projection)하여, 슛 라인 상의 가장 가까운 점을 찾음
        float projection = Vector3.Dot(vectorToGoal, shotDirection);

        // 만약 투영 거리가 음수이면, 공이 AI보다 뒤에 있다는 뜻이므로 슛 각도가 아님
        if (projection < 0) return false;
        
        Vector3 closestPointOnLine = aiPos + shotDirection * projection;

        // 5. 그 가장 가까운 점과 실제 골대 사이의 거리를 계산
        float distanceFromLine = Vector3.Distance(closestPointOnLine, goalPos);

        // 6. 이 거리가 우리가 설정한 '골대 반경'보다 작거나 같으면, 슛 각도가 나온 것으로 판단!
        return distanceFromLine <= shotTargetRadius;
    }
    // ====================================================================

    void Tackle()
    {
        // Tackle 함수 로직은 이전과 동일 (강력한 태클)
        Vector3 attackDirection = (opponentGoal.position - ball.position).normalized;
        Vector3 targetPoint = ball.position + attackDirection * tackleAimLeadDistance;
        Vector3 diveDirection = (targetPoint - transform.position).normalized;
        EnableRagdoll();
        if (spineRigid != null)
        {
            spineRigid.AddForce(diveDirection * tackleForce, ForceMode.Impulse);
        }
        if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
        ragDollCoroutine = StartCoroutine(ResetRagDoll());
    }

    #region Ragdoll System (이하 코드는 이전과 동일)
    // ... 이전과 동일한 래그돌 및 충돌 처리 함수들 ...
    private void DisableRagdoll() { isRagDoll = false; Vector3 original = spineRigid.position + new Vector3(0, -0.1f, 0); transform.position = original; anim.enabled = true; foreach (var j in joints) j.enableCollision = false; foreach (var c in ragColls) c.enabled = false; foreach (var r in ragsRigid) { r.detectCollisions = false; r.useGravity = false; } rb.detectCollisions = true; rb.useGravity = true; rb.linearVelocity = Vector3.zero; col.enabled = true; }
    private void EnableRagdoll() { isRagDoll = true; anim.enabled = false; foreach (var j in joints) j.enableCollision = true; foreach (var c in ragColls) c.enabled = true; foreach (var r in ragsRigid) { r.linearVelocity = rb.linearVelocity; r.detectCollisions = true; r.useGravity = true; } rb.detectCollisions = false; rb.useGravity = false; col.enabled = false; }
    private IEnumerator ResetRagDoll() { yield return new WaitForSeconds(ragdollResetTime); DisableRagdoll(); }
    private void OnCollisionEnter(Collision collision) { if (!isRagDoll && collision.gameObject.CompareTag("Obstacle")) { Rigidbody obsRigid = collision.gameObject.GetComponent<Rigidbody>(); obsRigid.freezeRotation = false; Vector3 dir = (transform.position - obsRigid.position).normalized; obsRigid.AddForce(dir * rb.linearVelocity.magnitude, ForceMode.VelocityChange); EnableRagdoll(); rb.AddForce(-dir * rb.linearVelocity.magnitude, ForceMode.VelocityChange); if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine); ragDollCoroutine = StartCoroutine(ResetRagDoll()); } else if (collision.gameObject.CompareTag("Ball")) { Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>(); if (ballRb == null) return; float ballSpeed = ballRb.linearVelocity.magnitude; if (ballSpeed >= ballSpeedRagdollThreshold) { EnableRagdoll(); Vector3 dir = transform.position - collision.gameObject.transform.position; dir.Normalize(); foreach (var r in ragsRigid) { r.AddForce(dir * ballCollisionForce, ForceMode.Impulse); } if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine); ragDollCoroutine = StartCoroutine(ResetRagDoll()); } } }
    #endregion

#if UNITY_EDITOR
    // [핵심 로직 추가] 기즈모를 통해 AI의 '생각'을 시각화합니다.
    private void OnDrawGizmos() 
    { 
        if (ball == null || opponentGoal == null) return; 
        if (!Application.isPlaying) return; 
        
        // AI의 목표 지점 표시
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"-- {currentState.ToString()} --");
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(strategicTargetPosition, 0.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, strategicTargetPosition); 
        
        // 슛 각도 관련 시각화
        if (currentState == AIState.ATTACKING)
        {
            // 1. 상대 골대의 목표 원 그리기
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(opponentGoal.position, shotTargetRadius);

            // 2. AI에서 공을 관통하는 '슛 라인' 그리기
            Vector3 shotDirection = (ball.position - transform.position).normalized;
            Gizmos.DrawRay(transform.position, shotDirection * 30f);

            // 3. 슛 각도 확보 여부에 따라 라인 색상 변경
            if (IsShotAligned())
            {
                Gizmos.color = Color.green; // 슛 가능: 녹색
            }
            else
            {
                Gizmos.color = Color.red; // 슛 불가능: 빨간색
            }
            Gizmos.DrawLine(transform.position, ball.position);
        }
    }
#endif
}