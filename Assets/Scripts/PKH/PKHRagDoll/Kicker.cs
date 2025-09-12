using System.Collections;
using System.Linq;
using UnityEngine;

public class Kicker : MonoBehaviour
{
    // Componet
    private Animator anim;
    private Rigidbody rb;
    private Rigidbody[] ragsRigid;
    private CharacterJoint[] joints;
    private Collider col;
    private Collider[] ragColls;

    [SerializeField] private Rigidbody spineRigid;
    [SerializeField] private Transform targetPos;


    // Const
    private const string WALKANIM = "isWalk";
    private const string KICKANIM = "isKick";

    // Status
    [SerializeField] private float speed = 5f;
    private float curKickDelay = 3f;
    private float kickDelay = 3f;
     
    
    // State
    private bool isKicker = true; // 현재 키커인지 확인
    private bool isRagDoll = false; // 현재 레그돌 실행중인지
    private bool isKick = false;
    private bool isReady = false;
    private Coroutine ragDollCoroutine;



    private void Awake()
    {
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

        Init();
    }

    private void Start()
    {
        PenaltyManager.Instance.ChangeKickerEvent += PenaltyManager_ChangeKickerEvent;
    }

    private void PenaltyManager_ChangeKickerEvent(object sender, bool e)
    {
        if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
        gameObject.SetActive(false);

        Debug.Log("현재 e값 :" + e);

        isKicker = !e;

        AISet();

        gameObject.SetActive(true);
    }

    private void AISet()
    {
        if (isKicker)
        {
            curKickDelay = kickDelay;

            isReady = false;
            isKick = false;
            transform.position = PenaltyManager.Instance.kickerPos;
        }
        else transform.position = PenaltyManager.Instance.goalKeeperPos;

        Init();
    }

    private void Init()
    {
        DisableRagdoll();
        rb.linearVelocity = Vector3.zero;
    }


    private void FixedUpdate()
    {
        if(isKicker)
        {
            if (isKick) return;

            if (Vector3.Distance(transform.position, targetPos.position) < 1f) Kick();

            if (!isReady)
            {
                anim.SetBool(WALKANIM, false);
                return;
            }
            else
            {
                Vector3 dir = targetPos.position - transform.position;
                dir = dir.normalized;

                anim.SetBool(WALKANIM, true);
                rb.MovePosition(rb.position + dir * speed * Time.fixedDeltaTime);

                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                rb.MoveRotation(targetRot);
            }
        }
        else
        {

        }
      
    }


    private void Update()
    {
        if(isKicker)
        {
            if (!isReady)
            {
                curKickDelay -= Time.deltaTime;
                if (curKickDelay < 0) isReady = true;
            }
        }  
    }

    private void DisableRagdoll()
    {
        isRagDoll = false;

        // 현재 피봇과 바디 위치 동기화
        Vector3 original = spineRigid.position + new Vector3(0, -0.1f, 0);
        transform.position = original;

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
    }

    private void EnableRagdoll()
    {
        isRagDoll = true;

        // 1. 애니메이션 종료
        anim.enabled = false;

        // 2. 조인트 물체 충돌 연결 활성화
        foreach (var j in joints)
        {
            j.enableCollision = true;
        }

        // 3. 콜라이더 활성화
        foreach (var c in ragColls)
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
    }

    // 다시 레그돌 실행
    private IEnumerator ResetRagDoll()
    {
        yield return new WaitForSeconds(2.5f);
        DisableRagdoll();
    }

    private void Kick()
    {
        if (isKick) return;

        Debug.Log("킥 준비");

        StartCoroutine(KickCoroutine());
    }


    private IEnumerator KickCoroutine()
    {
        Debug.Log("킥 발싸");

        isKick = true;
        anim.SetTrigger(KICKANIM);

        yield return new WaitForSeconds(0.5f);

        EnableRagdoll();

        yield return new WaitForSeconds(3f);

        if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);

        Debug.Log("실행 체크");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isKicker)
        {
            if (isRagDoll || isKick) return; // 실행중에는 차단

            if (collision.gameObject.CompareTag("Ball") || collision.gameObject.CompareTag("Player"))
            {
                // 레그돌 실행
                EnableRagdoll();
                Vector3 dir = (transform.position - collision.gameObject.transform.position).normalized;

                foreach (var r in ragsRigid)
                {
                    r.AddForce(dir * 20, ForceMode.Impulse);
                }

                if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
                ragDollCoroutine = StartCoroutine(ResetRagDoll());
            }
        }
        else
        {

        }
    }
}
