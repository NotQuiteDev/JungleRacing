using Mono.Cecil.Cil;
using UnityEngine;

// 오리를 조종하는 스크립트 (최종 완성 버전!)
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
    public float legForwardAngle = -120f;
    public float legBackwardAngle = -60f;
    public float legMoveSpeed = 250f;

    [Header("Forces")]
    public float propulsionForce = 25f;
    public float turnForce = 15f;
    [Tooltip("앞으로 갈 때 힘의 비율 (0.0 ~ 1.0)")] // [수정됨] 설명 변경
    public float backwardPropulsionModifier = 0.5f; // [수정됨] 이제 앞으로 가는 힘에 적용됨
    [Tooltip("역회전 시 힘의 비율 (0.0 ~ 1.0)")]
    public float reverseTurnModifier = 0.5f;
    [Tooltip("힘 적용 시 최소 속도 임계값")]
    public float minimumVelocityThreshold = 0.001f; // 아주 작은 값으로 시작

    [Header("Web Params")]
    [Tooltip("물갈퀴 펼쳤을 때 X축(좌우) 크기")]
    public float webOpenScaleX = 2f; // 기존 기본값 유지
    [Tooltip("물갈퀴 펼쳤을 때 Y축(상하) 크기")]
    public float webOpenScaleY = 0.5f; // 기존 Y축은 변경 안 했으므로 1f
    [Tooltip("물갈퀴 접었을 때 X축(좌우) 크기")]
    public float webCloseScaleX = 0.5f; // 기존 기본값 유지
    [Tooltip("물갈퀴 접었을 때 Y축(상하) 크기")]
    public float webCloseScaleY = 0.1f; // 기존 Y축은 변경 안 했으므로 1f

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
        // 1단계: 입력 처리 및 물갈퀘 크기 조절
        HandleInputAndWebScaling();

        // 2단계: 다리 움직임 처리 및 정보 반환
        var legMovementInfo = HandleLegMovement();

        // 3단계: 계산된 정보를 바탕으로 물리 효과 적용
        ApplyPhysicsEffects(legMovementInfo);
    }

    private void HandleInputAndWebScaling()
    {
        _leftWebOpen = Input.GetKey(KeyCode.Q);
        _rightWebOpen = Input.GetKey(KeyCode.R); // E → R로 변경

        // X축과 Y축을 각각 관리
        float currentScaleX = _leftWebOpen ? webOpenScaleX : webCloseScaleX;
        float currentScaleY = _leftWebOpen ? webOpenScaleY : webCloseScaleY;
        leftWeb.localScale = new Vector3(currentScaleX, currentScaleY, leftWeb.localScale.z);
        
        currentScaleX = _rightWebOpen ? webOpenScaleX : webCloseScaleX;
        currentScaleY = _rightWebOpen ? webOpenScaleY : webCloseScaleY;
        rightWeb.localScale = new Vector3(currentScaleX, currentScaleY, rightWeb.localScale.z);
    }

    private (float leftVel, float rightVel, bool leftKick, bool rightKick, bool leftRet, bool rightRet) HandleLegMovement()
    {
        float leftTargetAngle = Input.GetKey(KeyCode.W) ? legBackwardAngle : legForwardAngle; // A → W로 변경
        float rightTargetAngle = Input.GetKey(KeyCode.E) ? legBackwardAngle : legForwardAngle; // D → E로 변경

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

        if (leftKick && rightKick)
        {
            Debug.Log("후진");
            body.AddForce(-body.transform.up * propulsionForce /* * backwardPropulsionModifier */, ForceMode.Acceleration);
        }
        else if (leftRet && rightRet)
        {
            Debug.Log("전진");
            body.AddForce(body.transform.up * propulsionForce, ForceMode.Acceleration);
        }
        else if (leftKick)
        {
            Debug.Log("왼발 원상복구");
            body.AddTorque(-Vector3.up * turnForce * Mathf.Abs(leftVel) /* * reverseTurnModifier */, ForceMode.Acceleration);
        }
        else if (rightKick)
        {
            Debug.Log("오른발 원상복구");
            body.AddTorque(Vector3.up * turnForce * Mathf.Abs(rightVel) /* * reverseTurnModifier */, ForceMode.Acceleration);
        }
        else if (leftRet)
        {
            Debug.Log("왼발 킥");
            body.AddTorque(Vector3.up * turnForce * Mathf.Abs(leftVel), ForceMode.Acceleration);

        }
        else if (rightRet)
        {
            Debug.Log("오른발 킥");
            body.AddTorque(-Vector3.up * turnForce * Mathf.Abs(rightVel), ForceMode.Acceleration);
        }
    }
}