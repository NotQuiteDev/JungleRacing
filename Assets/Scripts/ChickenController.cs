using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ChickenFlapController : MonoBehaviour
{
    [Header("1. 날개 오브젝트 연결")]
    public Transform leftWing;
    public Transform rightWing;

    [Header("2. 왼쪽 날개 설정 (A 키)")]
    public float leftWingUpAngleZ = 20.0f;
    public float leftWingDownAngleZ = -90.0f;
    public float leftFlapDownTension = 10.0f;
    public float leftFlapUpTension = 5.0f;

    [Header("3. 오른쪽 날개 설정 (D 키)")]
    public float rightWingUpAngleZ = -20.0f;
    public float rightWingDownAngleZ = 90.0f;
    public float rightFlapDownTension = 10.0f;
    public float rightFlapUpTension = 5.0f;

    [Header("4. 추진력(Force) 설정")]
    public Transform leftThrusterPoint;
    public Transform rightThrusterPoint;
    public float flapThrust = 250f;
    public float upstrokeResistance = 50f;

    [Header("5. 회전력(Torque) 설정")]
    public float turnTorque = 100f;

    [Header("6. 동적 Z축 저항 (Roll Damping)")]
    public float maxZAxisTorqueDamping = 10f;
    
    private Rigidbody rb;
    private bool isAKeyPressed;
    private bool isDKeyPressed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Update의 역할: 입력 감지 + 시각적 회전 (가장 부드러운 화면을 위해)
        if (leftWing == null || rightWing == null) return;

        // 1. 키 입력 상태를 변수에 저장 (FixedUpdate와 통신하기 위함)
        isAKeyPressed = Input.GetKey(KeyCode.A);
        isDKeyPressed = Input.GetKey(KeyCode.D);

        // 2. 날개의 시각적 회전만 처리
        HandleWingRotation(leftWing, isAKeyPressed, leftWingUpAngleZ, leftWingDownAngleZ, leftFlapUpTension, leftFlapDownTension);
        HandleWingRotation(rightWing, isDKeyPressed, rightWingUpAngleZ, rightWingDownAngleZ, rightFlapUpTension, rightFlapDownTension);
    }
    
    void FixedUpdate()
    {
        // FixedUpdate의 역할: 모든 물리 계산 (가장 안정적인 물리 효과를 위해)
        if (rb == null || leftWing == null || rightWing == null || leftThrusterPoint == null || rightThrusterPoint == null) return;

        // 1. 날갯짓에 따른 상승/저항력 및 회전력 적용
        HandleWingPhysics(leftWing, isAKeyPressed, leftThrusterPoint, leftWingUpAngleZ, leftWingDownAngleZ, true); // 왼쪽: 양의 회전력
        HandleWingPhysics(rightWing, isDKeyPressed, rightThrusterPoint, rightWingUpAngleZ, rightWingDownAngleZ, false); // 오른쪽: 음의 회전력

        // 2. 날개 펼침 정도에 따른 Z축 회전 저항(안정장치) 적용
        ApplyDynamicRollDamping();
    }

    // ==========================================================================================
    // 로직을 명확하게 분리하기 위한 도우미 함수들
    // ==========================================================================================

    /// <summary>
    /// 한쪽 날개의 시각적 회전을 담당하는 함수
    /// </summary>
    void HandleWingRotation(Transform wing, bool isKeyPressed, float upAngle, float downAngle, float upTension, float downTension)
    {
        float targetAngle = isKeyPressed ? downAngle : upAngle;
        float currentTension = isKeyPressed ? downTension : upTension;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
        wing.localRotation = Quaternion.Slerp(wing.localRotation, targetRotation, currentTension * Time.deltaTime);
    }

    /// <summary>
    /// 한쪽 날개의 물리적 힘(상승/저항/회전)을 담당하는 함수
    /// </summary>
    void HandleWingPhysics(Transform wing, bool isKeyPressed, Transform thrusterPoint, float upAngle, float downAngle, bool isLeftWing)
    {
        if (isKeyPressed)
        {
            Quaternion targetRotation = Quaternion.Euler(0, 0, downAngle);
            if (Quaternion.Angle(wing.localRotation, targetRotation) > 1f)
            {
                rb.AddForceAtPosition(transform.up * flapThrust, thrusterPoint.position, ForceMode.Force);
                
                float torqueDirection = isLeftWing ? 1f : -1f; // 왼쪽이면 1, 오른쪽이면 -1
                rb.AddTorque(transform.up * turnTorque * torqueDirection, ForceMode.Force);
            }
        }
        else
        {
            Quaternion targetRotation = Quaternion.Euler(0, 0, upAngle);
            if (Quaternion.Angle(wing.localRotation, targetRotation) > 1f)
            {
                rb.AddForceAtPosition(-transform.up * upstrokeResistance, thrusterPoint.position, ForceMode.Force);
            }
        }
    }

    /// <summary>
    /// 양쪽 날개의 펼쳐진 상태를 기준으로 Z축 회전 저항을 계산하고 적용하는 함수
    /// </summary>
    void ApplyDynamicRollDamping()
    {
        // 왼쪽 날개 펼침 정도 계산 (0.0 ~ 1.0)
        float leftSpreadPercent = GetWingSpreadPercent(leftWing, leftWingUpAngleZ, leftWingDownAngleZ);
        
        // 오른쪽 날개 펼침 정도 계산 (0.0 ~ 1.0)
        float rightSpreadPercent = GetWingSpreadPercent(rightWing, rightWingUpAngleZ, rightWingDownAngleZ);

        // 총 저항력 결정 및 적용
        float totalSpreadPercent = Mathf.Clamp01(leftSpreadPercent + rightSpreadPercent);
        float currentZ_Damping = maxZAxisTorqueDamping * totalSpreadPercent;
        
        Vector3 localAV = transform.InverseTransformDirection(rb.angularVelocity);
        float torqueZ = -localAV.z * currentZ_Damping;
        Vector3 localTorque = new Vector3(0, 0, torqueZ);
        Vector3 worldTorque = transform.TransformDirection(localTorque);
        rb.AddTorque(worldTorque);
    }

    /// <summary>
    /// 한쪽 날개의 펼쳐진 정도를 0~1 사이 값으로 반환하는 함수 (Euler 각도 예외 처리 포함)
    /// </summary>
    float GetWingSpreadPercent(Transform wing, float upAngle, float downAngle)
    {
        // Euler 각도는 0~360 범위를 순환하므로, 음수 각도를 올바르게 비교하기 위해 조정합니다.
        // 예를 들어 -90도는 270도로 표현됩니다.
        float currentAngle = wing.localEulerAngles.z;
        
        // InverseLerp는 min, max 순서가 중요하므로 up/down 각도에 따라 정렬합니다.
        float minAngle = Mathf.Min(upAngle, downAngle);
        float maxAngle = Mathf.Max(upAngle, downAngle);
        
        // 음수 각도에 대한 예외 처리
        if (minAngle < 0 && currentAngle > 180) currentAngle -= 360; // 현재 각도를 음수 범위로 변환
        if (maxAngle < 0) maxAngle += 360; // 비교를 위해 max도 양수 범위로 변환 후 InverseLerp 사용
                                                
        return Mathf.InverseLerp(minAngle, maxAngle, currentAngle);
    }
}