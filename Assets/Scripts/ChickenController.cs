using UnityEngine;

/// <summary>
/// 닭의 날갯짓을 제어하는 스크립트입니다.
/// 왼쪽/오른쪽 날개의 모든 설정을 독립적으로 제어할 수 있습니다.
/// </summary>
public class ChickenFlapController : MonoBehaviour
{
    [Header("1. 날개 오브젝트 연결")]
    [Tooltip("왼쪽 날개의 Transform 컴포넌트를 여기에 연결하세요.")]
    public Transform leftWing;
    [Tooltip("오른쪽 날개의 Transform 컴포넌트를 여기에 연결하세요.")]
    public Transform rightWing;

    [Header("2. 왼쪽 날개 설정 (A 키)")]
    [Tooltip("왼쪽 날개가 평소에 있을 위쪽 각도")]
    public float leftWingUpAngleZ = 20.0f;
    [Tooltip("A키를 눌렀을 때 내려갈 아래쪽 각도")]
    public float leftWingDownAngleZ = -90.0f;
    [Tooltip("A키를 눌러 날개를 내릴 때의 속도/강도")]
    public float leftFlapDownTension = 10.0f;
    [Tooltip("A키를 떼서 날개가 올라갈 때의 속도/강도")]
    public float leftFlapUpTension = 5.0f;

    [Header("3. 오른쪽 날개 설정 (D 키)")]
    [Tooltip("오른쪽 날개가 평소에 있을 위쪽 각도 (대칭을 위해 음수 추천)")]
    public float rightWingUpAngleZ = -20.0f;
    [Tooltip("D키를 눌렀을 때 내려갈 아래쪽 각도 (대칭을 위해 양수 추천)")]
    public float rightWingDownAngleZ = 90.0f;
    [Tooltip("D키를 눌러 날개를 내릴 때의 속도/강도")]
    public float rightFlapDownTension = 10.0f;
    [Tooltip("D키를 떼서 날개가 올라갈 때의 속도/강도")]
    public float rightFlapUpTension = 5.0f;


    void Update()
    {
        // 날개 오브젝트가 연결되었는지 확인
        if (leftWing == null || rightWing == null)
        {
            Debug.LogError("오류: 날개 오브젝트가 스크립트에 지정되지 않았습니다!");
            return;
        }

        // --- 왼쪽 날개 로직 (완전 분리) ---
        {
            bool isAKeyPressed = Input.GetKey(KeyCode.A);
            float targetAngle = isAKeyPressed ? leftWingDownAngleZ : leftWingUpAngleZ;
            float currentTension = isAKeyPressed ? leftFlapDownTension : leftFlapUpTension;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            leftWing.localRotation = Quaternion.Slerp(leftWing.localRotation, targetRotation, currentTension * Time.deltaTime);
        }

        // --- 오른쪽 날개 로직 (완전 분리) ---
        {
            bool isDKeyPressed = Input.GetKey(KeyCode.D);
            float targetAngle = isDKeyPressed ? rightWingDownAngleZ : rightWingUpAngleZ;
            float currentTension = isDKeyPressed ? rightFlapDownTension : rightFlapUpTension;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            rightWing.localRotation = Quaternion.Slerp(rightWing.localRotation, targetRotation, currentTension * Time.deltaTime);
        }
    }
}