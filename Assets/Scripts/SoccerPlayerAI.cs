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

    [Header("Intelligent Attack Settings (지능형 공격 설정)")]
    public float shotTargetRadius = 4.0f;
    public float orbitDistance = 4.0f;

    // [핵심 로직 추가] =======================================================
    [Header("Avoidance Settings (우회 설정)")]
    [Tooltip("이 거리 안에 공이 있으면 우회를 고려합니다.")]
    public float avoidanceTriggerDistance = 1.5f;
    [Tooltip("공을 우회할 때 옆으로 얼마나 넓게 피할지 결정합니다.")]
    public float avoidanceSidewaysDistance = 1.5f;
    // ====================================================================

    [Header("Attack Settings (공격 실행 설정)")]
    public float tackleDistance = 2.0f;
    public float tackleForce = 150f;
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
    private Vector3 strategicTargetPosition; 
    private Vector3 finalMoveTarget; // [추가] 우회 경로까지 계산된 최종 이동 목표

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

    // [핵심 로직 변경] =======================================================
    private void FixedUpdate()
    {
        if (isRagDoll) return;

        // 1. 최종 목표 지점과 공 사이의 거리를 계산하여, 공이 경로를 방해하는지 확인
        Vector3 closestPointOnPath = FindClosestPointOnLineSegment(transform.position, strategicTargetPosition, ball.position);
        float distanceToPath = Vector3.Distance(ball.position, closestPointOnPath);
        
        // 2. 공이 경로에 너무 가깝고(방해물), AI가 공 뒤로 돌아가는 중이라면 (공이 AI와 최종 목표 사이에 있다면)
        bool isBallBetween = Vector3.Dot(strategicTargetPosition - transform.position, ball.position - transform.position) > 0;

        if (distanceToPath < avoidanceTriggerDistance && isBallBetween)
        {
            // 우회 경로 계산!
            // 공 옆으로 비껴갈 두 개의 후보 지점을 계산
            Vector3 dirToTarget = (strategicTargetPosition - transform.position).normalized;
            Vector3 sideDir = Vector3.Cross(dirToTarget, Vector3.up).normalized;

            Vector3 detourPoint1 = ball.position + sideDir * avoidanceSidewaysDistance;
            Vector3 detourPoint2 = ball.position - sideDir * avoidanceSidewaysDistance;

            // 두 지점 중 현재 위치에서 더 가까운 곳을 임시 목표로 설정
            if (Vector3.Distance(transform.position, detourPoint1) < Vector3.Distance(transform.position, detourPoint2))
            {
                finalMoveTarget = detourPoint1;
            }
            else
            {
                finalMoveTarget = detourPoint2;
            }
        }
        else
        {
            // 방해물이 없으면 원래 목표대로 직진
            finalMoveTarget = strategicTargetPosition;
        }
        
        // 3. 계산된 최종 이동 목표(finalMoveTarget)를 향해 이동
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
    // ====================================================================

    private void Update()
    {
        DecideState();
        GetStrategicPosition(); 

        if (!isRagDoll && currentState == AIState.ATTACKING)
        {
            float distanceToBall = Vector3.Distance(transform.position, ball.position);
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
    
    Vector3 GetAttackingPosition()
    {
        if (IsShotAligned())
        {
            return ball.position;
        }
        else
        {
            Vector3 attackDirection = (opponentGoal.position - ball.position).normalized;
            Vector3 orbitPosition = ball.position - attackDirection * orbitDistance;
            return orbitPosition;
        }
    }
    
    Vector3 GetDefendingPosition()
    {
        return transform.position;
    }

    private bool IsShotAligned()
    {
        if (opponentGoal == null || ball == null) return false;
        Vector3 aiPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 ballPos = new Vector3(ball.position.x, 0, ball.position.z);
        Vector3 goalPos = new Vector3(opponentGoal.position.x, 0, opponentGoal.position.z);
        Vector3 shotDirection = (ballPos - aiPos).normalized;
        Vector3 vectorToGoal = goalPos - aiPos;
        float projection = Vector3.Dot(vectorToGoal, shotDirection);
        if (projection < 0) return false;
        Vector3 closestPointOnLine = aiPos + shotDirection * projection;
        float distanceFromLine = Vector3.Distance(closestPointOnLine, goalPos);
        return distanceFromLine <= shotTargetRadius;
    }

    void Tackle()
    {
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

    // 유틸리티 함수이므로 Ragdoll System 밖으로 이동
    Vector3 FindClosestPointOnLineSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLengthSqr = lineDirection.sqrMagnitude;
        if (lineLengthSqr < 0.0001f) return lineStart;
        float t = Vector3.Dot(point - lineStart, lineDirection) / lineLengthSqr;
        t = Mathf.Clamp01(t);
        return lineStart + lineDirection * t;
    }

    #region Ragdoll System (이하 코드는 이전과 동일)
    // ... 이전과 동일한 래그돌 및 충돌 처리 함수들 ...
    private void DisableRagdoll() { isRagDoll = false; Vector3 original = spineRigid.position + new Vector3(0, -0.1f, 0); transform.position = original; anim.enabled = true; foreach (var j in joints) j.enableCollision = false; foreach (var c in ragColls) c.enabled = false; foreach (var r in ragsRigid) { r.detectCollisions = false; r.useGravity = false; } rb.detectCollisions = true; rb.useGravity = true; rb.linearVelocity = Vector3.zero; col.enabled = true; }
    private void EnableRagdoll() { isRagDoll = true; anim.enabled = false; foreach (var j in joints) j.enableCollision = true; foreach (var c in ragColls) c.enabled = true; foreach (var r in ragsRigid) { r.linearVelocity = rb.linearVelocity; r.detectCollisions = true; r.useGravity = true; } rb.detectCollisions = false; rb.useGravity = false; col.enabled = false; }
    private IEnumerator ResetRagDoll() { yield return new WaitForSeconds(ragdollResetTime); DisableRagdoll(); }
    private void OnCollisionEnter(Collision collision) { if (!isRagDoll && collision.gameObject.CompareTag("Obstacle")) { Rigidbody obsRigid = collision.gameObject.GetComponent<Rigidbody>(); obsRigid.freezeRotation = false; Vector3 dir = (transform.position - obsRigid.position).normalized; obsRigid.AddForce(dir * rb.linearVelocity.magnitude, ForceMode.VelocityChange); EnableRagdoll(); rb.AddForce(-dir * rb.linearVelocity.magnitude, ForceMode.VelocityChange); if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine); ragDollCoroutine = StartCoroutine(ResetRagDoll()); } else if (collision.gameObject.CompareTag("Ball")) { Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>(); if (ballRb == null) return; float ballSpeed = ballRb.linearVelocity.magnitude; if (ballSpeed >= ballSpeedRagdollThreshold) { EnableRagdoll(); Vector3 dir = transform.position - collision.gameObject.transform.position; dir.Normalize(); foreach (var r in ragsRigid) { r.AddForce(dir * ballCollisionForce, ForceMode.Impulse); } if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine); ragDollCoroutine = StartCoroutine(ResetRagDoll()); } } }
    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos() 
    { 
        if (ball == null || opponentGoal == null) return; 
        if (!Application.isPlaying) return; 
        
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"-- {currentState.ToString()} --");

        // 최종 전략 목표(strategicTargetPosition)를 노란색 큰 구체로 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(strategicTargetPosition, 0.5f);
        
        // [수정] 실제 이동 목표(finalMoveTarget)를 파란색 작은 구체와 선으로 표시
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(finalMoveTarget, 0.3f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, finalMoveTarget); 

        // 슛 각도 관련 시각화
        if (currentState == AIState.ATTACKING) { Gizmos.color = Color.green; Gizmos.DrawWireSphere(opponentGoal.position, shotTargetRadius); Vector3 shotDirection = (ball.position - transform.position).normalized; Gizmos.DrawRay(transform.position, shotDirection * 30f); if (IsShotAligned()) { Gizmos.color = Color.green; } else { Gizmos.color = Color.red; } Gizmos.DrawLine(transform.position, ball.position); }
    }
#endif
}