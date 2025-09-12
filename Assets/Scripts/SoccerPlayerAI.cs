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
        AIState previousState = currentState;

        // 1. 경기장의 중앙 지점을 계산합니다.
        Vector3 fieldCenter = (myGoal.position + opponentGoal.position) / 2f;
        // 2. 경기장 중앙에서 '우리 골대'를 향하는 방향 벡터를 구합니다. 이것이 "우리 진영"의 기준이 됩니다.
        Vector3 myHalfDirection = (myGoal.position - fieldCenter).normalized;
        // 3. 경기장 중앙에서 '공'을 향하는 방향 벡터를 구합니다.
        Vector3 ballDirectionFromCenter = (ball.position - fieldCenter).normalized;

        // 4. 내적(Dot Product)을 사용해 두 벡터가 같은 방향을 향하는지 확인합니다.
        // 결과가 양수(+)이면 공이 우리 진영에 있다는 뜻입니다.
        bool isBallInMyHalf = Vector3.Dot(myHalfDirection, ballDirectionFromCenter) > 0;

        // 5. 최종 상태를 결정합니다.
        if (isBallInMyHalf)
        {
            // 공이 우리 진영에 있다면, 무조건 수비 상태가 됩니다.
            currentState = AIState.DEFENDING;
        }
        else // 공이 상대 진영에 있다면
        {
            // 기존처럼 공과의 거리를 비교하여 공격/수비를 결정합니다.
            float myDistanceToBall = Vector3.Distance(transform.position, ball.position);
            float opponentDistanceToBall = Vector3.Distance(opponent.position, ball.position);

            if (myDistanceToBall <= opponentDistanceToBall)
            {
                currentState = AIState.ATTACKING;
            }
            else
            {
                currentState = AIState.DEFENDING;
            }
        }

        if (previousState != currentState)
        {
            Debug.Log($"상태 변경: {previousState} -> {currentState} (공이 우리 진영에?: {isBallInMyHalf})");
        }
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
        Vector3 directionToGoal = (opponentGoal.position - ball.position).normalized;
        Vector3 finalTargetPosition = ball.position - directionToGoal * attackOffset;
        
        // Dot Product를 사용하여 방향과 관계없이 '공 뒤에 있는지'를 판단하는 것이 더 범용적입니다.
        Vector3 directionFromMeToBall = (ball.position - transform.position).normalized;
        bool isBehindTheBall = Vector3.Dot(directionToGoal, directionFromMeToBall) < 0; // 내적이 음수면 서로 반대 방향 = 올바른 위치

        if (!isBehindTheBall) // 올바른 위치가 아니라면 (공을 등지고 있다면)
        {
            Vector3 sideDirection = Vector3.Cross(directionToGoal, Vector3.up).normalized;
            Vector3 directionToBallFromMe = (transform.position - ball.position).normalized;
            Vector3 leftFlankPosition = ball.position + (sideDirection * flankOffset) + (directionToBallFromMe * flankOffset * 0.5f);
            Vector3 rightFlankPosition = ball.position - (sideDirection * flankOffset) + (directionToBallFromMe * flankOffset * 0.5f);
            return (Vector3.Distance(transform.position, leftFlankPosition) < Vector3.Distance(transform.position, rightFlankPosition)) ? leftFlankPosition : rightFlankPosition;
        }
        else 
        { 
            return finalTargetPosition; 
        }
    }
    
    Vector3 GetDefendingPosition()
    {
        Vector3 directionToMyGoal = (myGoal.position - ball.position).normalized;
        Vector3 directionFromMeToBall = (ball.position - transform.position).normalized;
        
        bool isChasingDangerously = Vector3.Dot(directionToMyGoal, directionFromMeToBall) > 0;

        if (isChasingDangerously)
        {
            Vector3 sideDirection = Vector3.Cross(directionToMyGoal, Vector3.up).normalized;
            Vector3 directionToBallFromMe = (transform.position - ball.position).normalized;
            Vector3 leftFlankPosition = ball.position + (sideDirection * flankOffset) + (directionToBallFromMe * flankOffset * 0.5f);
            Vector3 rightFlankPosition = ball.position - (sideDirection * flankOffset) + (directionToBallFromMe * flankOffset * 0.5f);
            return (Vector3.Distance(transform.position, leftFlankPosition) < Vector3.Distance(transform.position, rightFlankPosition)) ? leftFlankPosition : rightFlankPosition;
        }
        else
        {
            Vector3 interceptPosition = FindClosestPointOnLineSegment(ball.position, myGoal.position, transform.position);
            
            if (Vector3.Distance(transform.position, interceptPosition) < rushTriggerDistance)
            {
                return ball.position;
            }
            else
            {
                return interceptPosition;
            }
        }
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
    
    private void OnCollisionEnter(Collision collision)
    {
        if(!isRagDoll)
        {
            if(collision.gameObject.CompareTag("Ball"))
            {
                EnableRagdoll();
                Vector3 dir = (transform.position - collision.contacts[0].point).normalized;
                foreach (var r in ragsRigid)
                {
                    //r.AddForce(dir * 1, ForceMode.Impulse);
                }
                if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
                ragDollCoroutine = StartCoroutine(ResetRagDoll());
            }
            else if(collision.gameObject.CompareTag("Obstacle"))
            {
                EnableRagdoll();
                if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
                ragDollCoroutine = StartCoroutine(ResetRagDoll());
            }
        }
    }

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

            Vector3 directionToGoal = (opponentGoal.position - ball.position).normalized;
            Vector3 directionFromMeToBall = (ball.position - transform.position).normalized;
            bool isBehindTheBall = Vector3.Dot(directionToGoal, directionFromMeToBall) < 0;

            if (!isBehindTheBall)
            {
                Gizmos.color = Color.cyan;
                Vector3 sideDirection = Vector3.Cross(directionToGoal, Vector3.up).normalized;
                Vector3 directionToBallFromMe = (transform.position - ball.position).normalized;
                Vector3 leftFlank = ball.position + (sideDirection * flankOffset) + (directionToBallFromMe * flankOffset * 0.5f);
                Vector3 rightFlank = ball.position - (sideDirection * flankOffset) + (directionToBallFromMe * flankOffset * 0.5f);

                Gizmos.DrawWireSphere(leftFlank, 0.3f);
                Gizmos.DrawWireSphere(rightFlank, 0.3f);
                Gizmos.DrawLine(transform.position, leftFlank);
                Gizmos.DrawLine(transform.position, rightFlank);
            }
        }
        else if (currentState == AIState.DEFENDING)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(ball.position, myGoal.position);

            Vector3 directionToMyGoal = (myGoal.position - ball.position).normalized;
            Vector3 directionFromMeToBall = (ball.position - transform.position).normalized;
            bool isChasingDangerously = Vector3.Dot(directionToMyGoal, directionFromMeToBall) > 0;

            if(!isChasingDangerously)
            {
                Vector3 closestPoint = FindClosestPointOnLineSegment(ball.position, myGoal.position, transform.position);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(closestPoint, rushTriggerDistance);
            }
        }

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(ball.position, 0.6f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(opponent.position, 1f);
    }
#endif
}