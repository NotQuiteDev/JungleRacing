using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    // === 입력 값 변수 ===
    private float steeringInput;  // A, D 키 입력 (X)
    private float throttleInput;  // W, S 키 입력 (Y)
    private Vector2 moveInput;    // WASD 입력을 Vector2로 받을 변수

    private PlayerInput playerControls; // ✨ 파일 이름에 맞춰 PlayerInput 클래스 사용 ✨

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

    private void Awake()
    {
        // ✨ PlayerInput 클래스로 인스턴스 생성 ✨
        playerControls = new PlayerInput();
        rb = GetComponent<Rigidbody>();
    }

    // ✨ Driving 맵 대신 Player 맵을 활성화/비활성화 합니다. ✨
    private void OnEnable() { playerControls.Player.Enable(); }
    private void OnDisable() { playerControls.Player.Enable(); }

    void Update()
    {
        // ✨ Player 맵의 Move 액션에서 Vector2 값을 읽어옵니다. ✨
        moveInput = playerControls.Player.Move.ReadValue<Vector2>();

        // 읽어온 Vector2 값에서 X는 조향(A,D), Y는 가속/감속(W,S)으로 분리합니다.
        steeringInput = moveInput.x;
        throttleInput = moveInput.y;
    }

    private void FixedUpdate()
    {
        currentSpeedKPH = Vector3.Dot(rb.linearVelocity, transform.forward) * 3.6f;

        // === 1. 마찰력 로직 (옆 미끄러짐 방지) ===
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        float lateralVelocity = localVelocity.x;
        Vector3 gripForce = -transform.right * lateralVelocity * rb.mass * gripFactor;
        rb.AddForce(gripForce, ForceMode.Acceleration);

        // === 2. 가속/감속 로직 (W, S 키) ===
        if (throttleInput > 0.1f) // 전진 (W 키)
        {
            rb.AddForce(transform.forward * throttleInput * forwardSpeed);
        }
        else if (throttleInput < -0.1f) // 후진/브레이크 (S 키)
        {
            if (currentSpeedKPH > 1.0f) // 브레이크
            {
                rb.AddForce(-transform.forward * brakeForce);
            }
            else // 후진
            {
                rb.AddForce(transform.forward * throttleInput * reverseSpeed);
            }
        }
        
        // === 3. 차체 회전 로직 (A, D 키) ===
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            float steerDirectionMultiplier = currentSpeedKPH >= 0 ? 1f : -1f;
            float targetAngle = transform.eulerAngles.y + steeringInput * maxSteerAngle * steerDirectionMultiplier;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        }
    }
}