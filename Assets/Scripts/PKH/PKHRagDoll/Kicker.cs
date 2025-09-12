using System.Collections;
using System.Linq;
using UnityEngine;

public enum DanceType
{
    TWERK, Silly, BreakDance, Slide
}

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
    [SerializeField] private Rigidbody legRigid;
    [SerializeField] private Transform targetPos;


    // Const
    private const string WALKANIM = "isWalk";
    private const string KICKANIM = "isKick";
    private  string[] DanceANIM = { "isTwerk", "isSilly", "isBreakDance", "isSlide" };

    //TWERKANIM = "isTwerk";
    private const string ENDANIM = "end";

    // Status
    [SerializeField] private float speed = 5f;
    private float curKickDelay = 3f;
    private float kickDelay = 3f;
    private Vector3 upDivingOffset = new Vector3(0, 0.6f, 0);
    private Vector3 downDivingOffset = new Vector3(0, 0.14f, 0);

    // State
    private bool isKicker = true; // 현재 키커인지 확인
    private bool isRagDoll = false; // 현재 레그돌 실행중인지
    private bool isKick = false;
    private bool isDiving = false;
    private bool isReady = false;
    private Coroutine ragDollCoroutine;
    private Coroutine divingCoroutine;
    private Coroutine kickCoroutine;


    private bool isCeremony = false; // 세레머니 타임
    private bool kickCeremony = false;
    private bool defenceCeremony = false;

    private int danceCount = 0;

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

        DisableRagdoll();

        danceCount = System.Enum.GetValues(typeof(DanceType)).Length;
    }

    private void Start()
    {
        PenaltyManager.Instance.ChangeKickerEvent += PenaltyManager_ChangeKickerEvent;
    }

    private void PenaltyManager_ChangeKickerEvent(object sender, bool e)
    {
        if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
        if (divingCoroutine != null) StopCoroutine(divingCoroutine);
        if (kickCoroutine != null) StopCoroutine(kickCoroutine);

        isKicker = !e;

        AISet();
    }

    private void AISet()
    {
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        DisableRagdoll();
        yield return null;

        anim.enabled = false;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;

        if (isKicker)
        {
            curKickDelay = kickDelay;
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


        isReady = false;
        isKick = false;
        isDiving = false;
        rb.isKinematic = false;
        isRagDoll = false;
        anim.enabled = true;
        //anim.Rebind();
        //anim.Update(0f);

        if (isCeremony)
        {
            isCeremony = false;
            kickCeremony = false;
            defenceCeremony = false;
            anim.SetTrigger(ENDANIM);
        }

        anim.SetBool(WALKANIM, false);
    }


    private void FixedUpdate()
    {
        if (PenaltyManager.Instance.isGameEnd) return;
        if (PenaltyManager.Instance.isComplete) return;
        if (isCeremony) return;

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
            if (isDiving) return;
            if (Vector3.Distance(transform.position, targetPos.position) < 13f) // 다이빙
            {
                Diving();
            }
        }
      
    }


    private void Update()
    {
        if (PenaltyManager.Instance.isGameEnd) return;
        if (isCeremony) return;

        if (PenaltyManager.Instance.isCeremonyTime)
        {
            Ceremony();
            return;
        }

        if (isKicker)
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
        anim.Rebind();           // 바인딩 초기화
        anim.Update(0f);

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
        yield return new WaitForSeconds(2.5f);
        DisableRagdoll();
    }

    private void Kick()
    {
        if (isKick) return;

        //Debug.Log("킥 준비");

        if(kickCoroutine != null) StopCoroutine(kickCoroutine);
        kickCoroutine = StartCoroutine(KickCoroutine());
    }
    
    private IEnumerator KickCoroutine()
    {
        //Debug.Log("킥 발싸");

        isKick = true;
        anim.SetTrigger(KICKANIM);
        EnableRagdoll();

        yield return new WaitForSeconds(2.5f);
        isKick = false;

        DisableRagdoll();
        //Debug.Log("실행 체크");
    }

    private void Diving()
    {
        if (isDiving) return;

        if (divingCoroutine != null) StopCoroutine(divingCoroutine);
        divingCoroutine = StartCoroutine(DivingCoroutine());
    }

    private IEnumerator DivingCoroutine()
    {
        isDiving = true;
        EnableRagdoll();

        bool isCenter = Mathf.Abs(targetPos.position.x - PenaltyManager.Instance.ballPos.x) <= 0.3f ? true : false;
        bool isLeft = targetPos.position.x - PenaltyManager.Instance.ballPos.x <= 0 ? true : false;
        bool upDiving = Random.Range(0, 2) == 0 ? true : false;

        Vector3 dir = Vector3.zero;

        float power = 175f;

        if (!isCenter)
        {
            if (isLeft) dir = -transform.right;
            else dir = transform.right;
        }
        else
        {
            dir -= PenaltyManager.Instance.ballPos - targetPos.position;
            power = 125f;
        }

        if (upDiving)
        {
            dir = (upDivingOffset - dir).normalized;

            spineRigid.AddForce(dir * power, ForceMode.Impulse);
        }
        else
        {
            dir = (downDivingOffset - dir).normalized;

            legRigid.AddForce(dir * power, ForceMode.Impulse);
        }

        yield return new WaitForSeconds(2.5f);

        DisableRagdoll();
    }

    public void Ceremony()
    {
        if(PenaltyManager.Instance.isGoal && !PenaltyManager.Instance.isPlayerKick) // 내가 골을 넣었을 때
        {
            isCeremony = true;
            kickCeremony = true;

            CeremonyStart();
        }
        else if (!PenaltyManager.Instance.isGoal && PenaltyManager.Instance.isPlayerKick) // 내가 골을 막았을 때
        {
            isCeremony = true;
            defenceCeremony = true;

            CeremonyStart();
        }
    }

    private void CeremonyStart()
    {
        Debug.Log("세레머니 시작");
        StartCoroutine(CeremonyCoroutine());
    }

    private IEnumerator CeremonyCoroutine()
    {
        if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
        if (divingCoroutine != null) StopCoroutine(divingCoroutine);
        if (kickCoroutine != null) StopCoroutine(kickCoroutine);

        DisableRagdoll();
        anim.enabled = false;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;

        if(kickCeremony)
        {
            rb.position = PenaltyManager.Instance.ballPos;
            rb.rotation = Quaternion.Euler(PenaltyManager.Instance.kickerRotate);
            transform.position = PenaltyManager.Instance.ballPos;
            transform.rotation = Quaternion.Euler(PenaltyManager.Instance.kickerRotate);
        }

        yield return null;

        rb.isKinematic = false;
        isRagDoll = false;
        anim.enabled = true;

        int num = Random.Range(0, danceCount);

        anim.SetTrigger(DanceANIM[num]);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (PenaltyManager.Instance.isComplete) return;

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
}
