using System.Collections;
using System.Linq;
using UnityEngine;

public class PlayerRagdollController : MonoBehaviour, IRagdollController
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

    [Tooltip("일어난 후 잠시 동안 다른 충격에 넘어지지 않는 무적 시간(초)입니다.")]
    public float spawnInvincibilityDuration = 0.5f;
    private bool isInvincible = false; // 현재 무적 상태인지 확인하는 내부 변수
 

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

    [Header("Ragdoll Settings")]
    [Tooltip("공이 이 속도(m/s) 이상으로 부딪혀야 래그돌이 활성화됩니다.")]
    public float ballSpeedRagdollThreshold = 10f;
    // [추가된 부분] =======================================================
    [Tooltip("래그돌 상태로 상대방과 부딪혔을 때, 상대에게 가하는 힘의 크기입니다.")]
    public float collisionTacklePower = 25f;
    // ====================================================================

    private Coroutine ragDollCoroutine;
    private Vector3 divingDir;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        ragsRigid = GetComponentsInChildren<Rigidbody>().Where(r => r != rb).ToArray();
        col = GetComponent<Collider>();
        ragColls = GetComponentsInChildren<Collider>().Where(c => c != col).ToArray();
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

        // --- [ 카메라 기준 이동 로직으로 수정 ] ---
        // 1. 메인 카메라의 Transform을 가져옵니다.
        Transform camTransform = Camera.main.transform;

        // 2. 카메라가 바라보는 방향을 기준으로 앞/옆 방향을 계산합니다. (Y축은 무시)
        Vector3 camForward = camTransform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = camTransform.right;
        camRight.y = 0;
        camRight.Normalize();

        // 3. 키보드 입력(WASD)을 받습니다.
        Vector2 moveInput = InputManager.Instance.MoveDirNormalized(); // (x, y) 형태

        // 4. 입력값과 카메라 방향을 조합하여 최종 이동 방향을 결정합니다.
        Vector3 dir = (camForward * moveInput.y + camRight * moveInput.x).normalized;
        // ===========================================

        if (dir.sqrMagnitude < 0.01f) // sqrMagnitude는 0과 비교할 때 더 효율적입니다.
        {
            // divingDir 관련 코드는 더 이상 필요하지 않을 수 있으므로 주석 처리하거나 삭제합니다.
            // divingDir = dir;
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

    // [추가된 부분] =======================================================
    #region Public Communication Functions
    /// <summary>
    /// 외부에서 현재 래그돌 상태인지 확인할 수 있게 해주는 함수입니다.
    /// </summary>
    public bool GetIsRagdollState()
    {
        return isRagDoll;
    }

    /// <summary>
    /// 외부의 충격으로 래그돌이 되도록 명령받는 함수입니다. (AI가 와서 부딪혔을 때 호출됨)
    /// </summary>
    /// <param name="impactDirection">충격 방향</param>
    /// <param name="impactForce">충격량</param>
    public void TriggerRagdollByImpact(Vector3 impactDirection, float impactForce)
    {
        if (isRagDoll || isInvincible) return; // 이미 래그돌 상태면 무시

        Debug.Log("플레이어가 AI의 충격으로 래그돌이 됩니다!");
        EnableRagdoll();

        // 충격 지점(가슴)에 전달받은 힘을 가해 실감 나게 넘어지게 합니다.
        spineRigid.AddForce(impactDirection * impactForce, ForceMode.Impulse);

        if (ragDollCoroutine != null) StopCoroutine(ragDollCoroutine);
        ragDollCoroutine = StartCoroutine(ResetRagDoll());
    }
    #endregion
    // ====================================================================

    private void DisableRagdoll()
    {
        isRagDoll = false;
        Vector3 original = spineRigid.position + new Vector3(0, -0.1f, 0);
        transform.position = original;
        anim.enabled = true;
        foreach (var j in joints) { j.enableCollision = false; }
        foreach (var c in ragColls) { c.enabled = false; }
        foreach (var r in ragsRigid) { r.detectCollisions = false; r.useGravity = false; }
        rb.detectCollisions = true; rb.useGravity = true; rb.linearVelocity = Vector3.zero; col.enabled = true; isGround = false;

        StartCoroutine(ResetInvincibility());
    }
    private IEnumerator ResetInvincibility()
    {
        isInvincible = true; // 무적 상태 시작
        yield return new WaitForSeconds(spawnInvincibilityDuration);
        isInvincible = false; // 설정된 시간이 지나면 무적 상태 해제
    }
    private void EnableRagdoll() { isRagDoll = true; anim.enabled = false; foreach (var j in joints) { j.enableCollision = true; } foreach (var c in ragColls) { c.enabled = true; } foreach (var r in ragsRigid) { r.linearVelocity = rb.linearVelocity; r.detectCollisions = true; r.useGravity = true; } rb.detectCollisions = false; rb.useGravity = false; col.enabled = false; isGround = true; }
    private IEnumerator ResetRagDoll() { yield return new WaitForSeconds(2.5f); DisableRagdoll(); }

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
        if (isRagDoll) return; // 래그돌 상태일 때는 아무것도 안함 (우편 배달부가 대신 일함)
        // 아래는 기존의 '서 있을 때'의 충돌 로직
        if (collision.gameObject.CompareTag("Obstacle"))
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
        else if (collision.gameObject.CompareTag("Ball"))
        {
            Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
            if (ballRb == null) return;
            float ballSpeed = ballRb.linearVelocity.magnitude;
            if (ballSpeed >= ballSpeedRagdollThreshold)
            {
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
        }
    }
        // [추가] '우편함' 역할을 할 Public 함수
    public void HandleRagdollCollision(Collision collision)
    {
        // 이 함수는 래그돌 파츠가 충돌했을 때만 호출됩니다.
        // 기존 OnCollisionEnter의 isRagDoll 상태일 때의 로직을 그대로 가져옵니다.
        if (collision.gameObject.CompareTag("AI"))
        {
            SoccerPlayerAI opponentAI = collision.gameObject.GetComponent<SoccerPlayerAI>();
            if (opponentAI != null && !opponentAI.GetIsRagdollState())
            {
                Debug.Log("플레이어 태클 성공! AI를 넘어뜨립니다.");
                Vector3 impactDirection = (collision.transform.position - transform.position).normalized;
                opponentAI.TriggerRagdollByImpact(impactDirection, collisionTacklePower);
            }
        }
    }
}