using System.Collections;
using UnityEngine;

/// <summary>
/// 크로스 시퀀스 매니저 - 1번/2번 크로스 위치 순환 및 골 판정 관리
/// </summary>
public class CrossSequenceManager : MonoBehaviour
{
    #region Settings
    [Header("== 크로스 위치 설정 ==")]
    [Tooltip("1번 크로스 위치")]
    public Vector3 position1 = new Vector3(-10, 2, 0);

    [Tooltip("2번 크로스 위치")]
    public Vector3 position2 = new Vector3(10, 2, 0);

    [Header("== 타이밍 설정 ==")]
    [Tooltip("크로스 후 킥 대기 시간 (초)")]
    public float kickWaitTime = 1f;

    [Tooltip("킥 후 골 대기 시간 (초)")]
    public float goalWaitTime = 3f;

    [Header("== 반복 설정 ==")]
    [Tooltip("총 반복 횟수 (1회 = 1번→2번 한 세트)")]
    public int totalCycles = 3;

    [Header("== 컴포넌트 참조 ==")]
    [Tooltip("공 오브젝트")]
    public GameObject ball;

    [Tooltip("골대 트리거")]
    public GoalTrigger goalTrigger;

    [Header("== 시각화 설정 ==")]
    [Tooltip("위치 시각화 여부")]
    public bool showPositions = true;

    [Tooltip("1번 위치 색상")]
    public Color position1Color = Color.blue;

    [Tooltip("2번 위치 색상")]
    public Color position2Color = Color.red;

    [Header("== 디버그 설정 ==")]
    [Tooltip("핵심 로그만 출력 (권장)")]
    public bool essentialLogsOnly = true;

    [Tooltip("크로스 대기 시간 (크로스 완료 후 킥 활성화까지)")]
    public float crossCompleteWaitTime = 0.5f;

    [Header("== 자동 시작 설정 ==")]
    [Tooltip("게임 시작 시 자동으로 시퀀스 시작")]
    public bool autoStartOnAwake = false;

    [Tooltip("시작 지연 시간 (초)")]
    public float startDelay = 2f;

    [Tooltip("위치 전환 대기 시간 (초)")]
    public float positionChangeDelay = 2f;
    #endregion

    #region Events
    /// <summary>시퀀스 시작 이벤트</summary>
    public System.Action OnSequenceStarted;

    /// <summary>시퀀스 완료 이벤트</summary>
    public System.Action OnSequenceCompleted;

    /// <summary>위치 변경 이벤트 (위치, 사이클, 포지션 번호)</summary>
    public System.Action<Vector3, int, int> OnPositionChanged;

    /// <summary>골 성공 이벤트</summary>
    public System.Action OnGoalAchieved;

    /// <summary>킥 타임아웃 이벤트</summary>
    public System.Action OnKickTimeout;
    #endregion

    #region Private Variables
    /// <summary>현재 사이클 (0부터 시작)</summary>
    private int currentCycle = 0;

    /// <summary>현재 위치 (1 또는 2)</summary>
    private int currentPosition = 1;

    /// <summary>시퀀스 진행 중 여부</summary>
    private bool isSequenceActive = false;

    /// <summary>현재 단계 대기 중 여부</summary>
    private bool isWaiting = false;

    /// <summary>킥 실행됨 여부</summary>
    private bool kickExecuted = false;

    /// <summary>골 성공 여부</summary>
    private bool goalScored = false;

    /// <summary>현재 위치 처리 완료 여부</summary>
    private bool positionProcessCompleted = false;

    /// <summary>공 컴포넌트들</summary>
    private CrossSystem crossSystem;
    private KickSystem kickSystem;
    private Rigidbody ballRigidbody;

    /// <summary>현재 진행 중인 코루틴</summary>
    private Coroutine currentSequenceCoroutine;

    /// <summary>디버그용 단계 추적</summary>
    private string currentStepName = "대기중";

    /// <summary>각 단계별 시작 시간 추적</summary>
    private float stepStartTime = 0f;

    /// <summary>다른 시스템들과의 중복 방지</summary>
    private bool isSystemControlled = false;
    #endregion

    #region Properties
    /// <summary>현재 사이클 번호 (1부터 시작)</summary>
    public int CurrentCycle => currentCycle + 1;

    /// <summary>현재 위치 번호</summary>
    public int CurrentPosition => currentPosition;

    /// <summary>시퀀스 활성 상태</summary>
    public bool IsSequenceActive => isSequenceActive;

    /// <summary>현재 위치 좌표</summary>
    public Vector3 CurrentPositionVector => currentPosition == 1 ? position1 : position2;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void Awake()
    {
        if (ball != null)
        {
            crossSystem = ball.GetComponent<CrossSystem>();
            kickSystem = ball.GetComponent<KickSystem>();
            ballRigidbody = ball.GetComponent<Rigidbody>();

            LogEssential($"시스템 초기화 완료 - CrossSystem: {crossSystem != null}, KickSystem: {kickSystem != null}");
        }
    }

    /// <summary>
    /// 이벤트 구독
    /// </summary>
    private void Start()
    {
        // 킥 시스템 이벤트 구독
        if (kickSystem != null)
        {
            kickSystem.OnKickExecuted += OnKickExecuted;
        }

        // 골 트리거 이벤트 구독
        if (goalTrigger != null)
        {
            goalTrigger.OnGoalScored += OnGoalScored;
        }

        // 크로스 시스템 이벤트 구독
        if (crossSystem != null)
        {
            crossSystem.OnCrossStarted += OnCrossStarted;
            crossSystem.OnCrossCompleted += OnCrossCompleted;
        }

        // 기존 SoccerBallCross 완전 비활성화
        DisableOtherSystems();

        // 자동 시작 옵션이 활성화되어 있으면 시퀀스 시작
        if (autoStartOnAwake)
        {
            StartCoroutine(AutoStartSequence());
        }

        LogEssential("크로스 시퀀스 매니저 준비 완료");
    }

    /// <summary>
    /// 이벤트 구독 해제
    /// </summary>
    private void OnDestroy()
    {
        if (kickSystem != null)
        {
            kickSystem.OnKickExecuted -= OnKickExecuted;
        }

        if (goalTrigger != null)
        {
            goalTrigger.OnGoalScored -= OnGoalScored;
        }

        if (crossSystem != null)
        {
            crossSystem.OnCrossStarted -= OnCrossStarted;
            crossSystem.OnCrossCompleted -= OnCrossCompleted;
        }
    }

    /// <summary>
    /// 위치 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        if (showPositions)
        {
            // 1번 위치
            Gizmos.color = position1Color;
            Gizmos.DrawWireSphere(position1, 0.5f);

            // 2번 위치
            Gizmos.color = position2Color;
            Gizmos.DrawWireSphere(position2, 0.5f);

            // 현재 위치 강조
            if (isSequenceActive)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(CurrentPositionVector, 0.7f);
            }

#if UNITY_EDITOR
            // 위치 정보 텍스트
            UnityEditor.Handles.Label(position1 + Vector3.up, $"위치 1\n{position1}");
            UnityEditor.Handles.Label(position2 + Vector3.up, $"위치 2\n{position2}");

            if (isSequenceActive)
            {
                string statusText = $"사이클: {CurrentCycle}/{totalCycles}\n위치: {currentPosition}번\n단계: {currentStepName}";
                if (isWaiting) statusText += "\n(대기 중)";
                if (positionProcessCompleted) statusText += "\n(처리 완료)";

                UnityEditor.Handles.Label(CurrentPositionVector + Vector3.up * 1.5f, statusText);
            }
#endif
        }
    }
    #endregion

    #region Sequence Control
    /// <summary>
    /// 크로스 시퀀스 시작
    /// </summary>
    public void StartSequence()
    {
        if (isSequenceActive)
        {
            Debug.LogWarning("[CrossSequenceManager] 이미 시퀀스가 진행 중입니다.");
            return;
        }

        if (ball == null)
        {
            Debug.LogError("[CrossSequenceManager] 공 오브젝트가 설정되지 않았습니다.");
            return;
        }

        LogEssential("=== 크로스 시퀀스 시작 ===");

        // 초기화
        currentCycle = 0;
        currentPosition = 1;
        isSequenceActive = true;
        isSystemControlled = true;
        ResetAllStates();

        OnSequenceStarted?.Invoke();

        // 시퀀스 코루틴 시작
        if (currentSequenceCoroutine != null)
        {
            StopCoroutine(currentSequenceCoroutine);
        }
        currentSequenceCoroutine = StartCoroutine(SequenceCoroutine());
    }

    /// <summary>
    /// 크로스 시퀀스 중단
    /// </summary>
    public void StopSequence()
    {
        if (!isSequenceActive) return;

        LogEssential("크로스 시퀀스 중단");

        isSequenceActive = false;
        isSystemControlled = false;
        isWaiting = false;

        if (currentSequenceCoroutine != null)
        {
            StopCoroutine(currentSequenceCoroutine);
            currentSequenceCoroutine = null;
        }

        // 진행 중인 크로스/킥 중단
        if (crossSystem != null && crossSystem.IsCrossing)
        {
            crossSystem.StopCross();
        }

        if (kickSystem != null)
        {
            kickSystem.ResetKick();
        }

        ResetAllStates();
        SetCurrentStep("시퀀스 중단 완료");
    }

    /// <summary>
    /// 메인 시퀀스 코루틴
    /// </summary>
    private IEnumerator SequenceCoroutine()
    {
        LogEssential($"총 {totalCycles}사이클 크로스 시퀀스 시작");
        SetCurrentStep("시퀀스 진행 중");

        while (currentCycle < totalCycles && isSequenceActive)
        {
            LogEssential($"▶ 사이클 {currentCycle + 1}/{totalCycles} 시작");

            // 1번 위치 처리
            currentPosition = 1;
            LogEssential($"📍 1번 위치 처리 시작");
            yield return StartCoroutine(ProcessPosition());

            if (!isSequenceActive) break; // 중단되었으면 탈출

            LogEssential($"✅ 1번 위치 처리 완료");

            // 위치 간 전환 대기 (문제점 1 해결)
            LogEssential($"⏳ 위치 전환 대기 ({positionChangeDelay}초)");
            yield return new WaitForSeconds(positionChangeDelay);

            // 2번 위치 처리
            currentPosition = 2;
            LogEssential($"📍 2번 위치 처리 시작");
            yield return StartCoroutine(ProcessPosition());

            if (!isSequenceActive) break; // 중단되었으면 탈출

            LogEssential($"✅ 2번 위치 처리 완료");

            // 사이클 완료
            currentCycle++;
            LogEssential($"🎉 사이클 {currentCycle}/{totalCycles} 완료");

            // 사이클 간 전환 대기
            if (currentCycle < totalCycles)
            {
                LogEssential($"⏳ 다음 사이클 대기 (1초)");
                yield return new WaitForSeconds(1f);
            }
        }

        // 모든 사이클 완료
        if (isSequenceActive)
        {
            CompleteSequence();
        }
    }

    /// <summary>
    /// 각 위치에서의 처리 로직
    /// </summary>
    private IEnumerator ProcessPosition()
    {
        Vector3 targetPosition = CurrentPositionVector;
        positionProcessCompleted = false;

        LogEssential($"🚀 위치 {currentPosition}번으로 이동 시작");
        SetCurrentStep($"위치 {currentPosition}번 이동");

        // 공을 목표 위치로 이동
        MoveBallToPosition(targetPosition);

        // 위치 변경 이벤트 발생
        OnPositionChanged?.Invoke(targetPosition, CurrentCycle, currentPosition);

        // 이동 후 안정화 대기
        yield return new WaitForSeconds(0.5f);

        // 크로스 시작
        LogEssential($"⚽ 위치 {currentPosition}번에서 크로스 시작");
        SetCurrentStep($"위치 {currentPosition}번 크로스");
        yield return StartCoroutine(ExecuteCross(targetPosition));

        // 크로스 완료 후 추가 대기
        yield return new WaitForSeconds(crossCompleteWaitTime);

        // 킥 대기
        LogEssential($"👟 위치 {currentPosition}번에서 킥 대기 시작 ({kickWaitTime}초)");
        SetCurrentStep($"위치 {currentPosition}번 킥 대기");
        yield return StartCoroutine(WaitForKick());

        // 킥이 실행되었다면 골 대기
        if (kickExecuted)
        {
            LogEssential($"🥅 위치 {currentPosition}번에서 골 대기 시작 ({goalWaitTime}초)");
            SetCurrentStep($"위치 {currentPosition}번 골 대기");
            yield return StartCoroutine(WaitForGoal());
        }

        // 상태 리셋
        ResetStepStates();
        positionProcessCompleted = true;

        LogEssential($"🎯 위치 {currentPosition}번 처리 완료");
        SetCurrentStep($"위치 {currentPosition}번 완료");
    }

    /// <summary>
    /// 크로스 실행
    /// </summary>
    private IEnumerator ExecuteCross(Vector3 fromPosition)
    {
        if (crossSystem == null)
        {
            Debug.LogError("[CrossSequenceManager] CrossSystem이 없습니다.");
            yield break;
        }

        // 크로스 시작
        if (crossSystem.useRandomCross)
        {
            crossSystem.StartRandomCross();
        }
        else
        {
            crossSystem.StartCross();
        }

        // 크로스가 시작될 때까지 대기
        float waitTime = 0f;
        while (!crossSystem.IsCrossing && waitTime < 2f)
        {
            waitTime += Time.deltaTime;
            yield return null;
        }

        if (!crossSystem.IsCrossing)
        {
            Debug.LogWarning($"⚠️ 크로스가 시작되지 않았습니다. (위치 {currentPosition}번)");
            yield break;
        }

        // 크로스 완료까지 대기
        while (crossSystem.IsCrossing && isSequenceActive)
        {
            yield return null;
        }

        LogEssential($"✅ 위치 {currentPosition}번 크로스 완료");
    }

    /// <summary>
    /// 킥 대기 (지정 시간 내에 킥이 되지 않으면 다음 위치로)
    /// </summary>
    private IEnumerator WaitForKick()
    {
        kickExecuted = false;
        isWaiting = true;
        float waitTime = 0f;

        // 킥 활성화
        if (kickSystem != null)
        {
            kickSystem.SetKickEnabled(true);
        }

        while (waitTime < kickWaitTime && !kickExecuted && isWaiting && isSequenceActive)
        {
            waitTime += Time.deltaTime;
            yield return null;
        }

        isWaiting = false;

        if (!kickExecuted && isSequenceActive)
        {
            LogEssential($"⏰ 위치 {currentPosition}번 킥 타임아웃 - 다음 위치로 이동");
            OnKickTimeout?.Invoke();

            // 킥 시스템 비활성화
            if (kickSystem != null)
            {
                kickSystem.SetKickEnabled(false);
            }
        }
        else if (kickExecuted)
        {
            LogEssential($"✅ 위치 {currentPosition}번 킥 실행 확인");
        }
    }

    /// <summary>
    /// 골 대기 (지정 시간 내에 골이 되지 않으면 다음 위치로)
    /// </summary>
    private IEnumerator WaitForGoal()
    {
        goalScored = false;
        isWaiting = true;
        float waitTime = 0f;

        while (waitTime < goalWaitTime && !goalScored && isWaiting && isSequenceActive)
        {
            waitTime += Time.deltaTime;
            yield return null;
        }

        isWaiting = false;

        if (goalScored)
        {
            LogEssential($"🎉 위치 {currentPosition}번 골 성공! ({waitTime:F1}초)");
            OnGoalAchieved?.Invoke();
        }
        else if (isSequenceActive)
        {
            LogEssential($"⏰ 위치 {currentPosition}번 골 타임아웃 - 다음 위치로 이동");
        }
    }

    /// <summary>
    /// 시퀀스 완료 처리
    /// </summary>
    private void CompleteSequence()
    {
        LogEssential($"🏁 모든 크로스 시퀀스 완료! (총 {totalCycles}사이클)");

        isSequenceActive = false;
        isSystemControlled = false;
        currentSequenceCoroutine = null;
        SetCurrentStep("시퀀스 완료");

        OnSequenceCompleted?.Invoke();
    }
    #endregion

    #region Event Handlers
    /// <summary>
    /// 크로스 시작 이벤트 핸들러 (문제점 4 해결 - 시스템 제어 시에만 작동)
    /// </summary>
    /// <param name="crossTarget">크로스 목표 위치</param>
    private void OnCrossStarted(Vector3 crossTarget)
    {
        if (!isSequenceActive || !isSystemControlled) return;

        LogEssential($"🚀 위치 {currentPosition}번 크로스 시작됨! 목표: {crossTarget}");
    }

    /// <summary>
    /// 킥 실행 이벤트 핸들러 (문제점 2 해결)
    /// </summary>
    /// <param name="kickTarget">킥 목표 위치</param>
    private void OnKickExecuted(Vector3 kickTarget)
    {
        if (!isSequenceActive || !isSystemControlled) return;

        LogEssential($"👟 위치 {currentPosition}번 킥 실행됨! 목표: {kickTarget}");
        kickExecuted = true;
        isWaiting = false;
    }

    /// <summary>
    /// 골 성공 이벤트 핸들러
    /// </summary>
    private void OnGoalScored()
    {
        if (!isSequenceActive || !isSystemControlled) return;

        LogEssential($"🥅 위치 {currentPosition}번 골 인정됨!");
        goalScored = true;
        isWaiting = false;
    }

    /// <summary>
    /// 크로스 완료 이벤트 핸들러
    /// </summary>
    private void OnCrossCompleted()
    {
        if (!isSequenceActive || !isSystemControlled) return;

        LogEssential($"✅ 위치 {currentPosition}번 크로스 완료됨!");
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// 다른 시스템들 비활성화 (문제점 4 해결)
    /// </summary>
    private void DisableOtherSystems()
    {
        // SoccerBallCross 완전 비활성화
        SoccerBallCross existingCross = ball?.GetComponent<SoccerBallCross>();
        if (existingCross != null)
        {
            existingCross.enabled = false;
            LogEssential("기존 SoccerBallCross 컴포넌트 비활성화");
        }

        // 다른 자동 시스템들도 비활성화 가능
    }

    /// <summary>
    /// 공을 지정 위치로 이동
    /// </summary>
    /// <param name="position">목표 위치</param>
    private void MoveBallToPosition(Vector3 position)
    {
        if (ball == null) return;

        // 물리 상태 초기화
        if (ballRigidbody != null)
        {
            ballRigidbody.linearVelocity = Vector3.zero;
            ballRigidbody.angularVelocity = Vector3.zero;
        }

        // 위치 이동
        ball.transform.position = position;

        // 진행 중인 크로스/킥 중단
        if (crossSystem != null && crossSystem.IsCrossing)
        {
            crossSystem.StopCross();
        }

        if (kickSystem != null)
        {
            kickSystem.ResetKick();
        }

        LogEssential($"📍 공을 위치 {currentPosition}번으로 이동: {position}");
    }

    /// <summary>
    /// 단계별 상태 리셋
    /// </summary>
    private void ResetStepStates()
    {
        kickExecuted = false;
        goalScored = false;
        isWaiting = false;

        // 킥 시스템 비활성화
        if (kickSystem != null)
        {
            kickSystem.SetKickEnabled(false);
        }
    }

    /// <summary>
    /// 모든 상태 리셋
    /// </summary>
    private void ResetAllStates()
    {
        kickExecuted = false;
        goalScored = false;
        isWaiting = false;
        positionProcessCompleted = false;
    }

    /// <summary>
    /// 현재 단계 설정
    /// </summary>
    /// <param name="stepName">단계 이름</param>
    private void SetCurrentStep(string stepName)
    {
        currentStepName = stepName;
        stepStartTime = Time.time;
    }

    /// <summary>
    /// 핵심 로그만 출력 (문제점 3 해결)
    /// </summary>
    /// <param name="message">로그 메시지</param>
    private void LogEssential(string message)
    {
        if (essentialLogsOnly)
        {
            Debug.Log($"[CrossSequence] {message}");
        }
    }

    /// <summary>
    /// 상세 로그 출력 (필요시에만)
    /// </summary>
    /// <param name="message">로그 메시지</param>
    private void LogVerbose(string message)
    {
        if (!essentialLogsOnly)
        {
            Debug.Log($"[CrossSequence-Verbose] {message}");
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 크로스 위치 설정
    /// </summary>
    /// <param name="pos1">1번 위치</param>
    /// <param name="pos2">2번 위치</param>
    public void SetCrossPositions(Vector3 pos1, Vector3 pos2)
    {
        position1 = pos1;
        position2 = pos2;
        LogEssential($"크로스 위치 설정 - 1번: {pos1}, 2번: {pos2}");
    }

    /// <summary>
    /// 타이밍 설정
    /// </summary>
    /// <param name="kickWait">킥 대기 시간</param>
    /// <param name="goalWait">골 대기 시간</param>
    public void SetWaitTimes(float kickWait, float goalWait)
    {
        kickWaitTime = kickWait;
        goalWaitTime = goalWait;
        LogEssential($"타이밍 설정 - 킥 대기: {kickWait}초, 골 대기: {goalWait}초");
    }

    /// <summary>
    /// 반복 횟수 설정
    /// </summary>
    /// <param name="cycles">총 반복 횟수</param>
    public void SetTotalCycles(int cycles)
    {
        totalCycles = cycles;
        LogEssential($"반복 횟수 설정: {cycles}회");
    }

    /// <summary>
    /// 현재 진행 상황 문자열 반환
    /// </summary>
    /// <returns>진행 상황 텍스트</returns>
    public string GetProgressText()
    {
        if (!isSequenceActive)
            return "시퀀스 비활성";

        string status = $"사이클 {CurrentCycle}/{totalCycles} - 위치 {currentPosition}번 - {currentStepName}";
        if (isWaiting) status += " (대기중)";
        if (positionProcessCompleted) status += " (완료)";

        return status;
    }

    /// <summary>
    /// 강제로 다음 위치로 이동 (디버그용)
    /// </summary>
    [ContextMenu("Force Next Position")]
    public void ForceNextPosition()
    {
        if (!isSequenceActive) return;

        LogEssential("🔧 강제로 다음 위치로 이동");
        isWaiting = false;
        kickExecuted = false;
        goalScored = false;
    }

    /// <summary>
    /// 현재 상태 출력 (디버그용)
    /// </summary>
    [ContextMenu("Print Current Status")]
    public void PrintCurrentStatus()
    {
        float elapsedTime = Time.time - stepStartTime;
        LogEssential($"📊 현재 상태:\n" +
                     $"- 시퀀스 활성: {isSequenceActive}\n" +
                     $"- 사이클: {CurrentCycle}/{totalCycles}\n" +
                     $"- 위치: {currentPosition}번\n" +
                     $"- 현재 단계: {currentStepName}\n" +
                     $"- 단계 경과시간: {elapsedTime:F1}초\n" +
                     $"- 시스템 제어: {isSystemControlled}");
    }

    /// <summary>
    /// 외부에서 시퀀스 시작 (UI 버튼 등에서 호출)
    /// </summary>
    [ContextMenu("Start Sequence")]
    public void StartSequenceManual()
    {
        StartSequence();
    }
    #endregion

    #region Coroutines
    /// <summary>
    /// 자동 시작 코루틴
    /// </summary>
    private IEnumerator AutoStartSequence()
    {
        LogEssential($"🕐 {startDelay}초 후 자동 시퀀스 시작 예정");
        yield return new WaitForSeconds(startDelay);
        
        if (!isSequenceActive)
        {
            LogEssential("🚀 자동 시퀀스 시작!");
            StartSequence();
        }
    }
    #endregion
}