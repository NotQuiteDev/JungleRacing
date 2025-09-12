using UnityEngine;

/// <summary>
/// 가장 간단한 축구 카메라 - 플레이어를 그냥 따라가기만 함
/// 캐릭터가 화면 중앙 하단에 위치하도록 카메라 머리 위를 중심으로 배치
/// 마우스로 카메라 회전 가능 (즉시 반응, 높이 고정, 각도 제한)
/// 레그돌 상태에서도 정확히 따라가기
/// </summary>
public class SimpleSoccerCamera : MonoBehaviour
{
    #region Settings
    [Header("== 기본 설정 ==")]
    [Tooltip("따라갈 플레이어 (비워두면 자동으로 JHJ_Soccer 찾음)")]
    public Transform target;
    
    [Tooltip("플레이어로부터 뒤쪽 거리")]
    public float distance = 20f;
    
    [Tooltip("플레이어 머리 위 높이 (카메라가 바라볼 지점)")]
    public float headHeight = 1f;
    
    [Tooltip("카메라의 고정 높이 (지면으로부터)")]
    public float fixedCameraHeight = 5f;
    
    [Header("== 마우스 회전 설정 ==")]
    [Tooltip("마우스 좌우 회전 속도")]
    public float mouseXSensitivity = 1f;
    
    [Tooltip("마우스 상하 회전 속도")]
    public float mouseYSensitivity = 1f;
    
    [Tooltip("좌우 회전 각도 제한 (좌측, -값)")]
    public float maxLeftAngle = -100f;
    
    [Tooltip("좌우 회전 각도 제한 (우측, +값)")]
    public float maxRightAngle = 100f;
    
    [Tooltip("상하 각도 제한 (위쪽 - 얼마나 위를 볼 수 있는지)")]
    public float maxVerticalAngle = 5f;
    
    [Tooltip("상하 각도 제한 (아래쪽)")]
    public float minVerticalAngle = -10f;
    
    [Header("== 부드러움 설정 ==")]
    [Tooltip("카메라 따라가기 부드러움 (높을수록 빠름)")]
    public float followSpeed = 5f;
    #endregion

    #region Private Variables
    /// <summary>현재 수평 회전 각도 (Y축) - 초기 위치(등 뒤) 기준 상대 각도</summary>
    private float currentYRotation = 0f;
    
    /// <summary>현재 수직 회전 각도 (X축)</summary>
    private float currentXRotation = 0f;
    
    /// <summary>JHJ_Soccer 참조 (레그dol 상태 확인용)</summary>
    private JHJ_Soccer playerController;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// 초기 설정: 타겟 자동 찾기
    /// </summary>
    private void Start()
    {
        // 타겟이 설정되지 않았으면 자동으로 JHJ_Soccer 찾기
        if (target == null)
        {
            if (JHJ_Soccer.Instance != null)
            {
                target = JHJ_Soccer.Instance.transform;
                playerController = JHJ_Soccer.Instance;
                Debug.Log("[SimpleSoccerCamera] JHJ_Soccer를 타겟으로 자동 설정했습니다.");
            }
            else
            {
                Debug.LogWarning("[SimpleSoccerCamera] JHJ_Soccer를 찾을 수 없습니다!");
            }
        }
        else
        {
            // 수동으로 타겟이 설정된 경우 JHJ_Soccer 컴포넌트 찾기
            playerController = target.GetComponent<JHJ_Soccer>();
        }

        // 초기 회전 각도 설정 (0도 = 등 뒤에서 시작)
        currentYRotation = 0f;
        currentXRotation = 0f;
    }

    /// <summary>
    /// 매 프레임 카메라 업데이트 - 마우스 입력으로 즉시 회전
    /// </summary>
    private void LateUpdate()
    {
        if (target == null) return;

        // 1. 마우스 입력으로 회전 각도 업데이트
        HandleMouseInput();

        // 2. 계산된 각도로 카메라 위치 및 회전 업데이트 (즉시 반응)
        UpdateCameraPositionAndRotation();
    }
    #endregion

    #region Camera Control
    /// <summary>
    /// 마우스 입력 처리 - 좌우 및 상하 회전 (각도 제한 적용)
    /// </summary>
    private void HandleMouseInput()
    {
        // 마우스 입력 받기
        float mouseX = Input.GetAxis("Mouse X") * mouseXSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseYSensitivity;

        // 수평 회전 (Y축) - 좌우 각도 제한 적용
        currentYRotation += mouseX;
        currentYRotation = Mathf.Clamp(currentYRotation, maxLeftAngle, maxRightAngle);

        // 수직 회전 (X축) - 제한된 각도로 위 아래만 살짝
        currentXRotation -= mouseY; // 마우스 Y는 반전
        currentXRotation = Mathf.Clamp(currentXRotation, minVerticalAngle, maxVerticalAngle);
    }

    /// <summary>
    /// 현재 플레이어의 실제 위치 가져오기 (레그돌 상태 고려)
    /// </summary>
    /// <returns>실제 따라가야 할 플레이어 위치</returns>
    private Vector3 GetActualPlayerPosition()
    {
        // JHJ_Soccer가 없다면 기본 transform 위치 사용
        if (playerController == null)
        {
            return target.position;
        }

        // JHJ_Soccer의 ActualPosition 프로퍼티 사용 (레그돌 상태 자동 고려됨)
        return playerController.ActualPosition;
    }

    /// <summary>
    /// 계산된 회전 각도로 카메라 위치 및 회전 업데이트 (즉시 적용)
    /// </summary>
    private void UpdateCameraPositionAndRotation()
    {
        // 실제 플레이어 위치 가져오기 (레그돌 상태 고려)
        Vector3 actualPlayerPosition = GetActualPlayerPosition();
        
        // 플레이어 머리 위 지점 계산 (카메라가 바라볼 중심점)
        Vector3 headPosition = actualPlayerPosition + Vector3.up * headHeight;

        // 회전 각도를 라디안으로 변환 (180도 오프셋 추가로 등 뒤에서 시작)
        float yRad = (currentYRotation + 180f) * Mathf.Deg2Rad;

        // 카메라 위치 계산 (높이는 고정, 수평 회전만)
        Vector3 desiredPosition = actualPlayerPosition;
        desiredPosition.x += distance * Mathf.Sin(yRad);
        desiredPosition.z += distance * Mathf.Cos(yRad);
        desiredPosition.y = fixedCameraHeight; // 고정 높이

        // 부드럽게 위치만 따라가기 (회전은 즉시)
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // 카메라 회전 계산 (즉시 적용)
        Vector3 lookDirection = headPosition - transform.position;
        
        // 수직 각도 조정 적용
        float currentPitch = currentXRotation;
        Quaternion pitchRotation = Quaternion.AngleAxis(currentPitch, Vector3.right);
        
        if (lookDirection != Vector3.zero)
        {
            Quaternion baseLookRotation = Quaternion.LookRotation(lookDirection);
            // 수직 회전 추가 적용
            transform.rotation = baseLookRotation * pitchRotation;
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 새로운 타겟 설정
    /// </summary>
    /// <param name="newTarget">새로운 따라갈 대상</param>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        playerController = target?.GetComponent<JHJ_Soccer>();
        Debug.Log($"[SimpleSoccerCamera] 새로운 타겟 설정: {target?.name}");
    }

    /// <summary>
    /// 카메라 회전 각도 리셋 (등 뒤 초기 위치로)
    /// </summary>
    public void ResetCameraRotation()
    {
        currentYRotation = 0f;
        currentXRotation = 0f;
        Debug.Log("[SimpleSoccerCamera] 카메라 회전 각도 리셋 완료 (등 뒤 위치)");
    }

    /// <summary>
    /// 특정 각도로 카메라 회전 설정
    /// </summary>
    /// <param name="yAngle">수평 회전 각도</param>
    /// <param name="xAngle">수직 회전 각도</param>
    public void SetCameraRotation(float yAngle, float xAngle)
    {
        currentYRotation = Mathf.Clamp(yAngle, maxLeftAngle, maxRightAngle);
        currentXRotation = Mathf.Clamp(xAngle, minVerticalAngle, maxVerticalAngle);
    }

    /// <summary>
    /// 수평 회전 각도 제한 설정
    /// </summary>
    /// <param name="leftLimit">좌측 제한 각도 (-값)</param>
    /// <param name="rightLimit">우측 제한 각도 (+값)</param>
    public void SetHorizontalAngleLimits(float leftLimit, float rightLimit)
    {
        maxLeftAngle = leftLimit;
        maxRightAngle = rightLimit;
        
        // 현재 각도가 새로운 제한을 벗어났다면 조정
        currentYRotation = Mathf.Clamp(currentYRotation, maxLeftAngle, maxRightAngle);
        
        Debug.Log($"[SimpleSoccerCamera] 수평 각도 제한 설정: {leftLimit}° ~ {rightLimit}°");
    }
    #endregion

    #region Debug
    #if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 카메라 시점 시각화
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Vector3 actualPosition = Application.isPlaying ? GetActualPlayerPosition() : target.position;

        // 실제 플레이어 위치 (레그dol 고려)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(actualPosition, 0.5f);

        // 플레이어 머리 위 지점
        Vector3 headPosition = actualPosition + Vector3.up * headHeight;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(headPosition, 0.3f);

        // 고정 높이 평면 표시
        Gizmos.color = Color.blue;
        Vector3 heightPlane = actualPosition;
        heightPlane.y = fixedCameraHeight;
        Gizmos.DrawWireCube(heightPlane, new Vector3(4f, 0.1f, 4f));

        // 카메라와 머리 위 지점 연결선
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, headPosition);

        // 상태 정보
        if (Application.isPlaying)
        {
            UnityEditor.Handles.color = Color.white;
            bool isRagdoll = playerController != null && playerController.IsRagDoll;
            string ragdollStatus = isRagdoll ? "레그돌 상태" : "일반 상태";
            string info = $"상태: {ragdollStatus}\nY 회전: {currentYRotation:F1}°\nX 회전: {currentXRotation:F1}°\n고정 높이: {fixedCameraHeight:F1}m";
            UnityEditor.Handles.Label(transform.position + Vector3.up, info);
        }
    }
    #endif
    #endregion
}