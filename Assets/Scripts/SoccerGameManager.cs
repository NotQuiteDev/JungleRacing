using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
// 새로운 Input System 사용을 위해 네임스페이스 추가
using UnityEngine.InputSystem;

public enum Team { Left, Right }

public class SoccerGameManager : MonoBehaviour
{
    public static SoccerGameManager Instance { get; private set; }

    [Header("게임 설정")]
    public float gameDurationMinutes = 90f;
    public float realTimeMinutesToCompleteGame = 1.5f;
    public string nextSceneName;
    public string mainMenuSceneName;
    public float resetDelayAfterGoal = 2f;

    [Header("게임 오브젝트 연결")]
    public GameObject ball;
    public Transform player;
    public Transform ai;
    public Transform leftGoalPost;
    public Transform rightGoalPost;

    // 점수 및 시간
    private int leftTeamScore = 0;
    private int rightTeamScore = 0;
    private float gameTimeSeconds = 0f;
    private float timeScale;

    // 게임 상태
    public bool IsGamePlaying { get; private set; } = true;
    private bool isGoalScored = false;
    private string winnerMessage = "";
    private bool isPaused = false;

    // 초기 위치
    private Vector3 ballInitialPos;
    private Vector3 playerInitialPos;
    private Vector3 aiInitialPos;

    // 메뉴 컨트롤 변수
    private int currentMenuSelection = 0;
    private int visibleButtonCount = 0;
    private float lastInputTime = 0f;
    private readonly float inputCooldown = 0.2f;
    
    // 새로운 Input System을 위한 변수
    private PlayerInput playerControls; // 파일 이름이 PlayerInput.inputactions 이므로 클래스 이름은 PlayerInput

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }

        // Input System 클래스 인스턴스 생성
        playerControls = new PlayerInput();
    }

    private void OnEnable()
    {
        // ✨ 'Pause'가 아닌 'Cancel' 액션에 HandlePause 함수를 구독합니다. ✨
        playerControls.Player.Pause.performed += HandlePause;
        
        // Submit 액션에 버튼 실행 함수를 구독합니다.
        playerControls.UI.Submit.performed += _ => ActivateSelectedButton();
    }

    private void OnDisable()
    {
        // ✨ 구독 해제도 'Cancel' 액션으로 변경합니다. ✨
        playerControls.Player.Pause.performed -= HandlePause;
        
        playerControls.UI.Submit.performed -= _ => ActivateSelectedButton();
        playerControls.Disable();
    }

    void Start()
    {
        ResumeGame(); 
        timeScale = (gameDurationMinutes * 60) / (realTimeMinutesToCompleteGame * 60);
        ballInitialPos = ball.transform.position;
        playerInitialPos = player.position;
        aiInitialPos = ai.position;
    }

    void Update()
    {
        if (!IsGamePlaying || isPaused)
        {
            HandleMenuNavigation();
        }
        else
        {
            gameTimeSeconds += Time.deltaTime * timeScale;
            if (gameTimeSeconds >= gameDurationMinutes * 60)
            {
                EndGame();
            }
        }
    }

    private void HandlePause(InputAction.CallbackContext context)
    {
        if (IsGamePlaying)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    private void HandleMenuNavigation()
    {
        if (Time.unscaledTime < lastInputTime + inputCooldown) return;
        Vector2 navInput = playerControls.UI.Navigate.ReadValue<Vector2>();

        if (navInput.y > 0.5f)
        {
            currentMenuSelection--;
            lastInputTime = Time.unscaledTime;
        }
        else if (navInput.y < -0.5f)
        {
            currentMenuSelection++;
            lastInputTime = Time.unscaledTime;
        }

        if (currentMenuSelection < 0) currentMenuSelection = visibleButtonCount - 1;
        if (currentMenuSelection >= visibleButtonCount) currentMenuSelection = 0;
    }

    private void ActivateSelectedButton()
    {
        if (!isPaused && IsGamePlaying) return;

        if (isPaused)
        {
            switch (currentMenuSelection)
            {
                case 0: ResumeGame(); break;
                case 1: RestartGame(); break;
                case 2: GoToMainMenu(); break;
            }
        }
        else if (!IsGamePlaying)
        {
            switch (currentMenuSelection)
            {
                case 0: RestartGame(); break;
                case 1: GoToNextScene(); break;
                case 2: GoToMainMenu(); break;
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        currentMenuSelection = 0;
        
        playerControls.Player.Disable();
        playerControls.UI.Enable();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        playerControls.UI.Disable();
        playerControls.Player.Enable();
    }
    
    private void EndGame()
    {
        IsGamePlaying = false;
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        currentMenuSelection = 0;
        
        playerControls.Player.Disable();
        playerControls.UI.Enable();

        if (leftTeamScore > rightTeamScore) { winnerMessage = "Player Wins!"; }
        else if (rightTeamScore > leftTeamScore) { winnerMessage = "A.I. Wins!"; }
        else { winnerMessage = "Draw!"; }
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
        yield return new WaitForSecondsRealtime(resetDelayAfterGoal);
        ball.transform.position = ballInitialPos;
        var ballRb = ball.GetComponent<Rigidbody>();
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        var playerRb = player.GetComponent<Rigidbody>();
        playerRb.linearVelocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;

        var aiRb = ai.GetComponent<Rigidbody>();
        aiRb.linearVelocity = Vector3.zero;
        aiRb.angularVelocity = Vector3.zero;

        isGoalScored = false;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("다음 씬 이름이 지정되지 않았습니다!");
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("메인 메뉴 씬 이름이 지정되지 않았습니다!");
        }
    }
    
    void OnGUI()
    {
        // OnGUI 부분은 수정할 필요 없습니다. (기존과 동일)
        GUIStyle style = new GUIStyle();
        style.fontSize = 30;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = Color.black;
        Rect shadowOffset = new Rect(0, 0, 0, 0);

        string scoreText = $"{leftTeamScore} : {rightTeamScore}";
        Rect scoreRect = new Rect(Screen.width / 2 - 100, 10, 200, 50);
        shadowOffset.x = scoreRect.x + 2; shadowOffset.y = scoreRect.y + 2;
        shadowOffset.width = scoreRect.width; shadowOffset.height = scoreRect.height;
        GUI.Label(shadowOffset, scoreText, shadowStyle);
        style.normal.textColor = Color.white;
        GUI.Label(scoreRect, scoreText, style);

        int minutes = (int)(gameTimeSeconds / 60);
        int seconds = (int)(gameTimeSeconds % 60);
        string timerText = $"Time: {minutes:00}:{seconds:00}";
        Rect timerRect = new Rect(Screen.width / 2 - 100, 50, 200, 50);
        shadowOffset.x = timerRect.x + 2; shadowOffset.y = timerRect.y + 2;
        shadowOffset.width = timerRect.width; shadowOffset.height = timerRect.height;
        GUI.Label(shadowOffset, timerText, shadowStyle);
        style.normal.textColor = Color.white;
        GUI.Label(timerRect, timerText, style);

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

        if (!IsGamePlaying || isPaused)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 20;
            buttonStyle.fontStyle = FontStyle.Bold;
            Color originalColor = GUI.color;

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
            else
            {
                Rect pausedRect = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 150, 300, 50);
                string pausedText = "PAUSED";
                shadowOffset.x = pausedRect.x + 2; shadowOffset.y = pausedRect.y + 2;
                shadowOffset.width = pausedRect.width; shadowOffset.height = pausedRect.height;
                GUI.Label(shadowOffset, pausedText, shadowStyle);
                style.normal.textColor = Color.cyan;
                GUI.Label(pausedRect, pausedText, style);
            }

            float buttonYOffset = !IsGamePlaying ? Screen.height / 2 + 20 : Screen.height / 2 - 50;
            int buttonIndex = 0;

            if (isPaused)
            {
                GUI.color = (buttonIndex == currentMenuSelection) ? Color.yellow : originalColor;
                if (GUI.Button(new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50), "재개하기", buttonStyle))
                {
                    ResumeGame();
                }
                buttonYOffset += 60;
                buttonIndex++;
            }

            GUI.color = (buttonIndex == currentMenuSelection) ? Color.yellow : originalColor;
            if (GUI.Button(new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50), "다시하기", buttonStyle))
            {
                RestartGame();
            }
            buttonYOffset += 60;
            buttonIndex++;

            if (!IsGamePlaying)
            {
                GUI.color = (buttonIndex == currentMenuSelection) ? Color.yellow : originalColor;
                if (GUI.Button(new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50), "다음 씬으로", buttonStyle))
                {
                    GoToNextScene();
                }
                buttonYOffset += 60;
                buttonIndex++;
            }

            GUI.color = (buttonIndex == currentMenuSelection) ? Color.yellow : originalColor;
            if (GUI.Button(new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50), "메인 메뉴", buttonStyle))
            {
                GoToMainMenu();
            }

            visibleButtonCount = buttonIndex + 1;
            GUI.color = originalColor;
        }
    }
}