using UnityEngine;

/// <summary>
/// Hinge Joint의 Spring 속성을 이용해 물리 기반 날갯짓을 제어하는 스크립트입니다.
/// 왼쪽 날개와 오른쪽 날개의 목표 각도를 완전히 독립적으로 설정할 수 있습니다.
/// </summary>
public class ChickenJointController : MonoBehaviour
{
    [Header("1. 관절(Joint) 연결")]
    [Tooltip("왼쪽 날개 피벗의 HingeJoint 컴포넌트를 연결하세요.")]
    public HingeJoint leftWingJoint;
    [Tooltip("오른쪽 날개 피벗의 HingeJoint 컴포넌트를 연결하세요.")]
    public HingeJoint rightWingJoint;

    [Header("2. 왼쪽 날개 목표 각도 (A 키)")]
    [Tooltip("왼쪽 날개가 올라갈 목표 각도")]
    public float leftWingUpAngle = 20.0f;
    [Tooltip("왼쪽 날개가 내려갈 목표 각도")]
    public float leftWingDownAngle = -90.0f;

    [Header("3. 오른쪽 날개 목표 각도 (D 키)")]
    [Tooltip("오른쪽 날개가 올라갈 목표 각도")]
    public float rightWingUpAngle = -20.0f;
    [Tooltip("오른쪽 날개가 내려갈 목표 각도")]
    public float rightWingDownAngle = 90.0f;

    void Update()
    {
        // 관절이 연결되었는지 확인
        if (leftWingJoint == null || rightWingJoint == null)
        {
            Debug.LogError("오류: Hinge Joint가 스크립트에 지정되지 않았습니다!");
            return;
        }

        // --- 왼쪽 날개 제어 (완전 분리) ---
        {
            JointSpring leftSpring = leftWingJoint.spring;
            if (Input.GetKey(KeyCode.A))
            {
                leftSpring.targetPosition = leftWingDownAngle; // 왼쪽 날개 전용 '내려갈 각도' 사용
            }
            else
            {
                leftSpring.targetPosition = leftWingUpAngle; // 왼쪽 날개 전용 '올라갈 각도' 사용
            }
            leftWingJoint.spring = leftSpring;
        }

        // --- 오른쪽 날개 제어 (완전 분리) ---
        {
            JointSpring rightSpring = rightWingJoint.spring;
            if (Input.GetKey(KeyCode.D))
            {
                rightSpring.targetPosition = rightWingDownAngle; // 오른쪽 날개 전용 '내려갈 각도' 사용
            }
            else
            {
                rightSpring.targetPosition = rightWingUpAngle; // 오른쪽 날개 전용 '올라갈 각도' 사용
            }
            rightWingJoint.spring = rightSpring;
        }
    }
}