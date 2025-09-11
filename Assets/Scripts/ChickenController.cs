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
    
    // ================== [ 수정된 부분 시작 ] ==================
    [Header("6. 각축별 회전 저항 (Angular Damping)")]
    [Tooltip("기본 회전 저항입니다. X(Pitch), Y(Yaw), Z(Roll) 순서입니다.")]
    public Vector3 baseAngularDrag = new Vector3(0.5f, 0.5f, 1f);

    [Tooltip("날개를 펼쳤을 때 Z축(좌우 기울기)에 추가로 적용될 최대 회전 저항입니다.")]
    public float maxZAxisTorqueDamping = 10f;
    // ================== [ 수정된 부분 끝 ] ==================
    
    [Header("7. 앞/뒤 기울기(Pitch) 설정")]
    public float pitchTorque = 50f;

    private Rigidbody rb;
    private bool isAKeyPressed;
    private bool isDKeyPressed;
    private bool isWKeyPressed;
    private bool isSKeyPressed;

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
        isWKeyPressed = Input.GetKey(KeyCode.W);
        isSKeyPressed = Input.GetKey(KeyCode.S);

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

        // 2. 앞/뒤 기울기(Pitch) 조절 물리 적용
        HandlePitching();
        
        // 3. 커스텀 회전 저항(안정장치) 적용
        ApplyCustomAngularDamping(); // << 함수 이름 변경
    }

    // ==========================================================================================
    // 로직을 명확하게 분리하기 위한 도우미 함수들
    // ==========================================================================================

    void HandleWingRotation(Transform wing, bool isKeyPressed, float upAngle, float downAngle, float upTension, float downTension)
    {
        float targetAngle = isKeyPressed ? downAngle : upAngle;
        float currentTension = isKeyPressed ? downTension : upTension;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
        wing.localRotation = Quaternion.Slerp(wing.localRotation, targetRotation, currentTension * Time.deltaTime);
    }

    void HandleWingPhysics(Transform wing, bool isKeyPressed, Transform thrusterPoint, float upAngle, float downAngle, bool isLeftWing)
    {
        if (isKeyPressed)
        {
            Quaternion targetRotation = Quaternion.Euler(0, 0, downAngle);
            if (Quaternion.Angle(wing.localRotation, targetRotation) > 1f)
            {
                rb.AddForceAtPosition(transform.up * flapThrust, thrusterPoint.position, ForceMode.Force);

                float torqueDirection = isLeftWing ? -1f : 1f;
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

    void HandlePitching()
    {
        if (isWKeyPressed)
        {
            rb.AddTorque(-transform.right * pitchTorque, ForceMode.Force);
        }

        if (isSKeyPressed)
        {
            rb.AddTorque(transform.right * pitchTorque, ForceMode.Force);
        }
    }
    
    // ================== [ 수정된 함수 시작 ] ==================
    void ApplyCustomAngularDamping()
    {
        // --- Z축(Roll)에 대한 동적 저항 계산 (기존 로직) ---
        // 왼쪽 날개 펼침 정도 계산 (0.0 ~ 1.0)
        float leftSpreadPercent = GetWingSpreadPercent(leftWing, leftWingUpAngleZ, leftWingDownAngleZ);

        // 오른쪽 날개 펼침 정도 계산 (0.0 ~ 1.0)
        float rightSpreadPercent = GetWingSpreadPercent(rightWing, rightWingUpAngleZ, rightWingDownAngleZ);

        // 총 펼침 정도에 따른 동적 Z축 저항력 결정
        float totalSpreadPercent = Mathf.Clamp01(leftSpreadPercent + rightSpreadPercent);
        float dynamicZ_Damping = maxZAxisTorqueDamping * totalSpreadPercent;

        // --- 각 축별 최종 저항 계수 계산 ---
        // X, Y 축은 기본 저항만 사용
        float totalDampingX = baseAngularDrag.x;
        float totalDampingY = baseAngularDrag.y;
        // Z 축은 기본 저항에 동적 저항을 더함
        float totalDampingZ = baseAngularDrag.z + dynamicZ_Damping;

        // --- 계산된 저항을 토크로 변환하여 적용 ---
        // 현재 회전 속도를 로컬 좌표 기준으로 변환
        Vector3 localAV = transform.InverseTransformDirection(rb.angularVelocity);
        
        // 각 축의 회전 속도에 반대 방향으로 저항 토크 계산
        float torqueX = -localAV.x * totalDampingX;
        float torqueY = -localAV.y * totalDampingY;
        float torqueZ = -localAV.z * totalDampingZ;

        // 계산된 로컬 토크를 다시 월드 좌표 기준으로 변환하여 적용
        Vector3 localTorque = new Vector3(torqueX, torqueY, torqueZ);
        Vector3 worldTorque = transform.TransformDirection(localTorque);
        rb.AddTorque(worldTorque);
    }
    // ================== [ 수정된 함수 끝 ] ==================

    float GetWingSpreadPercent(Transform wing, float upAngle, float downAngle)
    {
        Quaternion upRot = Quaternion.Euler(0, 0, upAngle);
        Quaternion downRot = Quaternion.Euler(0, 0, downAngle);

        float totalAngleRange = Quaternion.Angle(upRot, downRot);
        if (totalAngleRange < 0.1f) return 0; // 0으로 나누는 오류 방지

        float currentAngleFromDown = Quaternion.Angle(wing.localRotation, downRot);

        return Mathf.Clamp01(currentAngleFromDown / totalAngleRange);
    }
}