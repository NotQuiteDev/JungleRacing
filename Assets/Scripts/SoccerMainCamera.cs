using UnityEngine;

/// <summary>
/// 축구 게임을 위한 3인칭 추격 카메라입니다.
/// 지정된 타겟(플레이어)의 등 뒤를 따라다니며, 마우스로 시점을 조작할 수 있습니다.
/// </summary>
public class SoccerMainCamera : MonoBehaviour
{
    [Header("Camera Target")]
    [Tooltip("카메라가 따라다닐 메인 대상입니다. (플레이어 오브젝트)")]
    public Transform target;
    [Tooltip("카메라가 함께 화면에 담으려고 노력할 추가 대상입니다. (공 오브젝트)")]
    public Transform ball;

    [Header("Mouse Control Settings")]
    [Tooltip("마우스로 카메라를 조작하는 기능을 켤지 여부입니다.")]
    public bool enableMouseControl = true;
    [Tooltip("마우스 좌우 움직임 감도입니다.")]
    public float sensitivityX = 200f;
    [Tooltip("마우스 상하 움직임 감도입니다.")]
    public float sensitivityY = 150f;
    [Tooltip("카메라의 최소/최대 상하 각도입니다.")]
    public float yAngleMin = -20.0f;
    public float yAngleMax = 80.0f;
    
    [Header("Camera Settings")]
    [Tooltip("타겟으로부터 카메라가 떨어질 거리입니다.")]
    public float distance = 5.0f;
    [Tooltip("타겟의 피봇(회전 중심)보다 얼마나 높이에 카메라를 위치시킬지 결정합니다.")]
    public float heightOffset = 1.5f;
    [Tooltip("카메라가 타겟을 따라갈 때의 부드러움 정도입니다.")]
    public float positionSmoothSpeed = 0.125f;

    [Header("Dynamic Framing")]
    [Tooltip("플레이어와 공을 함께 화면에 담는 동적 프레이밍 기능을 켤지 여부입니다. (마우스 컨트롤 사용 시 비활성화 권장)")]
    public bool enableDynamicFraming = false;
    [Tooltip("카메라 시선이 플레이어와 공 사이에서 어느 쪽에 비중을 둘지 결정합니다.")]
    [Range(0f, 1f)]
    public float ballFramingBias = 0.3f;

    // 내부 변수
    private float currentX = 0.0f;
    private float currentY = 0.0f;
    private Vector3 positionVelocity = Vector3.zero;

    void Start()
    {
        if (enableMouseControl)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- ✨✨✨ 이 부분을 수정했습니다! (가장 중요) ✨✨✨ ---
        // 게임이 현재 플레이 중인지 확인하는 변수
        bool isGameCurrentlyPlaying = false; 

        // 1. 먼저 SoccerGameManager가 있는지 확인합니다.
        if (SoccerGameManager.Instance != null)
        {
            isGameCurrentlyPlaying = SoccerGameManager.Instance.IsGamePlaying;
        }
        // 2. 만약 없다면, CloneSoccerGameManager가 있는지 확인합니다.
        else if (CloneSoccerGameManager.Instance != null)
        {
            isGameCurrentlyPlaying = CloneSoccerGameManager.Instance.IsGamePlaying;
        }
        // (둘 다 없으면 isGameCurrentlyPlaying은 false로 유지됩니다)
        // --- 여기까지 수정 ---

        // --- 마우스 입력 처리 ---
        // 마우스 컨트롤이 활성화 되어있고, '현재 게임이 플레이 중' 상태일 때만 입력을 받습니다.
        if (enableMouseControl && isGameCurrentlyPlaying)
        {
            currentX += Input.GetAxis("Mouse X") * sensitivityX * Time.deltaTime;
            currentY -= Input.GetAxis("Mouse Y") * sensitivityY * Time.deltaTime; // Y축은 반전
            currentY = Mathf.Clamp(currentY, yAngleMin, yAngleMax); // 상하 각도 제한
        }

        // --- 카메라 위치 및 회전 계산 ---
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 targetPivot = target.position + Vector3.up * heightOffset;
        Vector3 desiredPosition = targetPivot - (rotation * Vector3.forward * distance);
        
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothSpeed);

        // --- 카메라가 타겟을 바라보도록 설정 ---
        if (enableMouseControl)
        {
            transform.LookAt(targetPivot);
        }
        else
        {
            Vector3 lookAtPoint;
            if (enableDynamicFraming && ball != null)
            {
                lookAtPoint = Vector3.Lerp(targetPivot, ball.position, ballFramingBias);
            }
            else
            {
                lookAtPoint = targetPivot;
            }
            transform.LookAt(lookAtPoint);
        }
    }
}