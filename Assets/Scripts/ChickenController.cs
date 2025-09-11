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
    [Tooltip("날개를 내릴 때 가해지는 고정적인 상승력")]
    public float flapThrust = 250f;

    // [수정] '저항 계수' -> '고정 저항력'으로 변경
    [Tooltip("날개를 올릴 때 가해지는 고정적인 저항력 (아래 방향)")]
    public float upstrokeResistance = 50f;

    // [추가] 회전력을 위한 새로운 Header와 변수
    [Header("5. 회전력(Torque) 설정")]
    [Tooltip("한쪽 날갯짓 시 가해지는 Y축 회전 힘")]
    public float turnTorque = 100f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (leftWing == null || rightWing == null || leftThrusterPoint == null || rightThrusterPoint == null) return;
        
        // --- 왼쪽 날개 로직 ---
        {
            bool isAKeyPressed = Input.GetKey(KeyCode.A);
            float targetAngle = isAKeyPressed ? leftWingDownAngleZ : leftWingUpAngleZ;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            leftWing.localRotation = Quaternion.Slerp(leftWing.localRotation, targetRotation, (isAKeyPressed ? leftFlapDownTension : leftFlapUpTension) * Time.deltaTime);

            if (isAKeyPressed) // 키를 눌렀을 때
            {
                // 날개가 아래로 움직이는 동안, 'flapThrust' 만큼의 고정된 상승력을 줌
                if (Quaternion.Angle(leftWing.localRotation, targetRotation) > 1f)
                {
                    rb.AddForceAtPosition(transform.up * flapThrust, leftThrusterPoint.position, ForceMode.Force);
                    
                    // [추가!] 왼쪽 날갯짓 시 Y축 양의 방향으로 회전력을 줌
                    rb.AddTorque(transform.up * turnTorque, ForceMode.Force);
                }
            }
            else // 키를 뗐을 때
            {
                // [핵심 수정] 속도 계산 없이, 날개가 위로 움직이는 동안 'upstrokeResistance' 만큼의 고정된 저항력을 줌
                if (Quaternion.Angle(leftWing.localRotation, targetRotation) > 1f)
                {
                    rb.AddForceAtPosition(-transform.up * upstrokeResistance, leftThrusterPoint.position, ForceMode.Force);
                }
            }
        }

        // --- 오른쪽 날개 로직 ---
        {
            bool isDKeyPressed = Input.GetKey(KeyCode.D);
            float targetAngle = isDKeyPressed ? rightWingDownAngleZ : rightWingUpAngleZ;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            rightWing.localRotation = Quaternion.Slerp(rightWing.localRotation, targetRotation, (isDKeyPressed ? rightFlapDownTension : rightFlapUpTension) * Time.deltaTime);

            if (isDKeyPressed)
            {
                if (Quaternion.Angle(rightWing.localRotation, targetRotation) > 1f)
                {
                    rb.AddForceAtPosition(transform.up * flapThrust, rightThrusterPoint.position, ForceMode.Force);
                    
                    // [추가!] 오른쪽 날갯짓 시 Y축 음의 방향으로 회전력을 줌
                    rb.AddTorque(-transform.up * turnTorque, ForceMode.Force);
                }
            }
            else
            {
                // [핵심 수정] 여기도 마찬가지로 고정된 저항력을 적용
                if (Quaternion.Angle(rightWing.localRotation, targetRotation) > 1f)
                {
                    rb.AddForceAtPosition(-transform.up * upstrokeResistance, rightThrusterPoint.position, ForceMode.Force);
                }
            }
        }
    }
}