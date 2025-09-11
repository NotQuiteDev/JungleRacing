using UnityEngine;

// 오리를 조종하는 스크립트 (최종 완성 버전)
public class DuckFinalController : MonoBehaviour
{
    // === 게임 오브젝트들을 연결하는 부분 ===
    [Header("References")]
    public Rigidbody body;
    public Transform leftLegPivot;
    public Transform rightLegPivot;
    public Transform leftWeb;
    public Transform rightWeb;

    // === 움직임 설정값들 ===
    [Header("Movement Params")]
    public float legForwardAngle = -120f;      // 다리를 앞으로 뻗을 때 각도 (기본 상태)
    public float legBackwardAngle = -60f;     // 다리를 뒤로 젓을 때 각도 (키를 누를 때)
    public float legMoveSpeed = 250f;

    [Header("Forces")]
    public float propulsionForce = 25f;       // 직진 힘의 세기
    public float turnForce = 15f;           // 회전 힘의 세기
    [Tooltip("후진 시 힘의 비율 (0.0 ~ 1.0)")]
    public float backwardPropulsionModifier = 0.5f; // 후진 힘 감소 비율
    [Tooltip("역회전 시 힘의 비율 (0.0 ~ 1.0)")]
    public float reverseTurnModifier = 0.5f;        // 역회전 힘 감소 비율
    [Tooltip("힘 적용 시 최소 속도 임계값")]
    public float minimumVelocityThreshold = 0.001f; // 너무 작은 움직임 무시

    [Header("Web Params")]
    public float webOpenScale = 2f;
    public float webCloseScale = 0.5f;

    // === 현재 상태를 기억하는 변수들 ===
    private float _currentLeftLegAngle;
    private float _currentRightLegAngle;
    private bool _leftWebOpen = false;
    private bool _rightWebOpen = false;

    void Start()
    {
        _currentLeftLegAngle = legForwardAngle;
        _currentRightLegAngle = legForwardAngle;
    }

    void Update()
    {
        // 1단계: 입력 처리 및 물갈퀴 크기 조절
        HandleInputAndWebScaling();

        // 2단계: 다리 움직임 처리 및 정보 반환
        var legMovementInfo = HandleLegMovement();

        // 3단계: 계산된 정보를 바탕으로 물리 효과 적용
        ApplyPhysicsEffects(legMovementInfo);
    }

    private void HandleInputAndWebScaling()
    {
        _leftWebOpen = Input.GetKey(KeyCode.Q);
        _rightWebOpen = Input.GetKey(KeyCode.E);

        leftWeb.localScale = new Vector3(_leftWebOpen ? webOpenScale : webCloseScale, leftWeb.localScale.y, leftWeb.localScale.z);
        rightWeb.localScale = new Vector3(_rightWebOpen ? webOpenScale : webCloseScale, rightWeb.localScale.y, rightWeb.localScale.z);
    }

    private (float leftVel, float rightVel, bool leftKick, bool rightKick, bool leftRet, bool rightRet) HandleLegMovement()
    {
        float leftTargetAngle = Input.GetKey(KeyCode.A) ? legBackwardAngle : legForwardAngle;
        float rightTargetAngle = Input.GetKey(KeyCode.D) ? legBackwardAngle : legForwardAngle;

        float previousLeftAngle = _currentLeftLegAngle;
        float previousRightAngle = _currentRightLegAngle;

        _currentLeftLegAngle = Mathf.MoveTowardsAngle(_currentLeftLegAngle, leftTargetAngle, legMoveSpeed * Time.deltaTime);
        _currentRightLegAngle = Mathf.MoveTowardsAngle(_currentRightLegAngle, rightTargetAngle, legMoveSpeed * Time.deltaTime);

        leftLegPivot.localRotation = Quaternion.Euler(_currentLeftLegAngle, 0, 0);
        rightLegPivot.localRotation = Quaternion.Euler(_currentRightLegAngle, 0, 0);

        float leftAngularVelocity = _currentLeftLegAngle - previousLeftAngle;
        float rightAngularVelocity = _currentRightLegAngle - previousRightAngle;

        bool isLeftKicking = _leftWebOpen && leftAngularVelocity < -minimumVelocityThreshold;
        bool isRightKicking = _rightWebOpen && rightAngularVelocity < -minimumVelocityThreshold;
        bool isLeftReturning = _leftWebOpen && leftAngularVelocity > minimumVelocityThreshold;
        bool isRightReturning = _rightWebOpen && rightAngularVelocity > minimumVelocityThreshold;

        return (leftAngularVelocity, rightAngularVelocity, isLeftKicking, isRightKicking, isLeftReturning, isRightReturning);
    }

    private void ApplyPhysicsEffects((float leftVel, float rightVel, bool leftKick, bool rightKick, bool leftRet, bool rightRet) movement)
    {
        var (leftVel, rightVel, leftKick, rightKick, leftRet, rightRet) = movement;

        if (leftKick && rightKick) // 최우선: 직진
        {
            body.AddForce(transform.parent.forward * propulsionForce, ForceMode.Acceleration);
        }
        else if (leftRet && rightRet) // 2순위: 후진
        {
            body.AddForce(-transform.parent.forward * propulsionForce * backwardPropulsionModifier, ForceMode.Acceleration);
        }
        else if (leftKick) // 3순위: 왼발로 차기 = 오른쪽 회전 (시계방향)
        {
            body.AddTorque(-Vector3.up * turnForce * Mathf.Abs(leftVel), ForceMode.Acceleration);
        }
        else if (rightKick) // 4순위: 오른발로 차기 = 왼쪽 회전 (반시계방향)
        {
            body.AddTorque(Vector3.up * turnForce * Mathf.Abs(rightVel), ForceMode.Acceleration);
        }
        else if (leftRet) // 5순위: 왼발 복귀 = 왼쪽 역회전 (반시계방향)
        {
            body.AddTorque(Vector3.up * turnForce * Mathf.Abs(leftVel) * reverseTurnModifier, ForceMode.Acceleration);
        }
        else if (rightRet) // 6순위: 오른발 복귀 = 오른쪽 역회전 (시계방향)
        {
            body.AddTorque(-Vector3.up * turnForce * Mathf.Abs(rightVel) * reverseTurnModifier, ForceMode.Acceleration);
        }
    }
}