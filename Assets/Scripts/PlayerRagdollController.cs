using System.Collections;
using System.Linq;
using UnityEngine;

// 이름을 PlayerRagdollController로 변경했습니다.
public class PlayerRagdollController : MonoBehaviour
{
    public static PlayerRagdollController Instance { get; private set; }

    // Component
    private Animator anim;

    [SerializeField] private Transform targetPos;
    private Vector3 upDivingOffset = new Vector3(0, 1.4f, 0);
    private Vector3 downDivingOffset = new Vector3(0, 0.34f, 0);


    private Rigidbody rb;
    private Rigidbody[] ragsRigid;
    [SerializeField] private Rigidbody spineRigid;
    [SerializeField] private Rigidbody legRigid;
    private CharacterJoint[] joints;
    private Collider col;
    private Collider[] ragColls;

    // Const
    private const string WALKANIM = "isWalk";


    // State
    [SerializeField] private LayerMask groundLayer;
    private bool isRagDoll = false; // 현재 레그돌 실행중인지
    private bool isGround = false;
    private bool isAttack = false;
    
    // Status
    [SerializeField] private float speed = 5f;
    private float curAttackDelay = 0f;
    private float attackDelay = 2f;
    
    // ===== [ 이 부분을 추가했습니다 ] =====
    [Header("Ragdoll Settings")]
    [Tooltip("공이 이 속도(m/s) 이상으로 부딪혀야 래그돌이 활성화됩니다.")]
    public float ballSpeedRagdollThreshold = 10f;
    // ===================================

    private Coroutine ragDollCoroutine;


    private Vector3 divingDir;

    private void Awake()
    {
        // 싱글톤 인스턴스 이름을 클래스 이름과 일치시켰습니다.
        if(Instance == null) Instance = this;


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
    }


    private void Start()
    {
        InputManager.Instance.OnAttack += (a, b) => Attack();
    }

    private void FixedUpdate()
    {
        if (isAttack)
        {
            return;
        }

        Vector2 moveDir = InputManager.Instance.MoveDirNormalized();
        
        // ================== [ 수정된 부분 ] ==================
        // W가 +X축(오른쪽), A가 +Z축(앞)을 향하도록 방향 벡터를 90도 회전시켰습니다.
        // (x, y) -> (y, 0, -x)
        Vector3 dir = new Vector3(moveDir.y, 0, -moveDir.x);
        // ================================================

        if (dir == Vector3.zero)
        {
            divingDir = dir;
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
        curAttackDelay -= Time.deltaTime;
        if (curAttackDelay <= 0f)
        {
            isAttack = false;
        }
    }

    // 이하 코드는 원본과 동일합니다.
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
        isGround = false;
    }

    private void EnableRagdoll()
    {
        isRagDoll = true;
        anim.enabled = false;
        foreach(var j in joints) { j.enableCollision = true; }
        foreach(var c in ragColls) { c.enabled = true; }
        // 넘어질 때 현재 플레이어의 속도를 래그돌이 이어받도록 수정하면 더 자연스러울 수 있습니다.
        foreach (var r in ragsRigid) { r.linearVelocity = rb.linearVelocity; r.detectCollisions = true; r.useGravity = true; }
        rb.detectCollisions = false;
        rb.useGravity = false;
        col.enabled = false;
        isGround = true;
    }

    private IEnumerator ResetRagDoll()
    {
        yield return new WaitForSeconds(2.5f);
        DisableRagdoll();
    }

    private void Attack()
    {
        if (isAttack) return;
        StartCoroutine(AttackCoroutine());
    }

    private IEnumerator AttackCoroutine()
    {
        isAttack = true;
        curAttackDelay = attackDelay;
        EnableRagdoll();
        Vector3 dir;
        if (InputManager.Instance.UpDiving())
        {
            dir = (upDivingOffset + targetPos.position);
            dir = (dir - transform.position).normalized;
            spineRigid.AddForce(dir * 150f, ForceMode.Impulse);
        }
        else
        {
            dir = (downDivingOffset + targetPos.position);
            dir = (dir - transform.position).normalized;
            legRigid.AddForce(dir * 150f, ForceMode.Impulse);
        }
        yield return new WaitForSeconds(2.5f);
        DisableRagdoll();
        Debug.Log("이제 일어나기");
    }

    // ================== [ 수정된 OnCollisionEnter 함수 ] ==================
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
            float ballSpeed = ballRb.linearVelocity.magnitude;

            // 3. 공의 속력이 우리가 설정한 역치(threshold)보다 클 때만 래그돌을 활성화합니다.
            if (ballSpeed >= ballSpeedRagdollThreshold)
            {
                Debug.Log($"플레이어, 공과 충돌! 공 속도: {ballSpeed:F2} m/s. 래그돌을 활성화합니다.");

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
                Debug.Log($"플레이어, 공과 충돌했지만 속도가 느립니다. (속도: {ballSpeed:F2} m/s)");
            }
        }
    }
    // ====================================================================
}