using System.Collections;
using System.Linq;
using UnityEngine;

public class Joker : MonoBehaviour
{
    public static Joker Instance { get; private set; }

    // Component
    private Animator anim;
    private Rigidbody rb;
    private CharacterJoint[] joints;
    private Collider col;
    private Collider[] ragColls;
    private Rigidbody[] ragsRigid;
    [SerializeField] private Transform targetPos;
    [SerializeField] private Rigidbody spineRigid;
    [SerializeField] private Rigidbody legRigid;

    // Const
    private const string WALKANIM = "isWalk";

    // State
    private bool isKicker = false; // 현재 키커인가.
    private bool isRagDoll = false; // 현재 레그돌 실행중인지
    private bool isAttack = false;

    // Status
    [SerializeField] private float speed = 5f;
    private float curAttackDelay = 0f;
    private float attackDelay = 2f;
    private Vector3 upDivingOffset = new Vector3(0, 1.4f, 0);
    private Vector3 downDivingOffset = new Vector3(0, 0.34f, 0);

    private Coroutine ragDollCoroutine;
    private Coroutine attackCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;

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
        PenaltyManager.Instance.ChangeKickerEvent += PenaltyManager_ChangeKickerEvent;
    }

    public void Test()
    {
        PenaltyManager.Instance.ChangeKicker();

        //DisableRagdoll();
        //rb.position = PenaltyManager.Instance.kickerPos;
    }

    private void PenaltyManager_ChangeKickerEvent(object sender, bool e)
    {
        if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);

        isKicker = e;

        PlayerSet();
    }

    private void PlayerSet()
    {
        StartCoroutine(Init());
    }
    private IEnumerator Init()
    {
        DisableRagdoll();
        anim.enabled = false;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;

        if (isKicker)
        {
            rb.position = PenaltyManager.Instance.kickerPos;
            rb.rotation = Quaternion.Euler(PenaltyManager.Instance.kickerRotate);
            transform.position = PenaltyManager.Instance.kickerPos;
            transform.rotation = Quaternion.Euler(PenaltyManager.Instance.kickerRotate);
        }
        else
        {
            rb.position = PenaltyManager.Instance.goalKeeperPos;
            rb.rotation = Quaternion.Euler(PenaltyManager.Instance.goalKeeperRotate);
            transform.position = PenaltyManager.Instance.goalKeeperPos;
            transform.rotation = Quaternion.Euler(PenaltyManager.Instance.goalKeeperRotate);
        }
        yield return null;

        rb.isKinematic = false;
        curAttackDelay = 0;
        isRagDoll = false;
        isAttack = false;
        anim.enabled = true;
    }

    private void FixedUpdate()
    {
        if (PenaltyManager.Instance.isComplete) return;
        if (isAttack) return;

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
            rb.MovePosition(rb.position + dir * speed * PenaltyManager.Instance.senemonySpeed * Time.fixedDeltaTime);

            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            rb.MoveRotation(targetRot);
        }
    }

    private void Update()
    {
        if (PenaltyManager.Instance.isGameEnd) return;

        curAttackDelay -= Time.deltaTime;
        if (curAttackDelay <= 0f)
        {
            isAttack = false;
        }
    }

    private void DisableRagdoll()
    {
        isRagDoll = false;

        Vector3 original = spineRigid.position + new Vector3(0, -0.1f, 0);
        transform.position = original;
        //Debug.Log("스냅 처리됨");

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
            r.isKinematic = true;
            r.detectCollisions = false; // 물리 충돌 감지 활성화
            r.useGravity = false; // 중력 비활성화
        }

        // 5. 플레이어 설정 활성화
        rb.isKinematic = false;
        rb.detectCollisions = true;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        col.enabled = true;
    }

    private void EnableRagdoll()
    {
        isRagDoll = true;

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
            r.isKinematic = false;
            r.linearVelocity = Vector3.zero; // 속도 초기화
            r.detectCollisions = true; // 물리 충돌 감지 활성화
            r.useGravity = true; // 중력 활성화
        }

        // 5. 플레이어 설정 비활성화
        rb.isKinematic = true;
        rb.detectCollisions = false;
        rb.useGravity = false;
        col.enabled = false;
    }

    // 다시 레그돌 실행
    private IEnumerator ResetRagDoll()
    {
        yield return new WaitForSeconds(2f);
        DisableRagdoll();
    }

    private void Attack()
    {
        if (PenaltyManager.Instance.isComplete) return;
        if (isAttack) return;

        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(AttackCoroutine());
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

            spineRigid.AddForce(dir * 175f, ForceMode.Impulse);
        }
        else
        {
            dir = (downDivingOffset + targetPos.position);
            dir = (dir - transform.position).normalized;

            legRigid.AddForce(dir * 175f, ForceMode.Impulse);
        }

        yield return new WaitForSeconds(2f);
        isAttack = false;

        DisableRagdoll();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (PenaltyManager.Instance.isComplete) return;

        if (collision.gameObject.CompareTag("Ball") || collision.gameObject.CompareTag("Enemy"))
        {
            if (!isRagDoll)
            {
                EnableRagdoll();
            }

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
