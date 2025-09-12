using UnityEngine;

/// <summary>
/// 축구공 크로스 시스템 - 공이 궤적을 그리며 목표 지점으로 날아옴
/// 개선된 궤적 계산으로 목표지점 정확히 도달
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SoccerBallCross : MonoBehaviour
{
    #region Settings
    [Header("== 크로스 설정 ==")]
    [Tooltip("목표 위치 (기본값: 0,0,0)")]
    public Vector3 targetPosition = Vector3.zero;
    
    [Tooltip("크로스 최고 높이 (목표점 기준 상대적)")]
    public float maxHeight = 6f;
    
    [Tooltip("비행 시간 (초) - 이 시간 안에 목표에 도달")]
    public float flightTime = 3f;
    
    [Tooltip("크로스 시작까지의 대기 시간")]
    public float delayBeforeStart = 1f;
    
    [Header("== 물리 설정 ==")]
    [Tooltip("공의 회전 속도")]
    public float spinSpeed = 5f;
    
    [Tooltip("공기 저항 (0=없음, 1=강함)")]
    public float airResistance = 0.1f;
    
    [Header("== 시각적 효과 ==")]
    [Tooltip("궤적 표시 여부")]
    public bool showTrajectory = true;
    
    [Tooltip("궤적 표시 지속 시간")]
    public float trajectoryDuration = 3f;
    #endregion

    #region Private Variables
    /// <summary>공의 Rigidbody 컴포넌트</summary>
    private Rigidbody ballRigidbody;
    
    /// <summary>크로스 시작 위치</summary>
    private Vector3 startPosition;
    
    /// <summary>크로스가 진행 중인지 여부</summary>
    private bool isCrossing = false;
    
    /// <summary>크로스 시작 시간</summary>
    private float crossStartTime;
    
    /// <summary>계산된 초기 속도</summary>
    private Vector3 initialVelocity;
    
    /// <summary>궤적 그리기를 위한 이전 위치들</summary>
    private Vector3[] trajectoryPoints;
    private int trajectoryIndex = 0;
    
    /// <summary>목표에 도달했는지 여부</summary>
    private bool hasReachedTarget = false;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        startPosition = transform.position;
        
        // 궤적 포인트 배열 초기화
        trajectoryPoints = new Vector3[200];
    }

    /// <summary>
    /// 게임 시작 시 크로스 실행
    /// </summary>
    private void Start()
    {
        // 지연 시간 후 크로스 시작
        Invoke(nameof(StartCross), delayBeforeStart);
    }

    /// <summary>
    /// 물리 업데이트
    /// </summary>
    private void FixedUpdate()
    {
        if (isCrossing && !hasReachedTarget)
        {
            // 공 회전 효과
            ApplyBallSpin();
            
            // 공기 저항 적용
            ApplyAirResistance();
            
            // 목표 근처 도달 체크
            CheckTargetReach();
        }
    }

    /// <summary>
    /// 매 프레임 업데이트
    /// </summary>
    private void Update()
    {
        // 궤적 포인트 저장
        if (isCrossing && showTrajectory)
        {
            RecordTrajectoryPoint();
        }
    }
    #endregion

    #region Cross System
    /// <summary>
    /// 크로스 시작
    /// </summary>
    private void StartCross()
    {
        if (isCrossing) return;

        isCrossing = true;
        hasReachedTarget = false;
        crossStartTime = Time.time;
        startPosition = transform.position;
        
        // 정확한 궤적 계산
        initialVelocity = CalculatePreciseLaunchVelocity();
        ballRigidbody.linearVelocity = initialVelocity;
        
        Debug.Log($"[SoccerBallCross] 크로스 시작!");
        Debug.Log($"시작 위치: {startPosition}");
        Debug.Log($"목표 위치: {targetPosition}");
        Debug.Log($"초기 속도: {initialVelocity}");
        Debug.Log($"비행 시간: {flightTime}초");
    }

    /// <summary>
    /// 정확한 포물선 궤적을 위한 발사 속도 계산
    /// 공기 저항을 고려한 더 강한 초기 속도로 계산
    /// </summary>
    private Vector3 CalculatePreciseLaunchVelocity()
    {
        Vector3 displacement = targetPosition - startPosition;
        float gravity = Mathf.Abs(Physics.gravity.y);
        
        // 수평 거리와 수직 거리 분리
        Vector3 horizontalDisplacement = new Vector3(displacement.x, 0, displacement.z);
        float horizontalDistance = horizontalDisplacement.magnitude;
        float verticalDisplacement = displacement.y;
        
        // 공기 저항을 고려해서 수평 속도를 더 크게 계산
        float airResistanceFactor = 1f + (airResistance * 2f); // 공기 저항 보상
        Vector3 horizontalVelocity = horizontalDisplacement.normalized * (horizontalDistance / flightTime) * airResistanceFactor;
        
        // 수직 속도 계산 - 더 높게 쏘기
        float verticalVelocity = (verticalDisplacement + 0.5f * gravity * flightTime * flightTime) / flightTime;
        
        // 최고 높이를 확실히 보장
        float minVerticalVelocityForHeight = Mathf.Sqrt(2 * gravity * maxHeight);
        verticalVelocity = Mathf.Max(verticalVelocity, minVerticalVelocityForHeight);
        
        // 공기 저항 보상을 위해 수직 속도도 증가
        verticalVelocity *= (1f + airResistance);
        
        Vector3 finalVelocity = horizontalVelocity + Vector3.up * verticalVelocity;
        
        Debug.Log($"[크로스 계산] 수평거리: {horizontalDistance:F1}m, 보정된 수평속도: {horizontalVelocity.magnitude:F1}m/s");
        Debug.Log($"[크로스 계산] 수직변위: {verticalDisplacement:F1}m, 보정된 수직속도: {verticalVelocity:F1}m/s");
        Debug.Log($"[크로스 계산] 공기저항 보정계수: {airResistanceFactor:F2}");
        Debug.Log($"[크로스 계산] 최종속도: {finalVelocity}");
        
        return finalVelocity;
    }

    /// <summary>
    /// 공 회전 효과 적용
    /// </summary>
    private void ApplyBallSpin()
    {
        Vector3 velocity = ballRigidbody.linearVelocity;
        if (velocity.magnitude > 0.1f)
        {
            // 이동 방향에 수직인 축으로 회전
            Vector3 spinAxis = Vector3.Cross(velocity.normalized, Vector3.up).normalized;
            ballRigidbody.angularVelocity = spinAxis * spinSpeed;
        }
    }

    /// <summary>
    /// 공기 저항 적용 - 더 부드럽게 조정
    /// </summary>
    private void ApplyAirResistance()
    {
        if (airResistance > 0)
        {
            Vector3 velocity = ballRigidbody.linearVelocity;
            // 공기 저항을 더 부드럽게 적용 (제곱 대신 선형)
            Vector3 resistance = -velocity * airResistance;
            ballRigidbody.AddForce(resistance, ForceMode.Force);
        }
    }

    /// <summary>
    /// 목표 도달 체크
    /// </summary>
    private void CheckTargetReach()
    {
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        
        // 목표에 가까워지거나 시간이 많이 지났으면 완료
        if (distanceToTarget < 1f || Time.time - crossStartTime > flightTime + 2f)
        {
            CompleteCross();
        }
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
    /// 크로스 완료 처리
    /// </summary>
    private void CompleteCross()
    {
        if (hasReachedTarget) return;
        
        hasReachedTarget = true;
        isCrossing = false;
        
        float actualDistance = Vector3.Distance(transform.position, targetPosition);
        Debug.Log($"[SoccerBallCross] 크로스 완료! 목표와의 거리: {actualDistance:F2}m");
        
        // 궤적 표시 제거 (지연)
        if (showTrajectory)
        {
            Invoke(nameof(ClearTrajectory), trajectoryDuration);
        }
    }

    /// <summary>
    /// 궤적 표시 제거
    /// </summary>
    private void ClearTrajectory()
    {
        trajectoryIndex = 0;
        trajectoryPoints = new Vector3[200];
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 새로운 목표로 크로스 실행
    /// </summary>
    /// <param name="newTarget">새로운 목표 위치</param>
    /// <param name="newFlightTime">새로운 비행 시간 (선택사항)</param>
    public void CrossToPosition(Vector3 newTarget, float newFlightTime = -1f)
    {
        targetPosition = newTarget;
        if (newFlightTime > 0) flightTime = newFlightTime;
        
        ResetCross();
        StartCross();
    }

    /// <summary>
    /// 크로스 리셋
    /// </summary>
    public void ResetCross()
    {
        isCrossing = false;
        hasReachedTarget = false;
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        ClearTrajectory();
        CancelInvoke();
    }

    /// <summary>
    /// 크로스 즉시 시작
    /// </summary>
    public void StartCrossImmediately()
    {
        CancelInvoke(nameof(StartCross));
        StartCross();
    }
    #endregion

    #region Debug Visualization
    #if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 궤적 및 목표 위치 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        // 목표 위치 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPosition, 0.5f);
        Gizmos.DrawLine(targetPosition + Vector3.up * 3f, targetPosition - Vector3.up * 0.5f);
        
        // 시작 위치에서 목표까지의 직선
        Gizmos.color = Color.yellow;
        Vector3 start = Application.isPlaying ? startPosition : transform.position;
        Gizmos.DrawLine(start, targetPosition);
        
        // 최고점 표시
        Vector3 midPoint = Vector3.Lerp(start, targetPosition, 0.5f);
        midPoint.y = Mathf.Max(start.y, targetPosition.y) + maxHeight;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(midPoint, 0.3f);
        
        // 예상 궤적 표시 (게임 실행 중이 아닐 때)
        if (!Application.isPlaying)
        {
            DrawPredictedTrajectory(start);
        }
        
        // 실제 궤적 표시 (게임 실행 중)
        if (Application.isPlaying && showTrajectory && trajectoryIndex > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 1; i < trajectoryIndex && i < trajectoryPoints.Length; i++)
            {
                if (trajectoryPoints[i - 1] != Vector3.zero && trajectoryPoints[i] != Vector3.zero)
                {
                    Gizmos.DrawLine(trajectoryPoints[i - 1], trajectoryPoints[i]);
                }
            }
        }

        // 상태 정보
        if (Application.isPlaying)
        {
            UnityEditor.Handles.color = Color.white;
            string status = isCrossing ? "크로스 중" : hasReachedTarget ? "완료" : "대기 중";
            float distance = Vector3.Distance(transform.position, targetPosition);
            float elapsedTime = Time.time - crossStartTime;
            string info = $"상태: {status}\n목표까지: {distance:F1}m\n경과 시간: {elapsedTime:F1}s\n속도: {ballRigidbody?.linearVelocity.magnitude:F1}m/s";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, info);
        }
    }

    /// <summary>
    /// 예상 궤적 그리기 (에디터 전용) - 공기 저항 고려한 시뮬레이션
    /// </summary>
    private void DrawPredictedTrajectory(Vector3 startPos)
    {
        if (Application.isPlaying) return;
        
        Gizmos.color = Color.blue;
        
        // 공기 저항을 고려한 속도 계산
        Vector3 displacement = targetPosition - startPos;
        Vector3 horizontalDisplacement = new Vector3(displacement.x, 0, displacement.z);
        float gravity = Mathf.Abs(Physics.gravity.y);
        
        // 공기 저항 보정
        float airResistanceFactor = 1f + (airResistance * 2f);
        Vector3 horizontalVel = horizontalDisplacement.normalized * (horizontalDisplacement.magnitude / flightTime) * airResistanceFactor;
        
        float verticalVel = (displacement.y + 0.5f * gravity * flightTime * flightTime) / flightTime;
        verticalVel = Mathf.Max(verticalVel, Mathf.Sqrt(2 * gravity * maxHeight));
        verticalVel *= (1f + airResistance); // 공기 저항 보정
        
        Vector3 currentPos = startPos;
        Vector3 currentVel = horizontalVel + Vector3.up * verticalVel;
        
        float timeStep = 0.05f; // 더 세밀하게
        int maxSteps = Mathf.RoundToInt((flightTime + 2f) / timeStep);
        
        for (int i = 0; i < maxSteps; i++)
        {
            Vector3 nextPos = currentPos + currentVel * timeStep;
            
            // 중력 적용
            currentVel.y += Physics.gravity.y * timeStep;
            
            // 공기 저항 적용 (시뮬레이션용)
            if (airResistance > 0)
            {
                currentVel -= currentVel * airResistance * timeStep;
            }
            
            Gizmos.DrawLine(currentPos, nextPos);
            currentPos = nextPos;
            
            // 목표 근처 도달하거나 너무 아래로 떨어지면 중단
            if (Vector3.Distance(currentPos, targetPosition) < 1f || currentPos.y < targetPosition.y - 3f)
                break;
        }
    }
    #endif
    #endregion
}