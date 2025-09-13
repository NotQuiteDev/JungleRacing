using UnityEngine;

/// <summary>
/// 축구공 킥 시스템 - 플레이어 충돌 시 킥 실행
/// </summary>
public class KickSystem : MonoBehaviour
{
    #region Kick Settings
    [Header("== 킥 목표 범위 설정 (월드 좌표계 고정) ==")]
    [Tooltip("킥 목표 영역의 중심점 (월드 좌표)")]
    public Vector3 kickAreaCenter = new Vector3(0, 1, 12);

    [Tooltip("킥 목표 영역의 X축 범위 (좌우)")]
    public float kickAreaRangeX = 12f;

    [Tooltip("킥 목표 영역의 Y축 범위 (상하)")]
    public float kickAreaRangeY = 3f;

    [Tooltip("킥 목표 영역의 Z축 범위 (앞뒤)")]
    public float kickAreaRangeZ = 2f;

    [Header("== 킥 설정 ==")]
    [Tooltip("킥 파워")]
    public float kickPower = 10f;

    [Tooltip("킥 Y축 보정 (공을 띄우는 힘)")]
    public float kickYBoost = 2f;

    [Tooltip("플레이어 충돌 시 킥 활성화 여부")]
    public bool enableKickOnCollision = true;

    [Header("== 시각화 설정 ==")]
    [Tooltip("킥 범위 시각화 여부")]
    public bool showKickArea = true;

    [Tooltip("킥 범위 와이어프레임 색상")]
    public Color kickAreaColor = Color.red;

    [Tooltip("킥 목표 마커 색상")]
    public Color kickTargetColor = Color.yellow;
    #endregion

    #region Events
    /// <summary>킥 실행 이벤트</summary>
    public System.Action<Vector3> OnKickExecuted;

    /// <summary>킥 완료 이벤트</summary>
    public System.Action OnKickCompleted;
    #endregion

    #region Private Variables
    /// <summary>킥이 가능한 상태인지 여부</summary>
    private bool canKick = false;

    /// <summary>이미 킥을 했는지 여부</summary>
    private bool hasKicked = false;

    /// <summary>실제 킥 목표 위치 (랜덤 생성됨)</summary>
    private Vector3 actualKickTarget;

    /// <summary>공의 Rigidbody 참조</summary>
    private Rigidbody ballRigidbody;

    /// <summary>마커 매니저 참조</summary>
    private MarkerManager markerManager;
    #endregion

    #region Properties
    /// <summary>킥 가능 상태 확인</summary>
    public bool CanKick => canKick && !hasKicked;

    /// <summary>킥 완료 상태 확인</summary>
    public bool HasKicked => hasKicked;

    /// <summary>현재 킥 목표 위치</summary>
    public Vector3 CurrentKickTarget => actualKickTarget;
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
            Debug.LogError("[KickSystem] Rigidbody 컴포넌트가 필요합니다!");
        }

        markerManager = GetComponent<MarkerManager>();
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
    /// 에디터에서 킥 범위 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        if (showKickArea)
        {
            DrawKickArea();
        }
    }

    /// <summary>
    /// 에디터에서 선택 시 킥 범위 시각화
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (showKickArea)
        {
            DrawKickArea();
            DrawKickAreaDetails();
        }
    }
    #endregion

    #region Kick Control
    /// <summary>
    /// 킥 가능 상태 설정
    /// </summary>
    /// <param name="enabled">킥 가능 여부</param>
    public void SetKickEnabled(bool enabled)
    {
        canKick = enabled;
        if (!enabled)
        {
            hasKicked = false; // 킥 비활성화 시 상태 리셋
        }
        Debug.Log($"[KickSystem] 킥 {(enabled ? "활성화" : "비활성화")}");
    }

    /// <summary>
    /// 킥 상태 리셋
    /// </summary>
    public void ResetKick()
    {
        canKick = false;
        hasKicked = false;
        Debug.Log("[KickSystem] 킥 상태 리셋");
    }

    /// <summary>
    /// 킥 실행 (플레이어 충돌 시 호출)
    /// </summary>
    public void ExecuteKick()
    {
        if (!canKick || hasKicked || ballRigidbody == null) return;

        hasKicked = true;
        canKick = false;

        Debug.Log("[KickSystem] 플레이어가 공을 찼습니다!");

        // 월드 좌표계 고정 범위에서 랜덤 목표 생성
        actualKickTarget = GenerateRandomKickTarget();

        // 킥 목표 마커 생성
        if (markerManager != null)
        {
            markerManager.CreateKickTargetMarker(actualKickTarget);
        }

        // 킥 방향 계산
        Vector3 kickDirection = actualKickTarget - transform.position;
        kickDirection.y *= kickYBoost; // Y축 보정

        // 킥 실행
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.AddForce(kickDirection.normalized * kickPower, ForceMode.Impulse);

        Debug.Log($"[KickSystem] 킥 목표: {actualKickTarget}");

        // 이벤트 발생
        OnKickExecuted?.Invoke(actualKickTarget);

        // 3초 후 완료 이벤트
        Invoke(nameof(CompleteKick), 3f);
    }

    /// <summary>
    /// 킥 완료 처리
    /// </summary>
    private void CompleteKick()
    {
        OnKickCompleted?.Invoke();
        if (markerManager != null)
        {
            markerManager.OnKickCompleted();
        }
        Debug.Log("[KickSystem] 킥 완료");
    }
    #endregion

    #region Kick Calculation
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
    #endregion

    #region Visualization
    /// <summary>
    /// 킥 범위 시각화
    /// </summary>
    private void DrawKickArea()
    {
        Gizmos.color = kickAreaColor;
        
        // 킥 범위 박스 그리기
        Vector3 size = new Vector3(kickAreaRangeX, kickAreaRangeY, kickAreaRangeZ);
        Gizmos.DrawWireCube(kickAreaCenter, size);

        // 중심점 표시
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(kickAreaCenter, 0.2f);

        // 킥 목표가 있으면 표시
        if (hasKicked && actualKickTarget != Vector3.zero)
        {
            Gizmos.color = kickTargetColor;
            Gizmos.DrawWireSphere(actualKickTarget, 0.3f);
            
            // 공에서 목표까지 선 그리기
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, actualKickTarget);
        }
    }

    /// <summary>
    /// 킥 범위 상세 정보 표시
    /// </summary>
    private void DrawKickAreaDetails()
    {
        // 범위 경계 표시
        Vector3 min = kickAreaCenter - new Vector3(kickAreaRangeX/2f, 0, kickAreaRangeZ/2f);
        Vector3 max = kickAreaCenter + new Vector3(kickAreaRangeX/2f, kickAreaRangeY, kickAreaRangeZ/2f);

        Gizmos.color = Color.cyan;
        
        // 최소/최대 지점 표시
        Gizmos.DrawWireSphere(min, 0.15f);
        Gizmos.DrawWireSphere(max, 0.15f);
        Gizmos.DrawWireSphere(new Vector3(min.x, max.y, min.z), 0.15f);
        Gizmos.DrawWireSphere(new Vector3(max.x, min.y, max.z), 0.15f);

#if UNITY_EDITOR
        // 범위 정보 텍스트 표시
        UnityEditor.Handles.Label(kickAreaCenter + Vector3.up * 0.5f, 
            $"킥 범위\nX: ±{kickAreaRangeX/2f:F1}\nY: 0~{kickAreaRangeY:F1}\nZ: ±{kickAreaRangeZ/2f:F1}");
#endif
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 킥 목표 영역 설정
    /// </summary>
    public void SetKickArea(Vector3 center, float rangeX, float rangeY, float rangeZ)
    {
        kickAreaCenter = center;
        kickAreaRangeX = rangeX;
        kickAreaRangeY = rangeY;
        kickAreaRangeZ = rangeZ;
        Debug.Log($"[KickSystem] 킥 영역 설정: 중심={center}, 범위=({rangeX},{rangeY},{rangeZ})");
    }

    /// <summary>
    /// 킥 범위 시각화 활성화/비활성화
    /// </summary>
    /// <param name="enable">시각화 활성화 여부</param>
    public void SetKickAreaVisualizationEnabled(bool enable)
    {
        showKickArea = enable;
        Debug.Log($"[KickSystem] 킥 범위 시각화 {(enable ? "활성화" : "비활성화")}");
    }
    #endregion
}