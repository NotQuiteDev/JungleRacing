using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 추가

// 이 enum은 어느 팀의 골대인지를 구분하기 위해 사용됩니다.
public enum Team { Left, Right }

public class SoccerGameManager : MonoBehaviour
{
    // 다른 스크립트에서 쉽게 접근할 수 있도록 싱글톤 인스턴스를 만듭니다.
    public static SoccerGameManager Instance { get; private set; }

    [Header("게임 설정")]
    [Tooltip("가상 게임 시간(분)입니다. 실제 시간과 다릅니다.")]
    public float gameDurationMinutes = 90f;
    [Tooltip("위의 가상 게임 시간이 흘러가는 데 걸리는 실제 시간(분)입니다.")]
    public float realTimeMinutesToCompleteGame = 1.5f; // 1.5분 = 90초
    
    [Tooltip("게임 종료 후 이동할 씬의 이름을 입력하세요.")]
    public string nextSceneName;
    [Tooltip("메인 메뉴로 이동할 씬의 이름을 입력하세요.")]
    public string mainMenuSceneName; // ✨ 메인 메뉴 씬 이름 추가 ✨

    [Header("게임 오브젝트 연결")]
    public GameObject ball;
    public Transform player;
    public Transform ai;
    public Transform leftGoalPost;  // 왼쪽 골대
    public Transform rightGoalPost; // 오른쪽 골대

    // 점수
    private int leftTeamScore = 0;
    private int rightTeamScore = 0;

    // 타이머
    private float gameTimeSeconds = 0f;
    private float timeScale;

    // 게임 상태
    public bool IsGamePlaying { get; private set; } = true;
    private bool isGoalScored = false; 
    private string winnerMessage = "";

    // ✨ 일시정지(메뉴) 상태 추가 ✨
    private bool isPaused = false;

    // 초기 위치 저장용
    private Vector3 ballInitialPos;
    private Vector3 playerInitialPos;
    private Vector3 aiInitialPos;

    [Tooltip("골이 들어간 후 게임이 리셋되기까지 기다리는 시간(초)입니다.")]
    public float resetDelayAfterGoal = 2f;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {

        InputManager.Instance.OnPause += HandlePause;

        // 게임 시작 시 커서를 숨기고 잠급니다. (재시작 시에도 적용)
        ResumeGame(); // 초기 상태는 게임 재개 상태로 시작

        timeScale = (gameDurationMinutes * 60) / (realTimeMinutesToCompleteGame * 60);

        ballInitialPos = ball.transform.position;
        playerInitialPos = player.position;
        aiInitialPos = ai.position;
    }

    void Update()
    {
        // ✨ ESC 키 입력 감지 ✨
        /*if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 게임이 종료된 상태가 아닐 때만 일시정지/재개 토글
            if (IsGamePlaying) 
            {
                if (isPaused)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }*/

        if (IsGamePlaying && !isPaused) // 게임이 플레이 중이고 일시정지 상태가 아닐 때만 시간 흐름
        {
            gameTimeSeconds += Time.deltaTime * timeScale;

            if (gameTimeSeconds >= gameDurationMinutes * 60)
            {
                EndGame();
            }
        }
    }

    private void HandlePause(object sender, EventArgs e)
    {
        if (IsGamePlaying)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    // ✨ 게임 일시정지 함수 ✨
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // 게임 시간 정지
        Cursor.visible = true; // 커서 보이게
        Cursor.lockState = CursorLockMode.None; // 커서 잠금 해제
    }

    // ✨ 게임 재개 함수 ✨
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // 게임 시간 정상화
        Cursor.visible = false; // 커서 숨김
        Cursor.lockState = CursorLockMode.Locked; // 커서 잠금
    }

    public void GoalScored(Team scoringTeam)
    {
        if (isGoalScored) return;

        isGoalScored = true;

        if (scoringTeam == Team.Left) { leftTeamScore++; }
        else if (scoringTeam == Team.Right) { rightTeamScore++; }

        StartCoroutine(ResetAfterGoal());
    }

    private IEnumerator ResetAfterGoal()
    {
        // 골 이후 리셋 대기 중에는 잠시 게임 시간을 0으로 만들지 않습니다.
        // 대신 isGoalScored 플래그로 중복 골 처리를 막고, 캐릭터 움직임만 잠시 멈출 수 있습니다.
        // 여기서는 그냥 전체 게임 흐름을 방해하지 않도록 Time.timeScale은 그대로 둡니다.

        yield return new WaitForSecondsRealtime(resetDelayAfterGoal); // ✨ Realtime 사용 ✨

        ball.transform.position = ballInitialPos;
        ball.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        ball.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        player.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        ai.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        ai.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        isGoalScored = false;
    }

    private void EndGame()
    {
        IsGamePlaying = false; 
        Time.timeScale = 0f; // ✨ 게임 종료 시에도 시간 정지 ✨
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (leftTeamScore > rightTeamScore) { winnerMessage = "Player Wins!"; }
        else if (rightTeamScore > leftTeamScore) { winnerMessage = "A.I. Wins!"; }
        else { winnerMessage = "Draw!"; }
    }

    // 게임 재시작 함수: 현재 씬을 다시 로드합니다.
    public void RestartGame()
    {
        Time.timeScale = 1f; // 씬 로드 전에 시간을 다시 정상화
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 다음 씬으로 이동하는 함수: 인스펙터에서 설정한 씬으로 이동합니다.
    public void GoToNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Time.timeScale = 1f; // 씬 로드 전에 시간을 다시 정상화
            SceneManager.LoadScene(0);
        }
        else
        {
            Debug.LogError("다음 씬 이름이 지정되지 않았습니다! Build Settings에 씬을 추가하고 이름을 확인하세요.");
        }
    }

    // ✨ 메인 메뉴로 이동하는 함수 추가 ✨
    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // 씬 로드 전에 시간을 다시 정상화
        SceneManager.LoadScene(0);

        /*if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            Time.timeScale = 1f; // 씬 로드 전에 시간을 다시 정상화
            SceneManager.LoadScene(0);
        }
        else
        {
            Debug.LogError("메인 메뉴 씬 이름이 지정되지 않았습니다! Build Settings에 씬을 추가하고 이름을 확인하세요.");
        }*/
    }


    // 게임 화면에 텍스트와 버튼을 직접 그리는 GUI 함수
    void OnGUI()
    {
        // --- 텍스트 스타일 설정 ---
        GUIStyle style = new GUIStyle();
        style.fontSize = 30;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = Color.black;
        Rect shadowOffset = new Rect(0, 0, 0, 0);

        // --- 점수 표시 --- (일시정지 중에도 보이게)
        string scoreText = $"{leftTeamScore} : {rightTeamScore}";
        Rect scoreRect = new Rect(Screen.width / 2 - 100, 10, 200, 50);
        shadowOffset.x = scoreRect.x + 2; shadowOffset.y = scoreRect.y + 2;
        shadowOffset.width = scoreRect.width; shadowOffset.height = scoreRect.height;
        GUI.Label(shadowOffset, scoreText, shadowStyle);
        style.normal.textColor = Color.white;
        GUI.Label(scoreRect, scoreText, style);

        // --- 시간 표시 --- (일시정지 중에도 보이게)
        int minutes = (int)(gameTimeSeconds / 60);
        int seconds = (int)(gameTimeSeconds % 60);
        string timerText = $"Time: {minutes:00}:{seconds:00}";
        Rect timerRect = new Rect(Screen.width / 2 - 100, 50, 200, 50);
        shadowOffset.x = timerRect.x + 2; shadowOffset.y = timerRect.y + 2;
        shadowOffset.width = timerRect.width; shadowOffset.height = timerRect.height;
        GUI.Label(shadowOffset, timerText, shadowStyle);
        style.normal.textColor = Color.white;
        GUI.Label(timerRect, timerText, style);

        // --- 조작법 안내 표시 --- (일시정지 중에도 보이게)
        GUIStyle controlStyle = new GUIStyle();
        controlStyle.fontSize = 18;
        controlStyle.fontStyle = FontStyle.Bold;
        controlStyle.alignment = TextAnchor.UpperLeft;
        controlStyle.normal.textColor = Color.white;
        GUIStyle controlShadowStyle = new GUIStyle(controlStyle);
        controlShadowStyle.normal.textColor = Color.black;
        string controlsText = "Move: WASD / Left Stick\n" + 
                              "Camera: Mouse / Right Stick\n" + 
                              "Kick: Spacebar / A Button";
        Rect controlsRect = new Rect(15, 15, 400, 100);
        Rect controlShadowRect = new Rect(controlsRect.x + 2, controlsRect.y + 2, controlsRect.width, controlsRect.height);
        GUI.Label(controlShadowRect, controlsText, controlShadowStyle);
        GUI.Label(controlsRect, controlsText, controlStyle);


        // --- ✨ 게임 종료 시 또는 일시정지 시 메뉴 UI 표시 ✨ ---
        if (!IsGamePlaying || isPaused)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 20;
            buttonStyle.fontStyle = FontStyle.Bold;

            // 결과 메시지 (게임 종료 시에만)
            if (!IsGamePlaying)
            {
                Rect resultRect = new Rect(Screen.width / 2 - 200, Screen.height / 2 - 100, 400, 100);
                string resultText = $"Game Over\n{winnerMessage}";
                shadowOffset.x = resultRect.x + 2; shadowOffset.y = resultRect.y + 2;
                shadowOffset.width = resultRect.width; shadowOffset.height = resultRect.height;
                GUI.Label(shadowOffset, resultText, shadowStyle);
                style.normal.textColor = Color.yellow;
                GUI.Label(resultRect, resultText, style);
            }
            else // 일시정지 중일 때
            {
                // "PAUSED" 메시지
                Rect pausedRect = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 150, 300, 50);
                string pausedText = "PAUSED";
                shadowOffset.x = pausedRect.x + 2; shadowOffset.y = pausedRect.y + 2;
                shadowOffset.width = pausedRect.width; shadowOffset.height = pausedRect.height;
                GUI.Label(shadowOffset, pausedText, shadowStyle);
                style.normal.textColor = Color.cyan; // 일시정지 메시지 색상
                GUI.Label(pausedRect, pausedText, style);
            }

            // ✨ 일시정지/종료 시 버튼 위치 조정 ✨
            float buttonYOffset = !IsGamePlaying ? Screen.height / 2 + 20 : Screen.height / 2 - 50; // 게임 종료 시는 아래, 일시정지는 위

            // '재개하기' 버튼 (게임 종료 시에는 안보이게)
            if (isPaused)
            {
                Rect resumeButtonRect = new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50);
                if (GUI.Button(resumeButtonRect, "재개하기", buttonStyle))
                {
                    ResumeGame();
                }
                buttonYOffset += 60; // 다음 버튼을 위해 Y 오프셋 증가
            }

            // '다시하기' 버튼
            Rect restartButtonRect = new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50);
            if (GUI.Button(restartButtonRect, "다시하기", buttonStyle))
            {
                RestartGame();
            }
            buttonYOffset += 60; // 다음 버튼을 위해 Y 오프셋 증가


            // '다음 씬으로' 버튼 (게임 종료 시에만)
            if (!IsGamePlaying)
            {
                Rect nextSceneButtonRect = new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50);
                if (GUI.Button(nextSceneButtonRect, "다음 씬으로", buttonStyle))
                {
                    GoToNextScene();
                }
                buttonYOffset += 60; // 다음 버튼을 위해 Y 오프셋 증가
            }
            
            // '메인 메뉴' 버튼 (일시정지, 게임 종료 모두)
            Rect mainMenuButtonRect = new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50);
            if (GUI.Button(mainMenuButtonRect, "메인 메뉴", buttonStyle))
            {
                GoToMainMenu();
            }
        }
    }
}