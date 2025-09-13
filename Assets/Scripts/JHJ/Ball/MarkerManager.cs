using UnityEngine;

/// <summary>
/// 축구공 크로스 시스템의 모든 마커들을 관리하는 매니저
/// 크로스 목표, 킥 목표, 지면 투영 마커를 통합 관리
/// </summary>
public class MarkerManager : MonoBehaviour
{
    #region Marker Settings
    [Header("== 크로스 목표 마커 설정 ==")]
    [Tooltip("크로스 목표 지점에 표시할 프리팹")]
    public GameObject crossTargetPrefab;

    [Tooltip("크로스 목표 마커 활성화 여부")]
    public bool enableCrossTargetMarker = true;

    [Tooltip("크로스 목표 마커를 크로스 완료 후에도 유지할지 여부")]
    public bool keepCrossMarkerAfterComplete = false;

    [Tooltip("크로스 목표 마커 자동 삭제 시간 (초, 0이면 삭제 안함)")]
    public float crossMarkerLifetime = 10f;

    [Header("== 킥 목표 마커 설정 ==")]
    [Tooltip("킥 목표 지점에 표시할 프리팹")]
    public GameObject kickTargetPrefab;

    [Tooltip("킥 목표 마커 활성화 여부")]
    public bool enableKickTargetMarker = true;

    [Tooltip("킥 목표 마커를 킥 후에도 유지할지 여부")]
    public bool keepKickMarkerAfterKick = false;

    [Tooltip("킥 목표 마커 자동 삭제 시간 (초, 0이면 삭제 안함)")]
    public float kickMarkerLifetime = 10f;

    [Header("== 지면 투영 마커 설정 ==")]
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
    #endregion

    #region Private Variables
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
    /// 컴포넌트 파괴 시 모든 마커 정리
    /// </summary>
    private void OnDestroy()
    {
        CleanupAllMarkers();
    }
    #endregion

    #region Cross Target Marker
    /// <summary>
    /// 크로스 목표 마커 생성
    /// </summary>
    /// <param name="position">마커를 배치할 위치</param>
    public void CreateCrossTargetMarker(Vector3 position)
    {
        if (!enableCrossTargetMarker || crossTargetPrefab == null)
        {
            //Debug.Log("[MarkerManager] 크로스 목표 마커가 비활성화되어 있거나 프리팹이 없습니다.");
            return;
        }

        // 기존 마커가 있으면 제거
        CleanupCrossTargetMarker();

        // 새 마커 생성
        crossTargetMarkerInstance = Instantiate(crossTargetPrefab, position, Quaternion.identity);
        crossTargetMarkerInstance.name = "CrossTargetMarker";

        //Debug.Log($"[MarkerManager] 크로스 목표 마커 생성: {position}");

        // 자동 삭제 설정
        if (crossMarkerLifetime > 0f)
        {
            Invoke(nameof(CleanupCrossTargetMarker), crossMarkerLifetime);
        }
    }

    /// <summary>
    /// 크로스 목표 마커 정리
    /// </summary>
    public void CleanupCrossTargetMarker()
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
            //Debug.Log("[MarkerManager] 크로스 목표 마커 정리 완료");
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

        Debug.Log($"[MarkerManager] 크로스 목표 마커 {(enable ? "활성화" : "비활성화")}");
    }
    #endregion

    #region Kick Target Marker
    /// <summary>
    /// 킥 목표 마커 생성
    /// </summary>
    /// <param name="position">마커를 배치할 위치</param>
    public void CreateKickTargetMarker(Vector3 position)
    {
        if (!enableKickTargetMarker || kickTargetPrefab == null)
        {
            Debug.Log("[MarkerManager] 킥 목표 마커가 비활성화되어 있거나 프리팹이 없습니다.");
            return;
        }

        // 기존 마커가 있으면 제거
        CleanupKickTargetMarker();

        // 새 마커 생성
        kickTargetMarkerInstance = Instantiate(kickTargetPrefab, position, Quaternion.identity);
        kickTargetMarkerInstance.name = "KickTargetMarker";

        Debug.Log($"[MarkerManager] 킥 목표 마커 생성: {position}");

        // 자동 삭제 설정
        if (kickMarkerLifetime > 0f)
        {
            Invoke(nameof(CleanupKickTargetMarker), kickMarkerLifetime);
        }
    }

    /// <summary>
    /// 킥 목표 마커 정리
    /// </summary>
    public void CleanupKickTargetMarker()
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
            Debug.Log("[MarkerManager] 킥 목표 마커 정리 완료");
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

        Debug.Log($"[MarkerManager] 킥 목표 마커 {(enable ? "활성화" : "비활성화")}");
    }
    #endregion

    #region Ground Projection
    /// <summary>
    /// 지면 투영 업데이트
    /// </summary>
    /// <param name="ballPosition">공의 현재 위치</param>
    public void UpdateGroundProjection(Vector3 ballPosition)
    {
        if (!enableGroundProjection) return;

        // 업데이트 간격 체크
        if (Time.time - lastMarkerUpdateTime < markerUpdateInterval)
            return;

        lastMarkerUpdateTime = Time.time;

        // 공의 현재 위치에서 수직 아래로 레이캐스트
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
            //Debug.Log($"[MarkerManager] 지면 투영 마커 생성: {position}");
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
    public void CleanupGroundMarker()
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
           // Debug.Log("[MarkerManager] 지면 투영 마커 정리 완료");
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

        Debug.Log($"[MarkerManager] 지면 투영 {(enable ? "활성화" : "비활성화")}");
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

    #region Cleanup
    /// <summary>
    /// 모든 마커 정리
    /// </summary>
    public void CleanupAllMarkers()
    {
        CleanupCrossTargetMarker();
        CleanupKickTargetMarker();
        CleanupGroundMarker();
        CancelInvoke(); // 모든 Invoke 취소
    }

    /// <summary>
    /// 크로스 완료 후 마커 정리 (설정에 따라)
    /// </summary>
    public void OnCrossCompleted()
    {
        if (!keepCrossMarkerAfterComplete)
        {
            CleanupCrossTargetMarker();
        }
    }

    /// <summary>
    /// 킥 완료 후 마커 정리 (설정에 따라)
    /// </summary>
    public void OnKickCompleted()
    {
        if (!keepKickMarkerAfterKick)
        {
            CleanupKickTargetMarker();
        }
    }
    #endregion
}