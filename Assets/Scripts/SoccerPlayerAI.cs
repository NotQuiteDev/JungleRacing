using System.Collections;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; 
#endif

public class SoccerPlayerAI : MonoBehaviour
{
    // ================== [ 축구 AI용 상태 및 변수 ] ==================
    public enum AIState { ATTACKING, DEFENDING }
    [Header("AI Soccer Brain")]
    public AIState currentState;
    public Transform ball;
    public Transform opponent;
    public Transform myGoal;
    public Transform opponentGoal;
    [Tooltip("AI가 공 뒤를 잡을 때의 거리")]
    public float attackOffset = 1.5f;
    [Tooltip("AI가 공을 돌아갈 때 옆으로 벌리는 거리")]
    public float flankOffset = 3f;
    [Tooltip("수비 지점에 이 거리 안으로 도착하면 공으로 돌진합니다.")]
    public float rushTriggerDistance = 1.0f;

    // [추가된 변수] =======================================================
    [Tooltip("공이 이 속도 이상으로 부딪혔을 때만 래그돌이 활성화됩니다.")]
    public float ballSpeedRagdollThreshold = 5f; // 기본값 5f로 설정
    // ====================================================================

    // Component
    private Animator anim;
    private Rigidbody rb;
    private Rigidbody[] ragsRigid;
    [SerializeField] private Rigidbody spineRigid; 
    private CharacterJoint[] joints;
    private Collider col;
    private Collider[] ragColls;

    // Const
    private const string WALKANIM = "isWalk";

    // State
    private bool isRagDoll = false;

    // Status
    [SerializeField] private float speed = 5f;
    private Coroutine ragDollCoroutine;

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
        Vector3 targetPosition = GetStrategicPosition();
        Vector3 dir = (targetPosition - transform.position).normalized;
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
    }

    void DecideState()
    {
        Debug.Log("DecideState() 호출: 현재 상태를 결정해야 합니다.");
    }

    Vector3 GetStrategicPosition()
    {
        switch (currentState)
        {
            case AIState.ATTACKING:
                return GetAttackingPosition();
            case AIState.DEFENDING:
                return GetDefendingPosition();
            default:
                return transform.position;
        }
    }

    Vector3 GetAttackingPosition()
    {
        Debug.LogWarning("GetAttackingPosition() 호출: 공격 목표 위치를 계산해야 합니다.");
        return transform.position;
    }
    
    Vector3 GetDefendingPosition()
    {
        Debug.LogError("GetDefendingPosition() 호출: 수비 목표 위치를 계산해야 합니다.");
        return transform.position;
    }

    Vector3 FindClosestPointOnLineSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLengthSqr = lineDirection.sqrMagnitude;
        if (lineLengthSqr < 0.0001f) return lineStart;
        float t = Vector3.Dot(point - lineStart, lineDirection) / lineLengthSqr;
        t = Mathf.Clamp01(t);
        return lineStart + lineDirection * t;
    }
    
    #region Ragdoll System
    private void DisableRagdoll()
    {
        isRagDoll = false;
        Vector3 original = spineRigid.position + new Vector3(0, -0.1f, 0);
        transform.position = original;
        
        anim.enabled = true;
        
        foreach (var j in joints) { j.enableCollision = false; }
        foreach (var c in ragColls) { c.enabled = false; }
        foreach (var r in ragsRigid) { r.detectCollisions = false; r.useGravity = false; }
        
        rb.detectCollisions = true;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        col.enabled = true;
    }

    private void EnableRagdoll()
    {
        isRagDoll = true;
        anim.enabled = false;
        
        foreach(var j in joints) { j.enableCollision = true; }
        foreach(var c in ragColls) { c.enabled = true; }
        foreach (var r in ragsRigid) 
        { 
            r.linearVelocity = rb.linearVelocity; 
            r.detectCollisions = true; 
            r.useGravity = true; 
        }
        
        rb.detectCollisions = false;
        rb.useGravity = false;
        col.enabled = false;
    }

    private IEnumerator ResetRagDoll()
    {
        yield return new WaitForSeconds(2.5f);
        DisableRagdoll();
    }
    
    // [수정된 부분] =======================================================
    // 기존 OnCollisionEnter를 삭제하고 보내주신 플레이어의 충돌 코드로 교체했습니다.
    private void OnCollisionEnter(Collision collision)
    {
        if(!isRagDoll && collision.gameObject.CompareTag("Obstacle"))
        {
            Rigidbody obsRigid = collision.gameObject.GetComponent<Rigidbody>();
            obsRigid.freezeRotation = false;
            Vector3 dir = (transform.position - obsRigid.position).normalized;
            obsRigid.AddForce(dir * rb.linearVelocity.magnitude, ForceMode.VelocityChange);
            EnableRagdoll();
            rb.AddForce(-dir * rb.linearVelocity.magnitude, ForceMode.VelocityChange);
            if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
            ragDollCoroutine = StartCoroutine(ResetRagDoll());
        }
        else if(collision.gameObject.CompareTag("Ball"))
        {
            // 1. 부딪힌 공의 Rigidbody를 가져옵니다.
            Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
            if (ballRb == null) return; // 공에 Rigidbody가 없으면 무시

            // 2. 공의 현재 속력을 계산합니다.
            float ballSpeed = ballRb.linearVelocity.magnitude; // linearVelocity 대신 velocity 사용

            // 3. 공의 속력이 우리가 설정한 역치(threshold)보다 클 때만 래그돌을 활성화합니다.
            if (ballSpeed >= ballSpeedRagdollThreshold)
            {
                Debug.Log($"[AI], 공과 충돌! 공 속도: {ballSpeed:F2} m/s. 래그돌을 활성화합니다.");

                EnableRagdoll();
                Vector3 dir = transform.position - collision.gameObject.transform.position;
                dir.Normalize();
                foreach (var r in ragsRigid)
                {
                    r.AddForce(dir * 5, ForceMode.Impulse);
                }
                if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
                ragDollCoroutine = StartCoroutine(ResetRagDoll());
            }
            else
            {
                Debug.Log($"[AI], 공과 충돌했지만 속도가 느립니다. (속도: {ballSpeed:F2} m/s)");
            }
        }
    }
    // ====================================================================
    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (ball == null || opponent == null || myGoal == null || opponentGoal == null) return;
        if (!Application.isPlaying) return; 

        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"-- {currentState.ToString()} --");

        Vector3 targetPosition = GetStrategicPosition();
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, targetPosition);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(targetPosition, 0.5f);
        
        if (currentState == AIState.ATTACKING)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(ball.position, opponentGoal.position);
        }
        else if (currentState == AIState.DEFENDING)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(ball.position, myGoal.position);
        }

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(ball.position, 0.6f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(opponent.position, 1f);
    }
#endif
}