using UnityEngine;

/// <summary>
/// 축구공 크로스 시스템 - 공이 궤적을 그리며 목표 지점으로 날아옴
/// 크로스 시작과 동시에 킥 가능, 플레이어 충돌 시 즉시 킥 실행
/// 월드 좌표계 고정 범위 기반 킥 목표 설정 + 랜덤 크로스 범위 지원
/// 공중 위치 투영 및 지면 마커 표시 기능
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SoccerBallCross : MonoBehaviour
{
    #region Settings
    [Header("== 크로스 설정 ==")]
    [Tooltip("고정 크로스 목표 위치")]
    public Vector3 targetPosition = Vector3.zero;
    
    [Tooltip("크로스 최고 높이")]
    public float maxHeight = 6f;
    
    [Tooltip("크로스 비행 시간 (초)")]
    public float flightTime = 3f;
    
    [Tooltip("크로스 시작 지연 시간")]
    public float delayBeforeStart = 1f;
    
    [Tooltip("자동 크로스 시작 여부")]
    public bool autoStartCross = true;
    
    [Header("== 랜덤 크로스 범위 설정 ==")]
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
    
    [Header("== 크로스 목표 지점 프리팹 설정 ==")]
    [Tooltip("크로스 목표 지점에 표시할 프리팹")]
    public GameObject crossTargetPrefab;
    
    [Tooltip("크로스 목표 프리팹 활성화 여부")]
    public bool enableCrossTargetMarker = true;
    
    [Tooltip("크로스 목표 프리팹을 크로스 완료 후에도 유지할지 여부")]
    public bool keepCrossMarkerAfterComplete = false;
    
    [Tooltip("크로스 목표 프리팹 자동 삭제 시간 (초, 0이면 삭제 안함)")]
    public float crossMarkerLifetime = 10f;
    
    [Header("== 킥 목표 범위 설정 (월드 좌표계 고정) ==")]
    [Tooltip("킥 목표 영역의 중심점 (월드 좌표)")]
    public Vector3 kickAreaCenter = new Vector3(0, 1, 12);
    
    [Tooltip("킥 목표 영역의 X축 범위 (좌우)")]
    public float kickAreaRangeX = 12f;
    
    [Tooltip("킥 목표 영역의 Y축 범위 (상하)")]
    public float kickAreaRangeY = 3f;
    
    [Tooltip("킥 목표 영역의 Z축 범위 (앞뒤)")]
    public float kickAreaRangeZ = 2f;
    
    [Header("== 킥 목표 지점 프리팹 설정 ==")]
    [Tooltip("킥 목표 지점에 표시할 프리팹")]
    public GameObject kickTargetPrefab;
    
    [Tooltip("킥 목표 프리팹 활성화 여부")]
    public bool enableKickTargetMarker = true;
    
    [Tooltip("킥 목표 프리팹을 킥 후에도 유지할지 여부")]
    public bool keepMarkerAfterKick = false;
    
    [Tooltip("킥 목표 프리팹 자동 삭제 시간 (초, 0이면 삭제 안함)")]
    public float markerLifetime = 10f;
    
    [Header("== 지면 투영 설정 ==")]
    [Tooltip("지면 투영 마커 활성화 여부")]
    public bool enableGroundProjection = true;
    
    [Tooltip("지면 투영 마커로 사용할 프리팹")]
    public GameObject groundMarkerPrefab;
    
    [Tooltip("레이캐스트 최대 거리")]
    public float raycastMaxDistance = 100f;
    
    [Tooltip("레이캐스트 대상 레이어 (지면)")]
    public LayerMask groundLayerMask = 1; // Default layer
    
    [Tooltip("마커 업데이트 간격 (초)")]
    public float markerUpdateInterval = 0.1f;
    
    [Header("== 킥 설정 ==")]
    [Tooltip("킥 파워")]
    public float kickPower = 10f;
    
    [Tooltip("킥 Y축 보정 (공을 띄우는 힘)")]
    public float kickYBoost = 2f;
    
    [Tooltip("플레이어 충돌 시 킥 활성화 여부")]
    public bool enableKickOnCollision = true;
    
    [Header("== 물리 설정 ==")]
    [Tooltip("공의 회전 속도")]
    public float spinSpeed = 5f;
    
    [Tooltip("공기 저항 (0=없음, 1=강함)")]
    public float airResistance = 0.05f;
    
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
    
    /// <summary>킥이 가능한 상태인지 여부</summary>
    private bool canKick = false;
    
    /// <summary>이미 킥을 했는지 여부</summary>
    private bool hasKicked = false;
    
    /// <summary>크로스 시작 시간</summary>
    private float crossStartTime;
    
    /// <summary>계산된 초기 속도</summary>
    private Vector3 initialVelocity;
    
    /// <summary>궤적 그리기를 위한 이전 위치들</summary>
    private Vector3[] trajectoryPoints;
    private int trajectoryIndex = 0;
    
    /// <summary>실제 킥 목표 위치 (랜덤 생성됨)</summary>
    private Vector3 actualKickTarget;
    
    /// <summary>실제 크로스 목표 위치 (랜덤 또는 고정)</summary>
    private Vector3 actualCrossTarget;
    
    /// <summary>크로스 목표 마커 인스턴스</summary>
    private GameObject crossTargetMarkerInstance;
    
    /// <summary>킥 목표 마커 인스턴스</summary>
    private GameObject kickTargetMarkerInstance;
    
    /// <summary>지면 투영 마커 인스턴스</summary>
    private GameObject groundMarkerInstance;
    
    /// <summary>현재 지면 투영 위치</summary>
    private Vector3 currentGroundProjection;
    
    /// <summary>마지막 마커 업데이트 시간</summary>
    private float lastMarkerUpdateTime;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        startPosition = transform.position;
        trajectoryPoints = new Vector3[200];
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
    /// 물리 업데이트
    /// </summary>
    private void FixedUpdate()
    {
        if (isCrossing)
        {
            ApplyBallSpin();
            ApplyAirResistance();
        }
    }

    /// <summary>
    /// 매 프레임 업데이트
    /// </summary>
    private void Update()
    {
        if (isCrossing && showTrajectory)
        {
            RecordTrajectoryPoint();
        }

        // 지면 투영 마커 업데이트
        if (enableGroundProjection && isCrossing)
        {
            UpdateGroundProjection();
        }
    }

    /// <summary>
    /// 플레이어와 충돌 시 킥 실행
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && canKick && !hasKicked && enableKickOnCollision)
        {
            ExecuteKick();
        }
    }

    /// <summary>
    /// 컴포넌트 파괴 시 마커 정리
    /// </summary>
    private void OnDestroy()
    {
        CleanupGroundMarker();
        CleanupCrossTargetMarker();
        CleanupKickTargetMarker();
    }
    #endregion

    #region Cross Target Marker System
    /// <summary>
    /// 크로스 목표 마커 생성
    /// </summary>
    /// <param name="position">마커를 배치할 위치</param>
    private void CreateCrossTargetMarker(Vector3 position)
    {
        if (!enableCrossTargetMarker || crossTargetPrefab == null)
        {
            Debug.Log("[SoccerBallCross] 크로스 목표 마커가 비활성화되어 있거나 프리팹이 없습니다.");
            return;
        }

        // 기존 마커가 있으면 제거
        CleanupCrossTargetMarker();

        // 새 마커 생성
        crossTargetMarkerInstance = Instantiate(crossTargetPrefab, position, Quaternion.identity);
        crossTargetMarkerInstance.name = "CrossTargetMarker";
        
        Debug.Log($"[SoccerBallCross] 크로스 목표 마커 생성: {position}");

        // 자동 삭제 설정
        if (crossMarkerLifetime > 0f)
        {
            Invoke(nameof(CleanupCrossTargetMarker), crossMarkerLifetime);
        }
    }

    /// <summary>
    /// 크로스 목표 마커 정리
    /// </summary>
    private void CleanupCrossTargetMarker()
    {
        if (crossTargetMarkerInstance != null)
        {
            if (Application.isPlaying)
            {
                Destroy(crossTargetMarkerInstance);
            }
            else
            {
                DestroyImmediate(crossTargetMarkerInstance);
            }
            crossTargetMarkerInstance = null;
            Debug.Log("[SoccerBallCross] 크로스 목표 마커 정리 완료");
        }
    }

    /// <summary>
    /// 크로스 목표 마커 활성화/비활성화
    /// </summary>
    /// <param name="enable">활성화 여부</param>
    public void SetCrossTargetMarkerEnabled(bool enable)
    {
        enableCrossTargetMarker = enable;
        
        if (!enable)
        {
            CleanupCrossTargetMarker();
        }
        
        Debug.Log($"[SoccerBallCross] 크로스 목표 마커 {(enable ? "활성화" : "비활성화")}");
    }
    #endregion

    #region Kick Target Marker System
    /// <summary>
    /// 킥 목표 마커 생성
    /// </summary>
    /// <param name="position">마커를 배치할 위치</param>
    private void CreateKickTargetMarker(Vector3 position)
    {
        if (!enableKickTargetMarker || kickTargetPrefab == null)
        {
            Debug.Log("[SoccerBallCross] 킥 목표 마커가 비활성화되어 있거나 프리팹이 없습니다.");
            return;
        }

        // 기존 마커가 있으면 제거
        CleanupKickTargetMarker();

        // 새 마커 생성
        kickTargetMarkerInstance = Instantiate(kickTargetPrefab, position, Quaternion.identity);
        kickTargetMarkerInstance.name = "KickTargetMarker";
        
        Debug.Log($"[SoccerBallCross] 킥 목표 마커 생성: {position}");

        // 자동 삭제 설정
        if (markerLifetime > 0f)
        {
            Invoke(nameof(CleanupKickTargetMarker), markerLifetime);
        }
    }

    /// <summary>
    /// 킥 목표 마커 정리
    /// </summary>
    private void CleanupKickTargetMarker()
    {
        if (kickTargetMarkerInstance != null)
        {
            if (Application.isPlaying)
            {
                Destroy(kickTargetMarkerInstance);
            }
            else
            {
                DestroyImmediate(kickTargetMarkerInstance);
            }
            kickTargetMarkerInstance = null;
            Debug.Log("[SoccerBallCross] 킥 목표 마커 정리 완료");
        }
    }

    /// <summary>
    /// 킥 목표 마커 활성화/비활성화
    /// </summary>
    /// <param name="enable">활성화 여부</param>
    public void SetKickTargetMarkerEnabled(bool enable)
    {
        enableKickTargetMarker = enable;
        
        if (!enable)
        {
            CleanupKickTargetMarker();
        }
        
        Debug.Log($"[SoccerBallCross] 킥 목표 마커 {(enable ? "활성화" : "비활성화")}");
    }
    #endregion

    #region Ground Projection System
    /// <summary>
    /// 지면 투영 업데이트
    /// </summary>
    private void UpdateGroundProjection()
    {
        // 업데이트 간격 체크
        if (Time.time - lastMarkerUpdateTime < markerUpdateInterval)
            return;

        lastMarkerUpdateTime = Time.time;

        // 공의 현재 위치에서 수직 아래로 레이캐스트
        Vector3 ballPosition = transform.position;
        Vector3 rayDirection = Vector3.down;

        if (Physics.Raycast(ballPosition, rayDirection, out RaycastHit hit, raycastMaxDistance, groundLayerMask))
        {
            currentGroundProjection = hit.point;
            
            // 마커 생성 또는 업데이트
            CreateOrUpdateGroundMarker(currentGroundProjection);
            
            Debug.DrawLine(ballPosition, currentGroundProjection, Color.red, markerUpdateInterval);
        }
        else
        {
            // 지면을 찾지 못했을 때는 마커 숨기기
            if (groundMarkerInstance != null)
            {
                groundMarkerInstance.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 지면 마커 생성 또는 위치 업데이트
    /// </summary>
    /// <param name="position">마커를 배치할 지면 위치</param>
    private void CreateOrUpdateGroundMarker(Vector3 position)
    {
        if (groundMarkerPrefab == null)
        {
            // 프리팹이 없으면 기본 디버그 표시만
            Debug.DrawRay(position, Vector3.up * 2f, Color.yellow, markerUpdateInterval);
            return;
        }

        // 마커가 없으면 생성
        if (groundMarkerInstance == null)
        {
            groundMarkerInstance = Instantiate(groundMarkerPrefab, position, Quaternion.identity);
            groundMarkerInstance.name = "GroundProjectionMarker";
            Debug.Log($"[SoccerBallCross] 지면 투영 마커 생성: {position}");
        }
        else
        {
            // 마커가 있으면 위치만 업데이트
            groundMarkerInstance.SetActive(true);
            groundMarkerInstance.transform.position = position;
        }
    }

    /// <summary>
    /// 지면 마커 정리
    /// </summary>
    private void CleanupGroundMarker()
    {
        if (groundMarkerInstance != null)
        {
            if (Application.isPlaying)
            {
                Destroy(groundMarkerInstance);
            }
            else
            {
                DestroyImmediate(groundMarkerInstance);
            }
            groundMarkerInstance = null;
            Debug.Log("[SoccerBallCross] 지면 투영 마커 정리 완료");
        }
    }

    /// <summary>
    /// 지면 투영 활성화/비활성화
    /// </summary>
    /// <param name="enable">활성화 여부</param>
    public void SetGroundProjectionEnabled(bool enable)
    {
        enableGroundProjection = enable;
        
        if (!enable)
        {
            CleanupGroundMarker();
        }
        
        Debug.Log($"[SoccerBallCross] 지면 투영 {(enable ? "활성화" : "비활성화")}");
    }

    /// <summary>
    /// 현재 지면 투영 위치 반환
    /// </summary>
    /// <returns>현재 지면 투영 위치</returns>
    public Vector3 GetCurrentGroundProjection()
    {
        return currentGroundProjection;
    }
    #endregion

    #region Cross System
    /// <summary>
    /// 크로스 시작 (랜덤 또는 고정 목표)
    /// </summary>
    public void StartCross()
    {
        if (isCrossing) return;

        isCrossing = true;
        canKick = true; // 크로스 시작하자마자 킥 가능
        hasKicked = false;
        crossStartTime = Time.time;
        startPosition = transform.position;
        
        // 크로스 목표 결정 (랜덤 또는 고정)
        actualCrossTarget = useRandomCross ? GenerateRandomCrossTarget() : targetPosition;
        
        // 크로스 목표 마커 생성
        CreateCrossTargetMarker(actualCrossTarget);
        
        // 궤적 계산 및 실행
        initialVelocity = CalculateCrossVelocity(actualCrossTarget);
        ballRigidbody.linearVelocity = initialVelocity;
        
        Debug.Log($"[SoccerBallCross] 크로스 시작! 목표: {actualCrossTarget} (랜덤: {useRandomCross})");
    }

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
    /// 크로스 궤적 속도 계산
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
        
        // 공기 저항 보정
        float airResistanceFactor = 1f + (airResistance * 2f);
        Vector3 horizontalVelocity = horizontalDisplacement.normalized * (horizontalDistance / flightTime) * airResistanceFactor;
        
        // 수직 속도 계산
        float verticalVelocity = (verticalDisplacement + 0.5f * gravity * flightTime * flightTime) / flightTime;
        float minVerticalVelocity = Mathf.Sqrt(2 * gravity * maxHeight);
        verticalVelocity = Mathf.Max(verticalVelocity, minVerticalVelocity);
        verticalVelocity *= (1f + airResistance);
        
        return horizontalVelocity + Vector3.up * verticalVelocity;
    }

    /// <summary>
    /// 공 회전 효과
    /// </summary>
    private void ApplyBallSpin()
    {
        Vector3 velocity = ballRigidbody.linearVelocity;
        if (velocity.magnitude > 0.1f)
        {
            Vector3 spinAxis = Vector3.Cross(velocity.normalized, Vector3.up).normalized;
            ballRigidbody.angularVelocity = spinAxis * spinSpeed;
        }
    }

    /// <summary>
    /// 공기 저항 적용
    /// </summary>
    private void ApplyAirResistance()
    {
        if (airResistance > 0)
        {
            Vector3 velocity = ballRigidbody.linearVelocity;
            Vector3 resistance = -velocity * airResistance;
            ballRigidbody.AddForce(resistance, ForceMode.Force);
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
    /// 궤적 표시 제거
    /// </summary>
    private void ClearTrajectory()
    {
        trajectoryIndex = 0;
        trajectoryPoints = new Vector3[200];
    }
    #endregion

    #region Kick System
    /// <summary>
    /// 월드 좌표계 고정 범위에서 랜덤 킥 목표 생성
    /// </summary>
    /// <returns>생성된 킥 목표 위치</returns>
    private Vector3 GenerateRandomKickTarget()
    {
        // 월드 좌표계 고정 범위에서 완전 랜덤 생성
        Vector3 randomOffset = new Vector3(
            Random.Range(-kickAreaRangeX / 2f, kickAreaRangeX / 2f),
            Random.Range(0f, kickAreaRangeY), // Y는 항상 양수 (지면 위)
            Random.Range(-kickAreaRangeZ / 2f, kickAreaRangeZ / 2f)
        );

        return kickAreaCenter + randomOffset;
    }

    /// <summary>
    /// 킥 실행 (플레이어 충돌 시 호출)
    /// </summary>
    private void ExecuteKick()
    {
        hasKicked = true;
        canKick = false;
        
        Debug.Log("[SoccerBallCross] 플레이어가 공을 찼습니다!");

        // 월드 좌표계 고정 범위에서 랜덤 목표 생성
        actualKickTarget = GenerateRandomKickTarget();

        // 킥 목표 마커 생성
        CreateKickTargetMarker(actualKickTarget);

        // 킥 방향 계산
        Vector3 kickDirection = actualKickTarget - transform.position;
        kickDirection.y *= kickYBoost; // Y축 보정

        // 킥 실행
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.AddForce(kickDirection.normalized * kickPower, ForceMode.Impulse);

        Debug.Log($"[SoccerBallCross] 킥 목표: {actualKickTarget}");

        // 상태 정리
        isCrossing = false;
        
        // 크로스 목표 마커 정리 (설정에 따라)
        if (!keepCrossMarkerAfterComplete)
        {
            CleanupCrossTargetMarker();
        }
        
        // 지면 마커 정리
        if (enableGroundProjection)
        {
            CleanupGroundMarker();
        }
        
        if (showTrajectory)
        {
            Invoke(nameof(ClearTrajectory), trajectoryDuration);
        }
        
        // 3초 후 리셋
        Invoke(nameof(ResetForNextCross), 3f);
    }

    /// <summary>
    /// 다음 크로스를 위한 리셋
    /// </summary>
    private void ResetForNextCross()
    {
        canKick = false;
        hasKicked = false;
        isCrossing = false;
        CleanupGroundMarker();
        
        // 킥 완료 후 마커 정리 (설정에 따라)
        if (!keepMarkerAfterKick)
        {
            CleanupKickTargetMarker();
        }
        
        Debug.Log("[SoccerBallCross] 다음 크로스 준비 완료");
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
        
        ResetCross();
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
        
        ResetCross();
        StartCross();
    }

    /// <summary>
    /// 기본 설정으로 랜덤 크로스 실행
    /// </summary>
    public void StartRandomCross()
    {
        useRandomCross = true; // 랜덤 모드로 변경
        ResetCross();
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
        Debug.Log($"[SoccerBallCross] 랜덤 크로스 영역 설정: 중심={center}, 범위=({rangeX},{rangeY},{rangeZ})");
    }

    /// <summary>
    /// 킥 목표 영역 설정
    /// </summary>
    public void SetKickArea(Vector3 center, float rangeX, float rangeY, float rangeZ)
    {
        kickAreaCenter = center;
        kickAreaRangeX = rangeX;
        kickAreaRangeY = rangeY;
        kickAreaRangeZ = rangeZ;
        Debug.Log($"[SoccerBallCross] 킥 영역 설정: 중심={center}, 범위=({rangeX},{rangeY},{rangeZ})");
    }

    /// <summary>
    /// 크로스 리셋
    /// </summary>
    public void ResetCross()
    {
        isCrossing = false;
        canKick = false;
        hasKicked = false;
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        ClearTrajectory();
        CleanupGroundMarker();
        CleanupCrossTargetMarker();
        CleanupKickTargetMarker();
        CancelInvoke();
    }

    /// <summary>
    /// 현재 상태 확인
    /// </summary>
    public string GetCurrentStatus()
    {
        if (hasKicked) return "킥 완료";
        if (canKick && isCrossing) return "크로스 중 (킥 가능)";
        if (isCrossing) return "크로스 중";
        return "대기 중";
    }

    /// <summary>
    /// 킥 가능 상태 확인
    /// </summary>
    public bool CanKick() => canKick && !hasKicked;
    
    /// <summary>
    /// 현재 크로스 목표 위치 반환
    /// </summary>
    /// <returns>현재 크로스 목표 위치</returns>
    public Vector3 GetCurrentCrossTarget()
    {
        return actualCrossTarget;
    }
    #endregion

    #region Debug Visualization
    #if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        // 고정 크로스 목표 위치
        if (!useRandomCross)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 0.5f);
        }
        
        // 랜덤 크로스 영역 표시
        if (useRandomCross)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(randomCrossCenter, new Vector3(randomCrossRangeX, randomCrossRangeY, randomCrossRangeZ));
            
            // 랜덤 크로스 중심점
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(randomCrossCenter, 0.3f);
        }
        
        // 실제 크로스 목표 (게임 실행 중)
        if (Application.isPlaying && isCrossing)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(actualCrossTarget, 0.4f);
            Gizmos.DrawLine(transform.position, actualCrossTarget);
            
            // 지면 투영 시각화
            if (enableGroundProjection && currentGroundProjection != Vector3.zero)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentGroundProjection);
                Gizmos.DrawWireSphere(currentGroundProjection, 0.2f);
            }
        }
        
        // 킥 목표 영역 (월드 좌표계 고정)
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(kickAreaCenter, new Vector3(kickAreaRangeX, kickAreaRangeY, kickAreaRangeZ));
        
        // 킥 영역 중심점
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(kickAreaCenter, 0.3f);
        
        // 실제 킥 목표 (킥 후)
        if (Application.isPlaying && hasKicked)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(actualKickTarget, 0.5f);
            Gizmos.DrawLine(transform.position, actualKickTarget);
        }
        
        // 상태 정보
        if (Application.isPlaying)
        {
            UnityEditor.Handles.color = Color.white;
            string crossType = useRandomCross ? "랜덤" : "고정";
            string info = $"[SoccerBallCross]\n상태: {GetCurrentStatus()}\n크로스: {crossType}";
            if (isCrossing)
            {
                info += $"\n크로스 목표: {actualCrossTarget}";
                if (enableGroundProjection && currentGroundProjection != Vector3.zero)
                {
                    info += $"\n지면 투영: {currentGroundProjection}";
                }
            }
            if (hasKicked)
            {
                info += $"\n킥 목표: {actualKickTarget}";
            }
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, info);
        }
    }
    #endif
    #endregion
}