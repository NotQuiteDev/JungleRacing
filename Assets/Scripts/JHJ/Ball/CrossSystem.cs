using UnityEngine;

/// <summary>
/// 축구공 크로스 시스템 - 공이 궤적을 그리며 목표 지점으로 날아가는 기능
/// </summary>
public class CrossSystem : MonoBehaviour
{
    #region Cross Settings
    [Header("== 크로스 기본 설정 ==")]
    [Tooltip("고정 크로스 목표 위치")]
    public Vector3 targetPosition = Vector3.zero;

    [Tooltip("크로스 최고 높이")]
    public float maxHeight = 6f;

    [Tooltip("크로스 비행 시간 (초)")]
    public float flightTime = 3f;

    [Tooltip("크로스 시작 지연 시간")]
    public float delayBeforeStart = 1f;

    [Header("== 랜덤 크로스 설정 ==")]
    [Tooltip("랜덤 크로스 사용 여부")]
    public bool useRandomCross = false;

    [Tooltip("랜덤 크로스 영역의 중심점")]
    public Vector3 randomCrossCenter = Vector3.zero;

    [Tooltip("랜덤 크로스 X축 범위 (좌우)")]
    public float randomCrossRangeX = 10f;

    [Tooltip("랜덤 크로스 Y축 범위 (상하)")]
    public float randomCrossRangeY = 2f;

    [Tooltip("랜덤 크로스 Z축 범위 (앞뒤)")]
    public float randomCrossRangeZ = 5f;

    [Header("== 물리 설정 ==")]
    [Tooltip("공기 저항 (0=없음, 1=강함)")]
    public float airResistance = 0.05f;

    [Tooltip("비행 시간 1초당 추가될 공기 저항 보상 값")]
    [Range(0f, 0.1f)]
    public float compensationPerSecond = 0.03f; // 1초당 3%의 힘을 더한다는 의미

    [Tooltip("적용될 수 있는 최대 공기 저항 보상 값")]
    [Range(1f, 1.5f)]
    public float maxCompensation = 1.25f; // 최대 25%의 힘까지만 추가

    [Tooltip("궤적 계산 방식 (true=정확한 포물선, false=기본 방식)")]
    public bool useAccurateTrajectory = true;

    [Header("== 시각화 설정 ==")]
    [Tooltip("크로스 범위 시각화 여부")]
    public bool showCrossArea = true;

    [Tooltip("크로스 범위 와이어프레임 색상")]
    public Color crossAreaColor = Color.blue;

    [Tooltip("크로스 목표 마커 색상")]
    public Color crossTargetColor = Color.green;

    [Tooltip("고정 목표 위치 표시 색상")]
    public Color fixedTargetColor = Color.magenta;

    [Tooltip("예상 궤적 표시 여부")]
    public bool showPredictedPath = true;

    [Tooltip("궤적 포인트 개수")]
    public int trajectoryPoints = 30;
    #endregion

    #region Events
    /// <summary>크로스 시작 이벤트</summary>
    public System.Action<Vector3> OnCrossStarted;

    /// <summary>크로스 완료 이벤트</summary>
    public System.Action OnCrossCompleted;
    #endregion

    #region Private Variables
    /// <summary>크로스 시작 위치</summary>
    private Vector3 startPosition;

    /// <summary>크로스가 진행 중인지 여부</summary>
    private bool isCrossing = false;

    /// <summary>크로스 시작 시간</summary>
    private float crossStartTime;

    /// <summary>계산된 초기 속도</summary>
    private Vector3 initialVelocity;

    /// <summary>실제 크로스 목표 위치 (랜덤 또는 고정)</summary>
    private Vector3 actualCrossTarget;

    /// <summary>공의 Rigidbody 참조</summary>
    private Rigidbody ballRigidbody;

    /// <summary>마커 매니저 참조</summary>
    private MarkerManager markerManager;

    /// <summary>예상 궤적 포인트들</summary>
    private Vector3[] predictedPath;

    /// <summary>실제 최고 높이 (계산된 값)</summary>
    private float calculatedMaxHeight;
    #endregion

    #region Properties
    /// <summary>현재 크로스 진행 중인지 여부</summary>
    public bool IsCrossing => isCrossing;

    /// <summary>현재 크로스 목표 위치</summary>
    public Vector3 CurrentCrossTarget => actualCrossTarget;

    /// <summary>계산된 최고 높이</summary>
    public float CalculatedMaxHeight => calculatedMaxHeight;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        if (ballRigidbody == null)
        {
            Debug.LogError("[CrossSystem] Rigidbody 컴포넌트가 필요합니다!");
        }

        markerManager = GetComponent<MarkerManager>();
        predictedPath = new Vector3[trajectoryPoints];
    }

    /// <summary>
    /// 물리 업데이트
    /// </summary>
    private void FixedUpdate()
    {
        if (isCrossing)
        {
            ApplyAirResistance();
            CheckCrossCompletion();
        }
    }

    /// <summary>
    /// 에디터에서 크로스 범위 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        if (showCrossArea)
        {
            DrawCrossArea();
        }
    }

    /// <summary>
    /// 에디터에서 선택 시 크로스 범위 시각화
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (showCrossArea)
        {
            DrawCrossArea();
            DrawCrossAreaDetails();
        }

        if (showPredictedPath && predictedPath != null)
        {
            DrawPredictedPath();
        }
    }
    #endregion

    #region Cross Control
    /// <summary>
    /// 크로스 시작 (랜덤 또는 고정 목표)
    /// </summary>
    public void StartCross()
    {
        if (isCrossing || ballRigidbody == null) return;

        isCrossing = true;
        crossStartTime = Time.time;
        startPosition = transform.position;

        // 크로스 목표 결정 (랜덤 또는 고정)
        actualCrossTarget = useRandomCross ? GenerateRandomCrossTarget() : targetPosition;

        // 크로스 목표 마커 생성
        if (markerManager != null)
        {
            markerManager.CreateCrossTargetMarker(actualCrossTarget);
        }

        // 궤적 계산 및 실행
        if (useAccurateTrajectory)
        {
            initialVelocity = CalculateAccurateCrossVelocity(actualCrossTarget);
        }
        else
        {
            initialVelocity = CalculateCrossVelocity(actualCrossTarget);
        }

        // 예상 궤적 계산
        CalculatePredictedPath();

        ballRigidbody.linearVelocity = initialVelocity;

        // 이벤트 발생
        OnCrossStarted?.Invoke(actualCrossTarget);

        Debug.Log($"[CrossSystem] 크로스 시작! 목표: {actualCrossTarget} (랜덤: {useRandomCross})");
        Debug.Log($"[CrossSystem] 초기 속도: {initialVelocity}, 예상 최고 높이: {calculatedMaxHeight:F2}m");
    }

    /// <summary>
    /// 크로스 중단
    /// </summary>
    public void StopCross()
    {
        if (!isCrossing) return;

        isCrossing = false;
        if (ballRigidbody != null)
        {
            ballRigidbody.linearVelocity = Vector3.zero;
        }

        OnCrossCompleted?.Invoke();
        if (markerManager != null)
        {
            markerManager.OnCrossCompleted();
        }
        Debug.Log("[CrossSystem] 크로스 중단");
    }

    /// <summary>
    /// 크로스 완료 처리
    /// </summary>
    public void CompleteCross()
    {
        if (!isCrossing) return;

        isCrossing = false;
        OnCrossCompleted?.Invoke();
        if (markerManager != null)
        {
            markerManager.OnCrossCompleted();
        }
        Debug.Log("[CrossSystem] 크로스 완료");
    }

    /// <summary>
    /// 크로스 완료 조건 확인
    /// </summary>
    private void CheckCrossCompletion()
    {
        // 비행 시간 초과 시 완료
        if (Time.time - crossStartTime >= flightTime)
        {
            CompleteCross();
            return;
        }

        // 목표 지점 근처 도달 시 완료
        float distanceToTarget = Vector3.Distance(transform.position, actualCrossTarget);
        if (distanceToTarget <= 1f && ballRigidbody.linearVelocity.y <= 0)
        {
            CompleteCross();
            return;
        }

        // 지면에 닿으면 완료
        if (transform.position.y <= actualCrossTarget.y && ballRigidbody.linearVelocity.y <= 0)
        {
            CompleteCross();
        }
    }
    #endregion

    #region Cross Calculation
    /// <summary>
    /// 랜덤 크로스 목표 생성
    /// </summary>
    /// <returns>생성된 랜덤 크로스 목표 위치</returns>
    private Vector3 GenerateRandomCrossTarget()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-randomCrossRangeX / 2f, randomCrossRangeX / 2f),
            Random.Range(0f, randomCrossRangeY), // Y는 항상 양수 (지면 위)
            Random.Range(-randomCrossRangeZ / 2f, randomCrossRangeZ / 2f)
        );

        return randomCrossCenter + randomOffset;
    }

    /// <summary>
    /// 정확한 크로스 궤적 속도 계산 (최고 높이 기준 + 동적 보정치)
    /// </summary>
    /// <param name="crossTarget">크로스 목표 위치</param>
    private Vector3 CalculateAccurateCrossVelocity(Vector3 crossTarget)
    {
        Vector3 displacement = crossTarget - startPosition;
        float gravity = Mathf.Abs(Physics.gravity.y);

        Vector3 horizontalDisplacement = new Vector3(displacement.x, 0, displacement.z);
        float horizontalDistance = horizontalDisplacement.magnitude;
        float verticalDisplacement = displacement.y;

        float minRequiredHeight = Mathf.Max(startPosition.y, crossTarget.y) + 0.5f;
        float targetMaxHeight = Mathf.Max(maxHeight, minRequiredHeight);

        // 1. '최고 높이' 기준 순수 비행 시간 계산
        float timeToMaxHeight = Mathf.Sqrt(Mathf.Max(0, 2f * (targetMaxHeight - startPosition.y) / gravity));
        float timeFromMaxHeight = Mathf.Sqrt(Mathf.Max(0, 2f * (targetMaxHeight - crossTarget.y) / gravity));
        float calculatedFlightTime = timeToMaxHeight + timeFromMaxHeight;

        if (float.IsNaN(calculatedFlightTime) || calculatedFlightTime <= 0)
        {
            Debug.LogError("[CrossSystem] 비행 시간을 계산할 수 없습니다. 목표 위치나 높이 설정을 확인하세요.");
            return Vector3.zero; // 계산 불가 시 정지
        }

        // 2. 계산된 비행 시간에 맞춰 속도 계산
        Vector3 horizontalVelocity = horizontalDisplacement.normalized * (horizontalDistance / calculatedFlightTime);
        float verticalVelocity = (verticalDisplacement / calculatedFlightTime) + (0.5f * gravity * calculatedFlightTime);

        // ================== 핵심 수정: 동적 보정치 자동 계산 ==================
        float compensation = 1.0f; // 기본값 (보정 없음)
        if (airResistance > 0)
        {
            // 비행 시간에 비례하여 보정치를 계산합니다.
            // 공식: 1 + (비행시간 * 초당 보상 값)
            compensation = 1.0f + (calculatedFlightTime * compensationPerSecond);

            // 설정된 최대 보정치를 넘지 않도록 값을 제한합니다.
            compensation = Mathf.Min(compensation, maxCompensation);
        }
        // ====================================================================

        Vector3 finalVelocity = (horizontalVelocity + Vector3.up * verticalVelocity) * compensation;

        // 계산 결과 업데이트
        calculatedMaxHeight = startPosition.y + (finalVelocity.y * finalVelocity.y) / (2f * gravity);
        this.flightTime = calculatedFlightTime;

        Debug.Log($"[CrossSystem] 궤적 계산(동적 보정) - 비행시간: {calculatedFlightTime:F2}s, 적용된 보정치: {compensation:F2}");

        return finalVelocity;
    }

    /// <summary>
    /// 기존 크로스 궤적 속도 계산 (호환성을 위해 유지)
    /// </summary>
    /// <param name="crossTarget">크로스 목표 위치</param>
    private Vector3 CalculateCrossVelocity(Vector3 crossTarget)
    {
        Vector3 displacement = crossTarget - startPosition;
        float gravity = Mathf.Abs(Physics.gravity.y);

        // 수평 성분
        Vector3 horizontalDisplacement = new Vector3(displacement.x, 0, displacement.z);
        float horizontalDistance = horizontalDisplacement.magnitude;
        float verticalDisplacement = displacement.y;

        // 수평 속도 계산 (공기 저항 보정 제거)
        Vector3 horizontalVelocity = horizontalDisplacement.normalized * (horizontalDistance / flightTime);

        // 수직 속도 계산 개선
        float verticalVelocity = (verticalDisplacement + 0.5f * gravity * flightTime * flightTime) / flightTime;
        float minVerticalVelocity = Mathf.Sqrt(2 * gravity * maxHeight);
        
        // 최고 높이 조건을 더 정확하게 적용
        if (verticalVelocity < minVerticalVelocity)
        {
            verticalVelocity = minVerticalVelocity;
        }

        // 실제 최고 높이 계산
        calculatedMaxHeight = startPosition.y + (verticalVelocity * verticalVelocity) / (2f * gravity);

        return horizontalVelocity + Vector3.up * verticalVelocity;
    }

    /// <summary>
    /// 예상 궤적 계산 (실제 물리와 동일한 공기 저항 적용)
    /// </summary>
    private void CalculatePredictedPath()
    {
        if (predictedPath == null) return;

        Vector3 currentPos = startPosition;
        Vector3 currentVel = initialVelocity;
        float deltaTime = flightTime / trajectoryPoints;
        Vector3 gravity = Physics.gravity;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            predictedPath[i] = currentPos;

            // 중력 적용
            currentVel += gravity * deltaTime;

            // ======== 수정된 공기 저항 계산 ========
            // ApplyAirResistance와 동일한 방식(속도의 제곱에 비례)으로 저항을 계산합니다.
            if (airResistance > 0)
            {
                // 실제 물리 업데이트의 ForceMode.VelocityChange는 속도를 직접 바꾸므로,
                // 여기서는 deltaTime을 곱해 비슷한 효과를 냅니다.
                Vector3 resistance = -currentVel.normalized * currentVel.sqrMagnitude * airResistance * deltaTime;
                currentVel += resistance;
            }
            // =====================================

            // 다음 위치 계산
            currentPos += currentVel * deltaTime;
        }
    }

    /// <summary>
    /// 공기 저항 적용 (기존 방식 개선)
    /// </summary>
    private void ApplyAirResistance()
    {
        if (airResistance > 0 && ballRigidbody != null)
        {
            Vector3 velocity = ballRigidbody.linearVelocity;
            Vector3 resistance = -velocity * velocity.magnitude * airResistance * Time.fixedDeltaTime;
            ballRigidbody.AddForce(resistance, ForceMode.VelocityChange);
        }
    }
    #endregion

    #region Visualization
    /// <summary>
    /// 크로스 범위 시각화
    /// </summary>
    private void DrawCrossArea()
    {
        // 랜덤 크로스 범위 표시 (useRandomCross가 true일 때만)
        if (useRandomCross)
        {
            Gizmos.color = crossAreaColor;
            Vector3 size = new Vector3(randomCrossRangeX, randomCrossRangeY, randomCrossRangeZ);
            Gizmos.DrawWireCube(randomCrossCenter, size);

            // 랜덤 범위 중심점 표시
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(randomCrossCenter, 0.2f);
        }

        // 고정 목표 위치 표시
        if (!useRandomCross || targetPosition != Vector3.zero)
        {
            Gizmos.color = fixedTargetColor;
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
        }

        // 실제 크로스 목표가 있으면 표시
        if (isCrossing && actualCrossTarget != Vector3.zero)
        {
            Gizmos.color = crossTargetColor;
            Gizmos.DrawWireSphere(actualCrossTarget, 0.3f);
            
            // 공에서 목표까지 선 그리기
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, actualCrossTarget);
        }
    }

    /// <summary>
    /// 예상 궤적 그리기
    /// </summary>
    private void DrawPredictedPath()
    {
        if (predictedPath == null || predictedPath.Length < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < predictedPath.Length - 1; i++)
        {
            if (predictedPath[i] != Vector3.zero && predictedPath[i + 1] != Vector3.zero)
            {
                Gizmos.DrawLine(predictedPath[i], predictedPath[i + 1]);
            }
        }

        // 궤적 포인트 표시
        Gizmos.color = Color.red;
        for (int i = 0; i < predictedPath.Length; i += 5)
        {
            if (predictedPath[i] != Vector3.zero)
            {
                Gizmos.DrawWireSphere(predictedPath[i], 0.1f);
            }
        }
    }

    /// <summary>
    /// 크로스 범위 상세 정보 표시
    /// </summary>
    private void DrawCrossAreaDetails()
    {
        if (useRandomCross)
        {
            // 랜덤 범위 경계 표시
            Vector3 min = randomCrossCenter - new Vector3(randomCrossRangeX/2f, 0, randomCrossRangeZ/2f);
            Vector3 max = randomCrossCenter + new Vector3(randomCrossRangeX/2f, randomCrossRangeY, randomCrossRangeZ/2f);

            Gizmos.color = Color.cyan;
            
            // 최소/최대 지점 표시
            Gizmos.DrawWireSphere(min, 0.15f);
            Gizmos.DrawWireSphere(max, 0.15f);
            Gizmos.DrawWireSphere(new Vector3(min.x, max.y, min.z), 0.15f);
            Gizmos.DrawWireSphere(new Vector3(max.x, min.y, max.z), 0.15f);

#if UNITY_EDITOR
            // 랜덤 범위 정보 텍스트 표시
            UnityEditor.Handles.Label(randomCrossCenter + Vector3.up * 0.5f, 
                $"랜덤 크로스 범위\nX: ±{randomCrossRangeX/2f:F1}\nY: 0~{randomCrossRangeY:F1}\nZ: ±{randomCrossRangeZ/2f:F1}");
#endif
        }

#if UNITY_EDITOR
        // 고정 목표 정보 표시
        if (targetPosition != Vector3.zero)
        {
            UnityEditor.Handles.Label(targetPosition + Vector3.up * 0.5f, 
                $"고정 크로스 목표\n{targetPosition}");
        }

        // 계산된 궤적 정보 표시
        if (isCrossing)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"크로스 정보\n최고높이: {calculatedMaxHeight:F1}m\n비행시간: {flightTime:F1}s\n초기속도: {initialVelocity.magnitude:F1}m/s");
        }
#endif
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 새로운 목표로 크로스 실행 (고정 목표)
    /// </summary>
    public void CrossToPosition(Vector3 newTarget, float newFlightTime = -1f)
    {
        useRandomCross = false; // 고정 목표 모드로 변경
        targetPosition = newTarget;
        if (newFlightTime > 0) flightTime = newFlightTime;

        StartCross();
    }

    /// <summary>
    /// 기본 설정으로 랜덤 크로스 실행
    /// </summary>
    public void StartRandomCross()
    {
        useRandomCross = true; // 랜덤 모드로 변경
        StartCross();
    }

    /// <summary>
    /// 랜덤 범위로 크로스 실행
    /// </summary>
    /// <param name="center">랜덤 영역 중심점</param>
    /// <param name="rangeX">X축 범위</param>
    /// <param name="rangeY">Y축 범위</param>
    /// <param name="rangeZ">Z축 범위</param>
    /// <param name="newFlightTime">비행 시간 (선택사항)</param>
    public void StartRandomCross(Vector3 center, float rangeX, float rangeY, float rangeZ, float newFlightTime = -1f)
    {
        useRandomCross = true; // 랜덤 모드로 변경
        randomCrossCenter = center;
        randomCrossRangeX = rangeX;
        randomCrossRangeY = rangeY;
        randomCrossRangeZ = rangeZ;
        if (newFlightTime > 0) flightTime = newFlightTime;

        StartCross();
    }

    /// <summary>
    /// 랜덤 크로스 범위 설정
    /// </summary>
    /// <param name="center">랜덤 영역 중심점</param>
    /// <param name="rangeX">X축 범위</param>
    /// <param name="rangeY">Y축 범위</param>
    /// <param name="rangeZ">Z축 범위</param>
    public void SetRandomCrossArea(Vector3 center, float rangeX, float rangeY, float rangeZ)
    {
        randomCrossCenter = center;
        randomCrossRangeX = rangeX;
        randomCrossRangeY = rangeY;
        randomCrossRangeZ = rangeZ;
        Debug.Log($"[CrossSystem] 랜덤 크로스 영역 설정: 중심={center}, 범위=({rangeX},{rangeY},{rangeZ})");
    }

    /// <summary>
    /// 크로스 범위 시각화 활성화/비활성화
    /// </summary>
    /// <param name="enable">시각화 활성화 여부</param>
    public void SetCrossAreaVisualizationEnabled(bool enable)
    {
        showCrossArea = enable;
        Debug.Log($"[CrossSystem] 크로스 범위 시각화 {(enable ? "활성화" : "비활성화")}");
    }
    #endregion
}