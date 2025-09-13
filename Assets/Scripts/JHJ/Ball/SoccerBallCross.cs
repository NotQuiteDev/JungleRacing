using UnityEngine;

/// <summary>
/// 축구공 크로스 시스템 - 메인 컨트롤러
/// 분할된 시스템들을 통합 관리하는 메인 컴포넌트
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SoccerBallCross : MonoBehaviour
{
    #region Settings
    [Header("== 자동 시작 설정 ==")]
    [Tooltip("자동 크로스 시작 여부")]
    public bool autoStartCross = true;
    
    [Tooltip("크로스 시작 지연 시간")]
    public float delayBeforeStart = 1f;
    #endregion

    #region System References
    /// <summary>마커 관리 시스템</summary>
    private MarkerManager markerManager;
    
    /// <summary>크로스 시스템</summary>
    private CrossSystem crossSystem;
    
    /// <summary>킥 시스템</summary>
    private KickSystem kickSystem;
    
    /// <summary>궤적 렌더러</summary>
    private TrajectoryRenderer trajectoryRenderer;
    
    /// <summary>공의 Rigidbody 컴포넌트</summary>
    private Rigidbody ballRigidbody;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        
        // 하위 시스템들 가져오기 또는 생성
        markerManager = GetComponent<MarkerManager>() ?? gameObject.AddComponent<MarkerManager>();
        crossSystem = GetComponent<CrossSystem>() ?? gameObject.AddComponent<CrossSystem>();
        kickSystem = GetComponent<KickSystem>() ?? gameObject.AddComponent<KickSystem>();
        trajectoryRenderer = GetComponent<TrajectoryRenderer>() ?? gameObject.AddComponent<TrajectoryRenderer>();
        
        // 이벤트 연결
        SetupEventHandlers();
    }

    /// <summary>
    /// 게임 시작 시 자동 크로스 실행
    /// </summary>
    private void Start()
    {
        if (autoStartCross)
        {
            Invoke(nameof(StartCross), delayBeforeStart);
        }
    }

    /// <summary>
    /// 매 프레임 업데이트
    /// </summary>
    private void Update()
    {
        // 지면 투영 마커 업데이트 (크로스 중일 때만)
        if (crossSystem.IsCrossing)
        {
            markerManager.UpdateGroundProjection(transform.position);
        }
    }
    #endregion

    #region Event Handling
    /// <summary>
    /// 이벤트 핸들러 설정
    /// </summary>
    private void SetupEventHandlers()
    {
        // 크로스 이벤트
        crossSystem.OnCrossStarted += OnCrossStarted;
        crossSystem.OnCrossCompleted += OnCrossCompleted;
        
        // 킥 이벤트
        kickSystem.OnKickExecuted += OnKickExecuted;
        kickSystem.OnKickCompleted += OnKickCompleted;
    }

    /// <summary>
    /// 크로스 시작 이벤트 핸들러
    /// </summary>
    /// <param name="target">크로스 목표 위치</param>
    private void OnCrossStarted(Vector3 target)
    {
        // 킥 활성화
        kickSystem.SetKickEnabled(true);
        
        // 크로스 목표 마커 생성
        markerManager.CreateCrossTargetMarker(target);
        
        // 궤적 기록 시작
        trajectoryRenderer.StartTrajectory();
        
        Debug.Log($"[SoccerBallCross] 크로스 시작! 목표: {target}");
    }

    /// <summary>
    /// 크로스 완료 이벤트 핸들러
    /// </summary>
    private void OnCrossCompleted()
    {
        // 마커 정리
        markerManager.OnCrossCompleted();
        markerManager.CleanupGroundMarker();
        
        // 궤적 기록 중단
        trajectoryRenderer.StopTrajectory();
        
        Debug.Log("[SoccerBallCross] 크로스 완료");
    }

    /// <summary>
    /// 킥 실행 이벤트 핸들러
    /// </summary>
    /// <param name="target">킥 목표 위치</param>
    private void OnKickExecuted(Vector3 target)
    {
        // 크로스 완료 처리
        crossSystem.CompleteCross();
        
        // 킥 목표 마커 생성
        markerManager.CreateKickTargetMarker(target);
        
        Debug.Log($"[SoccerBallCross] 킥 실행! 목표: {target}");
        
        // 3초 후 리셋
        Invoke(nameof(ResetForNextCross), 3f);
    }

    /// <summary>
    /// 킥 완료 이벤트 핸들러
    /// </summary>
    private void OnKickCompleted()
    {
        // 킥 마커 정리
        markerManager.OnKickCompleted();
        
        Debug.Log("[SoccerBallCross] 킥 완료");
    }

    /// <summary>
    /// 다음 크로스를 위한 리셋
    /// </summary>
    private void ResetForNextCross()
    {
        kickSystem.ResetKick();
        markerManager.CleanupGroundMarker();
        Debug.Log("[SoccerBallCross] 다음 크로스 준비 완료");
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 크로스 시작
    /// </summary>
    public void StartCross()
    {
        crossSystem.StartCross();
    }

    /// <summary>
    /// 새로운 목표로 크로스 실행 (고정 목표)
    /// </summary>
    public void CrossToPosition(Vector3 newTarget, float newFlightTime = -1f)
    {
        crossSystem.CrossToPosition(newTarget, newFlightTime);
    }

    /// <summary>
    /// 랜덤 범위로 크로스 실행
    /// </summary>
    public void StartRandomCross(Vector3 center, float rangeX, float rangeY, float rangeZ, float newFlightTime = -1f)
    {
        crossSystem.StartRandomCross(center, rangeX, rangeY, rangeZ, newFlightTime);
    }

    /// <summary>
    /// 기본 설정으로 랜덤 크로스 실행
    /// </summary>
    public void StartRandomCross()
    {
        crossSystem.StartRandomCross();
    }

    /// <summary>
    /// 랜덤 크로스 범위 설정
    /// </summary>
    public void SetRandomCrossArea(Vector3 center, float rangeX, float rangeY, float rangeZ)
    {
        crossSystem.SetRandomCrossArea(center, rangeX, rangeY, rangeZ);
    }

    /// <summary>
    /// 킥 목표 영역 설정
    /// </summary>
    public void SetKickArea(Vector3 center, float rangeX, float rangeY, float rangeZ)
    {
        kickSystem.SetKickArea(center, rangeX, rangeY, rangeZ);
    }

    /// <summary>
    /// 크로스 리셋
    /// </summary>
    public void ResetCross()
    {
        crossSystem.StopCross();
        kickSystem.ResetKick();
        trajectoryRenderer.StopTrajectory();
        markerManager.CleanupAllMarkers();
        
        if (ballRigidbody != null)
        {
            ballRigidbody.linearVelocity = Vector3.zero;
            ballRigidbody.angularVelocity = Vector3.zero;
        }
        
        CancelInvoke();
    }

    /// <summary>
    /// 현재 상태 확인
    /// </summary>
    public string GetCurrentStatus()
    {
        if (kickSystem.HasKicked) return "킥 완료";
        if (kickSystem.CanKick && crossSystem.IsCrossing) return "크로스 중 (킥 가능)";
        if (crossSystem.IsCrossing) return "크로스 중";
        return "대기 중";
    }

    /// <summary>
    /// 킥 가능 상태 확인
    /// </summary>
    public bool CanKick() => kickSystem.CanKick;

    /// <summary>
    /// 현재 크로스 목표 위치 반환
    /// </summary>
    public Vector3 GetCurrentCrossTarget() => crossSystem.CurrentCrossTarget;

    /// <summary>
    /// 현재 지면 투영 위치 반환
    /// </summary>
    public Vector3 GetCurrentGroundProjection() => markerManager.GetCurrentGroundProjection();
    #endregion

    #region Debug Visualization
    #if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        if (crossSystem == null || kickSystem == null) return;

        // 상태 정보
        if (Application.isPlaying)
        {
            UnityEditor.Handles.color = Color.white;
            string info = $"[SoccerBallCross]\n상태: {GetCurrentStatus()}";
            if (crossSystem.IsCrossing)
            {
                info += $"\n크로스 목표: {crossSystem.CurrentCrossTarget}";
                Vector3 groundPos = markerManager.GetCurrentGroundProjection();
                if (groundPos != Vector3.zero)
                {
                    info += $"\n지면 투영: {groundPos}";
                }
            }
            if (kickSystem.HasKicked)
            {
                info += $"\n킥 목표: {kickSystem.CurrentKickTarget}";
            }
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, info);
        }
    }
    #endif
    #endregion
}