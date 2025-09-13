using UnityEngine;

/// <summary>
/// 골대 트리거 - 공이 골대 안에 들어왔는지 감지
/// </summary>
public class GoalTrigger : MonoBehaviour
{
    #region Events
    /// <summary>골 성공 이벤트</summary>
    public System.Action OnGoalScored;
    #endregion

    #region Settings
    [Header("== 트리거 설정 ==")]
    [Tooltip("골대 트리거 활성화 여부")]
    public bool isActive = true;

    [Tooltip("트리거 시각화")]
    public bool showTrigger = true;

    [Tooltip("트리거 색상")]
    public Color triggerColor = Color.green;

    [Header("== 골 처리 설정 ==")]
    [Tooltip("골 시 자연스러운 그물 효과 사용")]
    public bool useNetEffect = true;

    [Tooltip("그물로 빨려들어가는 힘")]
    public float netPullForce = 5f;

    [Tooltip("공의 속도를 감소시키는 정도 (0~1)")]
    [Range(0f, 1f)]
    public float velocityDamping = 0.3f;

    [Tooltip("그물 효과 지속 시간 (초)")]
    public float netEffectDuration = 2f;

    [Tooltip("그물 중심 위치 (로컬 좌표)")]
    public Vector3 netCenter = new Vector3(0, 0, 0.5f);

    [Header("== 지면 정지 설정 ==")]
    [Tooltip("지면 높이 (이 높이 이하로 떨어지면 완전히 정지)")]
    public float groundLevel = -0.5f;

    [Tooltip("지면에 닿았을 때 완전 정지할지 여부")]
    public bool stopOnGround = true;
    #endregion

    #region Private Variables
    /// <summary>골이 이미 처리되었는지 여부</summary>
    private bool goalProcessed = false;

    /// <summary>그물 효과가 진행 중인지 여부</summary>
    private bool netEffectActive = false;

    /// <summary>그물 효과 시작 시간</summary>
    private float netEffectStartTime;

    /// <summary>공의 Rigidbody 참조</summary>
    private Rigidbody ballRigidbody;

    /// <summary>그물 중심의 월드 좌표</summary>
    private Vector3 worldNetCenter;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// 시작 시 그물 중심 위치 계산
    /// </summary>
    private void Start()
    {
        UpdateNetCenterPosition();
    }

    /// <summary>
    /// 트리거 충돌 감지
    /// </summary>
    /// <param name="other">충돌한 오브젝트</param>
    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || goalProcessed) return;

        if (other.CompareTag("Ball"))
        {
            goalProcessed = true;
            ballRigidbody = other.GetComponent<Rigidbody>();

            Debug.Log("[GoalTrigger] 골!! 그물 효과 시작");

            if (useNetEffect && ballRigidbody != null)
            {
                StartNetEffect();
            }
            else
            {
                // 기존 방식으로 즉시 정지
                StopBallImmediately(other.gameObject);
            }

            OnGoalScored?.Invoke();
        }
    }

    /// <summary>
    /// 매 프레임 그물 효과 업데이트
    /// </summary>
    private void Update()
    {
        if (netEffectActive && ballRigidbody != null)
        {
            UpdateNetEffect();
        }
    }

    /// <summary>
    /// 트리거 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        if (showTrigger)
        {
            // 트리거 영역 표시
            Gizmos.color = triggerColor;
            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                if (triggerCollider is BoxCollider box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (triggerCollider is SphereCollider sphere)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                }
            }

            // 그물 중심 표시
            UpdateNetCenterPosition();
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(worldNetCenter, 0.3f);

            // 지면 레벨 표시
            Gizmos.color = Color.red;
            Vector3 groundPos = new Vector3(transform.position.x, groundLevel, transform.position.z);
            Gizmos.DrawWireCube(groundPos, new Vector3(5f, 0.1f, 3f));

#if UNITY_EDITOR
            // 그물 정보 표시
            UnityEditor.Handles.Label(worldNetCenter + Vector3.up * 0.5f,
                $"그물 중심\n당기는 힘: {netPullForce}\n감쇠: {velocityDamping}");
#endif
        }
    }
    #endregion

    #region Net Effect
    /// <summary>
    /// 그물 효과 시작
    /// </summary>
    private void StartNetEffect()
    {
        netEffectActive = true;
        netEffectStartTime = Time.time;

        // 다른 시스템들 정지 (크로스, 킥 등)
        StopBallSystems(ballRigidbody.gameObject);

        Debug.Log($"[GoalTrigger] 그물 효과 시작 - 중심: {worldNetCenter}");
    }

    /// <summary>
    /// 매 프레임 그물 효과 업데이트
    /// </summary>
    private void UpdateNetEffect()
    {
        if (ballRigidbody == null) return;

        float elapsedTime = Time.time - netEffectStartTime;
        Vector3 ballPosition = ballRigidbody.transform.position;

        // 지면에 닿으면 완전 정지
        if (stopOnGround && ballPosition.y <= groundLevel)
        {
            StopBallCompletely();
            return;
        }

        // 그물 효과 지속 시간 체크
        if (elapsedTime >= netEffectDuration)
        {
            // 그물 효과 종료, 자연스럽게 떨어지도록
            netEffectActive = false;
            Debug.Log("[GoalTrigger] 그물 효과 종료 - 자연 낙하 시작");
            return;
        }

        // 그물 중심으로 끌어당기는 힘 계산
        Vector3 directionToNet = worldNetCenter - ballPosition;

        // Y축은 자연스럽게 유지 (중력 효과)
        directionToNet.y = 0;

        // 시간에 따른 힘 감소 (처음에는 강하게, 나중에는 약하게)
        float timeProgress = elapsedTime / netEffectDuration;
        float currentPullForce = netPullForce * (1f - timeProgress);

        // 속도 감쇠 적용
        Vector3 currentVelocity = ballRigidbody.linearVelocity;
        currentVelocity *= (1f - velocityDamping * Time.deltaTime);

        // 그물로 끌어당기는 힘 추가 (수평 방향만)
        if (directionToNet.magnitude > 0.1f)
        {
            Vector3 pullForceVector = directionToNet.normalized * currentPullForce;
            ballRigidbody.AddForce(pullForceVector, ForceMode.Force);
        }

        // 수정된 속도 적용
        ballRigidbody.linearVelocity = currentVelocity;

        // 회전 감쇠
        ballRigidbody.angularVelocity *= (1f - velocityDamping * Time.deltaTime);

        Debug.DrawLine(ballPosition, worldNetCenter, Color.green, Time.deltaTime);
    }

    /// <summary>
    /// 공을 완전히 정지
    /// </summary>
    private void StopBallCompletely()
    {
        if (ballRigidbody == null) return;

        netEffectActive = false;

        // 완전 정지
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;

        // 지면 위치로 조정
        Vector3 finalPosition = ballRigidbody.transform.position;
        finalPosition.y = groundLevel + 0.1f; // 지면보다 살짝 위
        ballRigidbody.transform.position = finalPosition;

        // 물리 고정
        ballRigidbody.isKinematic = true;

        Debug.Log($"[GoalTrigger] 공이 지면에서 완전 정지: {finalPosition}");
    }

    /// <summary>
    /// 즉시 정지 (기존 방식)
    /// </summary>
    private void StopBallImmediately(GameObject ball)
    {
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        StopBallSystems(ball);
        Debug.Log("[GoalTrigger] 공을 즉시 정지");
    }

    /// <summary>
    /// 그물 중심 위치 업데이트
    /// </summary>
    private void UpdateNetCenterPosition()
    {
        worldNetCenter = transform.TransformPoint(netCenter);
    }
    #endregion

    #region Ball Systems Control
    /// <summary>
    /// 공과 관련된 시스템들 정지
    /// </summary>
    /// <param name="ball">공 오브젝트</param>
    private void StopBallSystems(GameObject ball)
    {
        // CrossSystem 정지
        CrossSystem crossSystem = ball.GetComponent<CrossSystem>();
        if (crossSystem != null && crossSystem.IsCrossing)
        {
            crossSystem.StopCross();
            Debug.Log("[GoalTrigger] CrossSystem 정지");
        }

        // KickSystem 비활성화
        KickSystem kickSystem = ball.GetComponent<KickSystem>();
        if (kickSystem != null)
        {
            kickSystem.SetKickEnabled(false);
            Debug.Log("[GoalTrigger] KickSystem 비활성화");
        }

        // TrajectoryRenderer 정지
        TrajectoryRenderer trajectoryRenderer = ball.GetComponent<TrajectoryRenderer>();
        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.StopTrajectory();
            Debug.Log("[GoalTrigger] TrajectoryRenderer 정지");
        }

        // MarkerManager 정리
        MarkerManager markerManager = ball.GetComponent<MarkerManager>();
        if (markerManager != null)
        {
            markerManager.CleanupAllMarkers();
            Debug.Log("[GoalTrigger] MarkerManager 정리");
        }
    }

    /// <summary>
    /// 골 상태 리셋 (새로운 시퀀스 시작 시 호출)
    /// </summary>
    public void ResetGoal()
    {
        goalProcessed = false;
        netEffectActive = false;
        ballRigidbody = null;
        Debug.Log("[GoalTrigger] 골 상태 리셋");
    }

    /// <summary>
    /// 공의 물리 상태 복원 (필요시 호출)
    /// </summary>
    /// <param name="ball">공 오브젝트</param>
    public void RestoreBallPhysics(GameObject ball)
    {
        if (ball == null) return;

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;
            Debug.Log("[GoalTrigger] 공의 물리 상태 복원");
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 트리거 활성화/비활성화
    /// </summary>
    /// <param name="active">활성화 여부</param>
    public void SetActive(bool active)
    {
        isActive = active;

        // 트리거 비활성화 시 골 상태도 리셋
        if (!active)
        {
            ResetGoal();
        }

        Debug.Log($"[GoalTrigger] 트리거 {(active ? "활성화" : "비활성화")}");
    }

    /// <summary>
    /// 그물 효과 설정
    /// </summary>
    /// <param name="useEffect">그물 효과 사용 여부</param>
    /// <param name="pullForce">당기는 힘</param>
    /// <param name="damping">속도 감쇠</param>
    public void SetNetEffect(bool useEffect, float pullForce, float damping)
    {
        useNetEffect = useEffect;
        netPullForce = pullForce;
        velocityDamping = damping;
        Debug.Log($"[GoalTrigger] 그물 효과 설정 - 사용: {useEffect}, 힘: {pullForce}, 감쇠: {damping}");
    }

    /// <summary>
    /// 그물 중심 위치 설정
    /// </summary>
    /// <param name="localCenter">로컬 좌표계에서의 그물 중심</param>
    public void SetNetCenter(Vector3 localCenter)
    {
        netCenter = localCenter;
        UpdateNetCenterPosition();
        Debug.Log($"[GoalTrigger] 그물 중심 설정: {netCenter} (월드: {worldNetCenter})");
    }
    #endregion
}