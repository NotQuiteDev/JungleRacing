using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    // === 입력 값 변수 ===
    private float steeringInput;
    private float throttleInput;
    private PlayerControls playerControls;

    // === 자동차 제어 변수 ===
    [Header("Car Settings")]
    public float maxSteerAngle = 30f;
    public float forwardSpeed = 500f;
    public float reverseSpeed = 150f;
    public float brakeForce = 300f;
    public float gripFactor = 0.6f;
    public float turnSpeed = 5.0f;

    // === 내부 컴포넌트 및 상태 변수 ===
    private Rigidbody rb;
    private float currentSpeedKPH;

    // 디버그용 변수
    [Header("Debug")]
    public bool showDebugUI = true;
    private string carStatus = "Idle";

    private void Awake()
    {
        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable() { playerControls.Driving.Enable(); }
    private void OnDisable() { playerControls.Driving.Disable(); }

    void Update()
    {
        steeringInput = playerControls.Driving.Move.ReadValue<float>();
        throttleInput = playerControls.Driving.Throttle.ReadValue<float>();
    }

    private void FixedUpdate()
    {
        // 속도 계산을 FixedUpdate 최상단으로 옮겨 모든 로직에서 사용하도록 합니다.
        currentSpeedKPH = Vector3.Dot(rb.linearVelocity, transform.forward) * 3.6f;

        // === 1. 마찰력 로직 (옆 미끄러짐 방지) ===
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        float lateralVelocity = localVelocity.x;
        Vector3 gripForce = -transform.right * lateralVelocity * rb.mass * gripFactor;
        rb.AddForce(gripForce, ForceMode.Acceleration);

        // === 2. 가속/감속 로직 ===
        if (throttleInput > 0.1f) // 전진
        {
            rb.AddForce(transform.forward * throttleInput * forwardSpeed);
            carStatus = "Accelerating";
        }
        else if (throttleInput < -0.1f) // 후진/브레이크
        {
            if (currentSpeedKPH > 1.0f) // 브레이크
            {
                rb.AddForce(-transform.forward * brakeForce);
                carStatus = "Braking";
            }
            else // 후진
            {
                rb.AddForce(transform.forward * throttleInput * reverseSpeed);
                carStatus = "Reversing";
            }
        }
        else
        {
            carStatus = "Idle (Coasting)";
        }
        
        // ★★★ 3. 차체 회전 로직 (수정된 핵심!) ★★★
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            // ★★★ 수정된 부분 ★★★
            // 조향 방향을 결정하는 승수. 현재 속도(currentSpeedKPH)가 0 이상(전진)이면 1, 음수(후진)이면 -1이 됩니다.
            float steerDirectionMultiplier = currentSpeedKPH >= 0 ? 1f : -1f;

            // 목표 회전 각도를 계산합니다.
            // 이제 throttleInput이 아닌, 실제 주행 방향에 따라 조향이 결정됩니다.
            float targetAngle = transform.eulerAngles.y + steeringInput * maxSteerAngle * steerDirectionMultiplier;

            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        }
    }

    // 디버그 UI (기존과 동일)
    private void OnGUI()
    {
        // OnGUI에서도 실시간 속도 계산을 위해 FixedUpdate에서 계산된 값을 사용합니다.
        if (!showDebugUI) return;
        GUIStyle style = new GUIStyle { fontSize = 20, normal = { textColor = Color.white } };
        GUI.Label(new Rect(10, 10, 300, 200),
            $"--- INPUT DEBUG ---\n" +
            $"Throttle Input: {throttleInput:F2}\n" +
            $"Steering Input: {steeringInput:F2}\n\n" +
            $"--- CAR STATUS ---\n" +
            $"Speed: {currentSpeedKPH:F2} km/h\n" +
            $"Status: {carStatus}",
            style);
    }
}