using UnityEngine;

/// <summary>
/// 축구공의 궤적 표시 및 물리 효과를 담당하는 시스템
/// </summary>
public class TrajectoryRenderer : MonoBehaviour
{
    #region Settings
    [Header("== 시각적 효과 ==")]
    [Tooltip("궤적 표시 여부")]
    public bool showTrajectory = true;

    [Tooltip("궤적 표시 지속 시간")]
    public float trajectoryDuration = 3f;

    [Header("== 물리 설정 ==")]
    [Tooltip("공의 회전 속도")]
    public float spinSpeed = 5f;
    #endregion

    #region Private Variables
    /// <summary>궤적 그리기를 위한 이전 위치들</summary>
    private Vector3[] trajectoryPoints;
    private int trajectoryIndex = 0;

    /// <summary>공의 Rigidbody 참조</summary>
    private Rigidbody ballRigidbody;

    /// <summary>크로스 진행 상태 추적</summary>
    private bool isActive = false;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        trajectoryPoints = new Vector3[200];
    }

    /// <summary>
    /// 물리 업데이트
    /// </summary>
    private void FixedUpdate()
    {
        if (isActive)
        {
            ApplyBallSpin();
        }
    }

    /// <summary>
    /// 매 프레임 업데이트
    /// </summary>
    private void Update()
    {
        if (isActive && showTrajectory)
        {
            RecordTrajectoryPoint();
        }
    }
    #endregion

    #region Trajectory Control
    /// <summary>
    /// 궤적 기록 시작
    /// </summary>
    public void StartTrajectory()
    {
        isActive = true;
        ClearTrajectory();
        //Debug.Log("[TrajectoryRenderer] 궤적 기록 시작");
    }

    /// <summary>
    /// 궤적 기록 중단
    /// </summary>
    public void StopTrajectory()
    {
        isActive = false;

        if (showTrajectory)
        {
            Invoke(nameof(ClearTrajectory), trajectoryDuration);
        }
        else
        {
            ClearTrajectory();
        }

        //Debug.Log("[TrajectoryRenderer] 궤적 기록 중단");
    }

    /// <summary>
    /// 궤적 포인트 기록
    /// </summary>
    private void RecordTrajectoryPoint()
    {
        if (trajectoryIndex < trajectoryPoints.Length)
        {
            trajectoryPoints[trajectoryIndex] = transform.position;
            trajectoryIndex++;
        }
    }

    /// <summary>
    /// 궤적 표시 제거
    /// </summary>
    private void ClearTrajectory()
    {
        trajectoryIndex = 0;
        trajectoryPoints = new Vector3[200];
        //Debug.Log("[TrajectoryRenderer] 궤적 정리 완료");
    }
    #endregion

    #region Physics Effects
    /// <summary>
    /// 공 회전 효과
    /// </summary>
    private void ApplyBallSpin()
    {
        if (ballRigidbody == null) return;

        Vector3 velocity = ballRigidbody.linearVelocity;
        if (velocity.magnitude > 0.1f)
        {
            Vector3 spinAxis = Vector3.Cross(velocity.normalized, Vector3.up).normalized;
            ballRigidbody.angularVelocity = spinAxis * spinSpeed;
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 궤적 표시 활성화/비활성화
    /// </summary>
    /// <param name="enable">활성화 여부</param>
    public void SetTrajectoryEnabled(bool enable)
    {
        showTrajectory = enable;

        if (!enable)
        {
            ClearTrajectory();
        }

        Debug.Log($"[TrajectoryRenderer] 궤적 표시 {(enable ? "활성화" : "비활성화")}");
    }

    /// <summary>
    /// 스핀 속도 설정
    /// </summary>
    /// <param name="speed">새로운 스핀 속도</param>
    public void SetSpinSpeed(float speed)
    {
        spinSpeed = speed;
        Debug.Log($"[TrajectoryRenderer] 스핀 속도 설정: {speed}");
    }
    #endregion
}