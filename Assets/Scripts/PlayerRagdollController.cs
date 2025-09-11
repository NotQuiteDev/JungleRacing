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
        foreach (var r in ragsRigid) { r.linearVelocity = Vector3.zero; r.detectCollisions = true; r.useGravity = true; }
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
            EnableRagdoll();
            Vector3 dir = transform.position - collision.gameObject.transform.position;
            dir.Normalize();
            foreach (var r in ragsRigid)
            {
                r.AddForce(dir * 20, ForceMode.Impulse);
            }
            if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
            ragDollCoroutine = StartCoroutine(ResetRagDoll());
        }
    }
}