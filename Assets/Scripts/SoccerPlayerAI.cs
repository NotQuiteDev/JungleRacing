using System.Collections;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; 
#endif

public class SoccerPlayerAI : MonoBehaviour, IRagdollController
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

    [Header("Intelligent Attack Settings (지능형 공격 설정)")]
    public float shotTargetRadius = 4.0f;
    public float orbitDistance = 4.0f;

    [Header("Avoidance Settings (우회 설정)")]
    public float avoidanceTriggerDistance = 1.5f;
    public float avoidanceSidewaysDistance = 1.5f;

    [Header("Attack Settings (공격 실행 설정)")]
    public float tackleDistance = 2.0f;
    public float tackleForce = 150f;
    public float tackleAimLeadDistance = 2.0f;

    [Header("Defensive Tackle Settings (수비형 태클 설정)")]
    [Tooltip("AI가 수비형 태클을 사용하는지 여부입니다.")]
    public bool enableDefensiveTackle = true;
    [Tooltip("플레이어가 이 반경 안에 들어오고 특정 조건을 만족하면 태클을 시도합니다.")]
    public float defensiveTackleRadius = 3.5f;

    [Header("Ragdoll Settings (래그돌 설정)")]
    public float ballSpeedRagdollThreshold = 5f;
    public float ballCollisionForce = 5f;
    public float ragdollResetTime = 2.5f;
    // [추가된 부분] =======================================================
    [Tooltip("래그돌 상태로 상대방과 부딪혔을 때, 상대에게 가하는 힘의 크기입니다.")]
    public float collisionTacklePower = 25f;


    [Tooltip("일어난 후 잠시 동안 다른 충격에 넘어지지 않는 무적 시간(초)입니다.")]
    public float spawnInvincibilityDuration = 0.5f;
    private bool isInvincible = false; // 현재 무적 상태인지 확인하는 내부 변수


    [Tooltip("공격 태클 시 슛의 부정확도(오차)입니다. 0이면 완벽하게 중앙으로 찹니다.")]
    public float shotInaccuracy = 0.1f;
    // ====================================================================


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
    private Vector3 strategicTargetPosition;
    private Vector3 finalMoveTarget;
    public Transform respawnPoint;

    [Header("Map Bounds Settings (맵 경계 설정)")]
    [Tooltip("AI가 활동할 안전 영역으로 사용할 트리거 콜라이더입니다.")]
    public Collider safeZoneCollider;
    [Tooltip("맵 경계를 벗어났는지 확인하는 주기(초)입니다.")]
    public float boundsCheckInterval = 2.0f;

    void Start()
    {
        // 게임이 시작되면 경계 확인 코루틴을 실행합니다.
        StartCoroutine(CheckBoundsPeriodically());
    }

    void Awake()
    {
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
        Vector3 closestPointOnPath = FindClosestPointOnLineSegment(transform.position, strategicTargetPosition, ball.position);
        float distanceToPath = Vector3.Distance(ball.position, closestPointOnPath);
        bool isBallBetween = Vector3.Dot(strategicTargetPosition - transform.position, ball.position - transform.position) > 0;

        if (distanceToPath < avoidanceTriggerDistance && isBallBetween)
        {
            Vector3 dirToTarget = (strategicTargetPosition - transform.position).normalized;
            Vector3 sideDir = Vector3.Cross(dirToTarget, Vector3.up).normalized;
            Vector3 detourPoint1 = ball.position + sideDir * avoidanceSidewaysDistance;
            Vector3 detourPoint2 = ball.position - sideDir * avoidanceSidewaysDistance;
            finalMoveTarget = (Vector3.Distance(transform.position, detourPoint1) < Vector3.Distance(transform.position, detourPoint2)) ? detourPoint1 : detourPoint2;
        }
        else
        {
            finalMoveTarget = strategicTargetPosition;
        }

        Vector3 dir = (finalMoveTarget - transform.position).normalized;
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
        GetStrategicPosition();

        if (!isRagDoll && currentState == AIState.ATTACKING)
        {
            float distanceToBall = Vector3.Distance(transform.position, ball.position);

            // 1순위: 슛 각도가 나왔을 때 공을 향한 공격형 태클
            if (IsShotAligned() && distanceToBall <= tackleDistance)
            {
                Tackle(ball);
            }
            // 2순위: 플레이어가 위협적일 때 플레이어를 향한 수비형 태클
            else if (enableDefensiveTackle && opponent != null)
            {
                // 조건 1: 플레이어가 나보다 공에 더 가까운가?
                float playerDistToBall = Vector3.Distance(opponent.position, ball.position);
                if (playerDistToBall < distanceToBall)
                {
                    // 조건 2: 그 플레이어가 내 주변 반경 안에 있는가?
                    float distToPlayer = Vector3.Distance(transform.position, opponent.position);
                    if (distToPlayer <= defensiveTackleRadius)
                    {
                        // ===== [ 추가된 조건 3 ] =====
                        // 그리고 플레이어가 나보다 '우리 골대'에 더 가까워 위협적인가?
                        float playerDistToMyGoal = Vector3.Distance(opponent.position, myGoal.position);
                        float myDistToMyGoal = Vector3.Distance(transform.position, myGoal.position);
                        if (playerDistToMyGoal < myDistToMyGoal)
                        {
                            // 모든 조건 만족! 플레이어에게 태클!
                            Tackle(opponent);
                        }
                        // ============================
                    }
                }
            }
        }
    }

    void DecideState() { currentState = AIState.ATTACKING; }
    void GetStrategicPosition() { switch (currentState) { case AIState.ATTACKING: strategicTargetPosition = GetAttackingPosition(); break; case AIState.DEFENDING: strategicTargetPosition = GetDefendingPosition(); break; default: strategicTargetPosition = transform.position; break; } }
    Vector3 GetAttackingPosition() { if (IsShotAligned()) { return ball.position; } else { Vector3 attackDirection = (opponentGoal.position - ball.position).normalized; Vector3 orbitPosition = ball.position - attackDirection * orbitDistance; return orbitPosition; } }
    Vector3 GetDefendingPosition() { return transform.position; }
    private bool IsShotAligned() { if (opponentGoal == null || ball == null) return false; Vector3 aiPos = new Vector3(transform.position.x, 0, transform.position.z); Vector3 ballPos = new Vector3(ball.position.x, 0, ball.position.z); Vector3 goalPos = new Vector3(opponentGoal.position.x, 0, opponentGoal.position.z); Vector3 shotDirection = (ballPos - aiPos).normalized; Vector3 vectorToGoal = goalPos - aiPos; float projection = Vector3.Dot(vectorToGoal, shotDirection); if (projection < 0) return false; Vector3 closestPointOnLine = aiPos + shotDirection * projection; float distanceFromLine = Vector3.Distance(closestPointOnLine, goalPos); return distanceFromLine <= shotTargetRadius; }

    void Tackle(Transform target)
    {
        Vector3 diveDirection;
        if (target == ball)
        {
            Debug.Log("AI가 공을 향해 [공격형 태클]을 시도합니다!");

            // 1. 공에서 상대 골대를 향하는 기본 방향 계산
            Vector3 attackDirection = (opponentGoal.position - ball.position).normalized;

            // ===== [ 이 부분이 추가/수정되었습니다 ] =====
            // 2. 설정된 부정확도(shotInaccuracy)에 따라 랜덤한 오차를 적용
            if (shotInaccuracy > 0)
            {
                // Random.insideUnitSphere는 반지름 1인 구 안의 랜덤한 지점을 반환합니다.
                // 여기에 부정확도 값을 곱해서 오차의 범위를 조절합니다.
                Vector3 error = Random.insideUnitSphere * shotInaccuracy;
                attackDirection = (attackDirection + error).normalized; // 기존 방향에 오차를 더하고 다시 정규화
            }
            // ===========================================

            // 3. 최종적으로 오차가 적용된 방향으로 목표 지점 설정
            Vector3 targetPoint = ball.position + attackDirection * tackleAimLeadDistance;
            diveDirection = (targetPoint - transform.position).normalized;
        }
        else
        {
            // 수비형 태클: 플레이어를 직접 조준
            diveDirection = (target.position - transform.position).normalized;
        }

        EnableRagdoll();
        if (spineRigid != null)
        {
            spineRigid.AddForce(diveDirection * tackleForce, ForceMode.Impulse);
        }
        if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
        ragDollCoroutine = StartCoroutine(ResetRagDoll());
    }

    Vector3 FindClosestPointOnLineSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 point) { Vector3 lineDirection = lineEnd - lineStart; float lineLengthSqr = lineDirection.sqrMagnitude; if (lineLengthSqr < 0.0001f) return lineStart; float t = Vector3.Dot(point - lineStart, lineDirection) / lineLengthSqr; t = Mathf.Clamp01(t); return lineStart + lineDirection * t; }

    // [추가된 부분] =======================================================
    #region Public Communication Functions
    /// <summary>
    /// 외부에서 현재 래그돌 상태인지 확인할 수 있게 해주는 함수입니다.
    /// </summary>
    public bool GetIsRagdollState()
    {
        return isRagDoll;
    }

    /// <summary>
    /// 외부의 충격으로 래그돌이 되도록 명령받는 함수입니다. (플레이어가 와서 부딪혔을 때 호출됨)
    /// </summary>
    /// <param name="impactDirection">충격 방향</param>
    /// <param name="impactForce">충격량</param>
    public void TriggerRagdollByImpact(Vector3 impactDirection, float impactForce)
    {
        if (isRagDoll || isInvincible) return; // 이미 래그돌 상태거나 무적 상태면 무시

        Debug.Log("AI가 플레이어의 충격으로 래그돌이 됩니다!");
        EnableRagdoll();

        // 충격 지점(가슴)에 전달받은 힘을 가해 실감 나게 넘어지게 합니다.
        spineRigid.AddForce(impactDirection * impactForce, ForceMode.Impulse);

        if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
        ragDollCoroutine = StartCoroutine(ResetRagDoll());
    }
    #endregion
    // ====================================================================

    #region Ragdoll System
    private void DisableRagdoll()
    {
        isRagDoll = false; Vector3 original = spineRigid.position + new Vector3(0, -0.1f, 0); transform.position = original; anim.enabled = true; foreach (var j in joints) { j.enableCollision = false; }
        foreach (var c in ragColls) { c.enabled = false; }
        foreach (var r in ragsRigid) { r.detectCollisions = false; r.useGravity = false; }
        rb.detectCollisions = true; rb.useGravity = true; rb.linearVelocity = Vector3.zero; col.enabled = true;

        StartCoroutine(ResetInvincibility());

    }
    private IEnumerator ResetInvincibility()
    {
        isInvincible = true; // 무적 상태 시작
        yield return new WaitForSeconds(spawnInvincibilityDuration);
        isInvincible = false; // 설정된 시간이 지나면 무적 상태 해제
    }
    private void EnableRagdoll() { isRagDoll = true; anim.enabled = false; foreach (var j in joints) { j.enableCollision = true; } foreach (var c in ragColls) c.enabled = true; foreach (var r in ragsRigid) { r.linearVelocity = rb.linearVelocity; r.detectCollisions = true; r.useGravity = true; } rb.detectCollisions = false; rb.useGravity = false; col.enabled = false; }
    private IEnumerator ResetRagDoll() { yield return new WaitForSeconds(ragdollResetTime); DisableRagdoll(); }

    // [수정된 부분] =======================================================
    private void OnCollisionEnter(Collision collision)
    {
        if (isRagDoll) return;

        // 아래는 기존의 '서 있을 때'의 충돌 로직
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Rigidbody obsRigid = collision.gameObject.GetComponent<Rigidbody>(); obsRigid.freezeRotation = false; Vector3 dir = (transform.position - obsRigid.position).normalized; obsRigid.AddForce(dir * rb.linearVelocity.magnitude, ForceMode.VelocityChange); EnableRagdoll(); rb.AddForce(-dir * rb.linearVelocity.magnitude, ForceMode.VelocityChange); if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine); ragDollCoroutine = StartCoroutine(ResetRagDoll());
        }
        else if (collision.gameObject.CompareTag("Ball"))
        {
            Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>(); if (ballRb == null) return; float ballSpeed = ballRb.linearVelocity.magnitude; if (ballSpeed >= ballSpeedRagdollThreshold) { EnableRagdoll(); Vector3 dir = transform.position - collision.gameObject.transform.position; dir.Normalize(); foreach (var r in ragsRigid) { r.AddForce(dir * ballCollisionForce, ForceMode.Impulse); } if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine); ragDollCoroutine = StartCoroutine(ResetRagDoll()); }
        }
    }
    // [추가] '우편함' 역할을 할 Public 함수
    public void HandleRagdollCollision(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerRagdollController opponentPlayer = collision.gameObject.GetComponent<PlayerRagdollController>();
            if (opponentPlayer != null && !opponentPlayer.GetIsRagdollState())
            {
                Debug.Log("AI 태클 성공! 플레이어를 넘어뜨립니다.");
                Vector3 impactDirection = (collision.transform.position - transform.position).normalized;
                opponentPlayer.TriggerRagdollByImpact(impactDirection, collisionTacklePower);
            }
        }
    }
    
    /// <summary>
    /// 일정 주기마다 AI가 안전 영역 안에 있는지 확인하는 코루틴입니다.
    /// </summary>
    private IEnumerator CheckBoundsPeriodically()
    {
        // 게임이 실행되는 동안 무한 반복
        while (true)
        {
            // 설정된 시간만큼 기다립니다.
            yield return new WaitForSeconds(boundsCheckInterval);

            if (safeZoneCollider == null)
            {
                Debug.LogError("Safe Zone Collider가 지정되지 않았습니다!");
                // 코루틴을 멈춰서 더 이상 에러가 반복되지 않게 합니다.
                yield break; 
            }

            // 래그돌 상태든 아니든 항상 존재하는 몸통(spineRigid)의 위치를 확인합니다.
            Vector3 checkPosition = spineRigid.position;

            // 몸통의 위치가 안전 영역 콜라이더의 경계(bounds) 안에 있는지 확인합니다.
            bool isInside = safeZoneCollider.bounds.Contains(checkPosition);

            if (isInside)
            {
                // 안에 있다면 디버그 로그 출력
                Debug.Log($"[경계 확인] {gameObject.name}는 안전 영역 안에 있습니다.");
            }
            else
            {
                // 밖에 있다면 경고 로그를 출력하고 리셋 함수를 호출합니다.
                Debug.LogWarning($"[경계 확인] {gameObject.name}는 안전 영역 밖에 있습니다! 리스폰합니다.");
                ResetPositionAndState();
            }
        }
    }

    /// <summary>
    /// 캐릭터를 리스폰 위치로 옮기고 상태를 초기화합니다.
    /// </summary>
    private void ResetPositionAndState()
    {
        if (respawnPoint == null)
        {
            Debug.LogError("리스폰 위치(Respawn Point)가 지정되지 않았습니다!");
            return;
        }

        transform.position = respawnPoint.position;
        
        if (isRagDoll)
        {
            DisableRagdoll();
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    // ====================================================================
    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos() { if (ball == null || opponentGoal == null) return; if (!Application.isPlaying) return; UnityEditor.Handles.color = Color.white; UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"-- {currentState.ToString()} --"); Gizmos.color = Color.yellow; Gizmos.DrawSphere(strategicTargetPosition, 0.5f); Gizmos.color = Color.cyan; Gizmos.DrawSphere(finalMoveTarget, 0.3f); Gizmos.color = Color.red; Gizmos.DrawLine(transform.position, finalMoveTarget); if (currentState == AIState.ATTACKING) { Gizmos.color = Color.green; Gizmos.DrawWireSphere(opponentGoal.position, shotTargetRadius); Vector3 shotDirection = (ball.position - transform.position).normalized; Gizmos.DrawRay(transform.position, shotDirection * 30f); if (IsShotAligned()) { Gizmos.color = Color.green; } else { Gizmos.color = Color.red; } Gizmos.DrawLine(transform.position, ball.position); } }
#endif
}