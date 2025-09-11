using System.Collections;
using System.Linq;
using UnityEngine;

public class Joker : MonoBehaviour
{
    // Component
    private Animator anim;

    [SerializeField] private Transform targetPos;

    private Rigidbody rb;
    private Rigidbody[] ragsRigid;
    [SerializeField] private Rigidbody hipRigid;
    private CharacterJoint[] joints;
    private Collider col;
    private Collider[] ragColls;

    // Const
    private const string WALKANIM = "isWalk";


    // State
    [SerializeField] private LayerMask groundLayer;
    private bool isGround = false;
    private bool isAttack = false;
   

    // Status
    [SerializeField] private float speed = 5f;
    private float curAttackDelay = 0f;
    private float attackDelay = 2f;

    private Coroutine ragDollCoroutine;


    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        //ragsRigid = GetComponentsInChildren<Rigidbody>();
        ragsRigid = GetComponentsInChildren<Rigidbody>()
            .Where(r => r != rb)
            .ToArray();

        col = GetComponent<Collider>();
        //ragColls = GetComponentsInChildren<Collider>();
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
        Vector3 dir = new Vector3(moveDir.x, 0, moveDir.y);

        if (dir == Vector3.zero)
        {
            // 레그돌에서 애니메이션을 꺼도 이게 되는건질 알아봐야함
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

        /*if (!isGround)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f, groundLayer))
            {
                isGround = true;
            }
        }*/
    }

    private void DisableRagdoll()
    {
        // 1. 애니메이션 실행
        anim.enabled = true;

        // 2. 조인트 물체 충돌 연결 해제
        foreach (var j in joints)
        {
            j.enableCollision = false;
        }

        // 3. 콜라이더 비활성화
        foreach (var c in ragColls)
        {
            c.enabled = false;
        }

        // 4. 물리 비활성화
        foreach (var r in ragsRigid)
        {
            r.detectCollisions = false; // 물리 충돌 감지 활성화
            r.useGravity = false; // 중력 비활성화
        }

        // 5. 플레이어 설정 활성화
        rb.detectCollisions = true;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        col.enabled = true;

        isGround = false;
    }

    private void EnableRagdoll()
    {
        // 1. 애니메이션 종료
        anim.enabled = false;

        // 2. 조인트 물체 충돌 연결 활성화
        foreach(var j in joints)
        {
            j.enableCollision = true;
        }
 
        // 3. 콜라이더 활성화
        foreach(var c in ragColls)
        {
            c.enabled = true;
        }

        // 4. 물리 활성화 및 초기화
        foreach (var r in ragsRigid)
        {
            r.linearVelocity = Vector3.zero; // 속도 초기화
            r.detectCollisions = true; // 물리 충돌 감지 활성화
            r.useGravity = true; // 중력 활성화
        }

        // 5. 플레이어 설정 비활성화
        rb.detectCollisions = false;
        rb.useGravity = false;
        col.enabled = false;

        isGround = true;

        //if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
        //ragDollCoroutine = StartCoroutine(ResetRagDoll());
    }

    // 다시 레그돌 실행
    private IEnumerator ResetRagDoll()
    {
        yield return new WaitForSeconds(1.5f);
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

        Vector3 dir = (targetPos.position - transform.position).normalized;
        hipRigid.AddForce(dir * 150f, ForceMode.Impulse);

        /*
        // 밀고
        Vector3 dir = (targetPos.position - transform.position).normalized;
        rb.AddForce(dir * 15f, ForceMode.Impulse);

        // 받고
        Vector3 v = rb.linearVelocity;
        Vector3 w = rb.angularVelocity;

        // 자식에 넣고
        foreach (var r in ragsRigid)
        {
            r.linearVelocity = v;
            r.angularVelocity = w;
        }*/

        yield return new WaitForSeconds(1.5f);
        DisableRagdoll();

        Debug.Log("이제 일어나기");

    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Obstacle"))
        {
            Rigidbody obsRigid = collision.gameObject.GetComponent<Rigidbody>();
            obsRigid.freezeRotation = false;

            Vector3 dir = (transform.position - obsRigid.position).normalized;
            obsRigid.AddForce(dir * rb.linearVelocity.magnitude, ForceMode.VelocityChange);

            // 레그돌 실행
            EnableRagdoll();
            rb.AddForce(-dir * rb.linearVelocity.magnitude, ForceMode.VelocityChange);

            if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
            ragDollCoroutine = StartCoroutine(ResetRagDoll());
        }
    }
}
