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
    public float flapForce = 100f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (leftWing == null || rightWing == null || leftThrusterPoint == null || rightThrusterPoint == null)
        {
            Debug.LogError("오류: 스크립트에 필요한 모든 Transform이 지정되지 않았습니다!");
            return;
        }

        // --- 왼쪽 날개 로직 ---
        {
            // 날개 회전은 키를 '누르고 있는 동안' 계속 유지 (GetKey 사용)
            bool isAKeyPressed = Input.GetKey(KeyCode.A);
            float targetAngle = isAKeyPressed ? leftWingDownAngleZ : leftWingUpAngleZ;
            float currentTension = isAKeyPressed ? leftFlapDownTension : leftFlapUpTension;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            leftWing.localRotation = Quaternion.Slerp(leftWing.localRotation, targetRotation, currentTension * Time.deltaTime);

            // [수정] 힘은 키를 '처음 눌렀을 때' 딱 한 번만 가함 (GetKeyDown 사용)
            if (Input.GetKeyDown(KeyCode.A))
            {
                // [수정] ForceMode.Impulse 로 변경하여 순간적인 힘을 가함
                rb.AddForceAtPosition(transform.up * flapForce, leftThrusterPoint.position, ForceMode.Impulse);
            }
        }

        // --- 오른쪽 날개 로직 ---
        {
            // 날개 회전은 키를 '누르고 있는 동안' 계속 유지 (GetKey 사용)
            bool isDKeyPressed = Input.GetKey(KeyCode.D);
            float targetAngle = isDKeyPressed ? rightWingDownAngleZ : rightWingUpAngleZ;
            float currentTension = isDKeyPressed ? rightFlapDownTension : rightFlapUpTension;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            rightWing.localRotation = Quaternion.Slerp(rightWing.localRotation, targetRotation, currentTension * Time.deltaTime);

            // [수정] 힘은 키를 '처음 눌렀을 때' 딱 한 번만 가함 (GetKeyDown 사용)
            if (Input.GetKeyDown(KeyCode.D))
            {
                // [수정] ForceMode.Impulse 로 변경하여 순간적인 힘을 가함
                rb.AddForceAtPosition(transform.up * flapForce, rightThrusterPoint.position, ForceMode.Impulse);
            }
        }
    }
}