using System.Collections;
using System.Linq;
using UnityEngine;

// IJKL 조작이 적용된 AI 컨트롤러 스크립트
public class AIRagdollController : MonoBehaviour
{
    // 싱글톤은 AI 컨트롤러에 필요 없으므로 제거하거나 주석 처리합니다.
    // public static AIRagdollController Instance { get; private set; }

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
    private bool isRagDoll = false;
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
        // if(Instance == null) Instance = this;
        
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

    private void FixedUpdate()
    {
        if (isAttack)
        {
            return;
        }

        // ================== [ IJKL 조작으로 수정된 부분 ] ==================
        // I,J,K,L 키 입력을 직접 감지하여 방향 벡터를 만듭니다.
        Vector2 moveInput = Vector2.zero;
        if (Input.GetKey(KeyCode.I)) moveInput.y = 1;  // 위
        if (Input.GetKey(KeyCode.K)) moveInput.y = -1; // 아래
        if (Input.GetKey(KeyCode.J)) moveInput.x = -1; // 왼쪽
        if (Input.GetKey(KeyCode.L)) moveInput.x = 1;  // 오른쪽

        // 윗방향(I)키가 +X축(오른쪽)을 향하도록 방향 벡터를 90도 회전시킵니다.
        Vector3 dir = new Vector3(moveInput.y, 0, -moveInput.x);
        
        // 대각선 이동 시 속도가 빨라지는 것을 방지하기 위해 정규화
        if (dir.magnitude > 1f)
        {
            dir.Normalize();
        }
        // =============================================================

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

        // 공격 키는 오른쪽 Shift로 동일하게 유지
        if (Input.GetKeyDown(KeyCode.RightShift))
        {
            Attack();
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
        
        // 다이빙 구분은 왼쪽 Shift / 그 외로 동일하게 유지
        if (Input.GetKey(KeyCode.LeftShift)) // 위로 다이빙
        {
            dir = (upDivingOffset + targetPos.position);
            dir = (dir - transform.position).normalized;
            spineRigid.AddForce(dir * 150f, ForceMode.Impulse);
        }
        else // 아래로 다이빙
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