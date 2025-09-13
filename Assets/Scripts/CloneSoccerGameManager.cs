using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
// ✨ 새로운 Input System 사용을 위해 네임스페이스 추가 ✨
using UnityEngine.InputSystem;



public class CloneSoccerGameManager : MonoBehaviour
{
    // --- 싱글톤 및 기존 변수 선언 (대부분 동일) ---
    public static CloneSoccerGameManager Instance { get; private set; }

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

    [Header("프리팹 및 스폰 설정")]
    public GameObject leftTeamPrefab;
    public GameObject rightTeamPrefab;
    public Transform leftTeamSpawnPoint;
    public Transform rightTeamSpawnPoint;
    public int clonesToSpawnPerGoal = 1;
    
    [Header("경기장 설정")]
    public Collider safeZoneCollider;
    public Transform leftTeamRespawnPoint;
    public Transform rightTeamRespawnPoint;

    // --- 내부 변수 (점수, 시간, 상태 등) ---
    private int leftTeamScore = 0;
    private int rightTeamScore = 0;
    private float gameTimeSeconds = 0f;
    private float timeScale;
    public bool IsGamePlaying { get; private set; } = true;
    private bool isGoalScored = false;
    private string winnerMessage = "";
    private bool isPaused = false;
    private Vector3 ballInitialPos, playerInitialPos, aiInitialPos;

    // ✨ ----- 메뉴 컨트롤 및 Input System 변수 추가 ----- ✨
    private int currentMenuSelection = 0;
    private int visibleButtonCount = 0;
    private float lastInputTime = 0f;
    private readonly float inputCooldown = 0.2f;
    private PlayerInput playerControls;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }

        // ✨ Input System 클래스 인스턴스 생성 ✨
        playerControls = new PlayerInput();
    }

    // ✨ OnEnable/OnDisable: Input System 생명주기 관리 ✨
    private void OnEnable()
    {
        // Player 맵의 Pause 액션에 일시정지 함수를 구독
        playerControls.Player.Pause.performed += HandlePause;
        // UI 맵의 Submit 액션에 메뉴 선택 함수를 구독
        playerControls.UI.Submit.performed += _ => ActivateSelectedButton();
    }

    private void OnDisable()
    {
        // 구독 해제
        playerControls.Player.Pause.performed -= HandlePause;
        playerControls.UI.Submit.performed -= _ => ActivateSelectedButton();
        playerControls.Disable();
    }

    void Start()
    {
        ResumeGame(); // Action Map 설정을 위해 호출
        timeScale = (gameDurationMinutes * 60) / (realTimeMinutesToCompleteGame * 60);
        ballInitialPos = ball.transform.position;
        playerInitialPos = player.position;
        aiInitialPos = ai.position;
    }

    void Update()
    {
        // ✨ 기존의 Escape 키 입력 감지 로직은 삭제하고, 아래 로직으로 대체 ✨
        if (IsGamePlaying && !isPaused)
        {
            gameTimeSeconds += Time.deltaTime * timeScale;
            if (gameTimeSeconds >= gameDurationMinutes * 60)
            {
                EndGame();
            }
        }
        else // 게임이 멈췄을 때(일시정지 또는 종료) 메뉴 네비게이션 처리
        {
            HandleMenuNavigation();
        }
    }

    // ✨ Input System 이벤트에 의해 호출될 일시정지 함수 ✨
    private void HandlePause(InputAction.CallbackContext context)
    {
        if (IsGamePlaying)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        currentMenuSelection = 0; // 메뉴 선택 초기화
        
        // ✨ Action Map 전환: Player(게임플레이) 끄고 UI(메뉴조작) 켜기 ✨
        playerControls.Player.Disable();
        playerControls.UI.Enable();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // ✨ Action Map 전환: UI 끄고 Player 켜기 ✨
        playerControls.UI.Disable();
        playerControls.Player.Enable();
    }
    
    private void EndGame()
    {
        IsGamePlaying = false;
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        currentMenuSelection = 0; // 메뉴 선택 초기화

        // ✨ 게임 종료 시에도 UI 조작을 위해 UI Action Map 켜기 ✨
        playerControls.Player.Disable();
        playerControls.UI.Enable();

        if (leftTeamScore > rightTeamScore) { winnerMessage = "Player Wins!"; }
        else if (rightTeamScore > leftTeamScore) { winnerMessage = "A.I. Wins!"; }
        else { winnerMessage = "Draw!"; }
    }

    // ✨ --- 아래의 게임 핵심 로직(GoalScored, SpawnPrefab 등)은 그대로 유지됩니다 --- ✨
    public void GoalScored(Team scoringTeam)
    {
        if (isGoalScored) return;
        isGoalScored = true;

        if (scoringTeam == Team.Left)
        {
            leftTeamScore++;
            SpawnPrefab(Team.Right);
        }
        else if (scoringTeam == Team.Right)
        {
            rightTeamScore++;
            SpawnPrefab(Team.Left);
        }
        StartCoroutine(ResetAfterGoal());
    }
    
    private void SpawnPrefab(Team teamToSpawn)
    {
        GameObject prefabToSpawn = (teamToSpawn == Team.Left) ? leftTeamPrefab : rightTeamPrefab;
        Transform spawnPoint = (teamToSpawn == Team.Left) ? leftTeamSpawnPoint : rightTeamSpawnPoint;

        if (prefabToSpawn != null && spawnPoint != null)
        {
            for (int i = 0; i < clonesToSpawnPerGoal; i++)
            {
                Vector3 spawnPosition = spawnPoint.position + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f));
                GameObject newAIObject = Instantiate(prefabToSpawn, spawnPosition, spawnPoint.rotation);
                SoccerPlayerAI aiScript = newAIObject.GetComponent<SoccerPlayerAI>();

                if (aiScript != null)
                {
                    aiScript.ball = this.ball.transform;
                    aiScript.safeZoneCollider = this.safeZoneCollider;
                    if (teamToSpawn == Team.Left)
                    {
                        aiScript.myGoal = leftGoalPost;
                        aiScript.opponentGoal = rightGoalPost;
                        aiScript.opponent = this.ai;
                        aiScript.respawnPoint = leftTeamRespawnPoint;
                    }
                    else
                    {
                        aiScript.myGoal = rightGoalPost;
                        aiScript.opponentGoal = leftGoalPost;
                        aiScript.opponent = this.player;
                        aiScript.respawnPoint = rightTeamRespawnPoint;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning(teamToSpawn.ToString() + " 팀의 프리팹 또는 스폰 포인트가 설정되지 않았습니다.");
        }
    }

    private IEnumerator ResetAfterGoal()
    {
        yield return new WaitForSecondsRealtime(resetDelayAfterGoal);

        ball.transform.position = ballInitialPos;
        var ballRb = ball.GetComponent<Rigidbody>();
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        player.position = playerInitialPos;
        var playerRb = player.GetComponent<Rigidbody>();
        playerRb.linearVelocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;

        ai.position = aiInitialPos;
        var aiRb = ai.GetComponent<Rigidbody>();
        aiRb.linearVelocity = Vector3.zero;
        aiRb.angularVelocity = Vector3.zero;

        isGoalScored = false;
    }

    // --- 씬 이동 함수들 (RestartGame, GoToNextScene, GoToMainMenu)은 그대로 유지 ---
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
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("메인 메뉴 씬 이름이 지정되지 않았습니다!");
        }
    }
    
    // ✨ ----- 패드 지원을 위한 메뉴 함수들 추가 ----- ✨
    private void HandleMenuNavigation()
    {
        if (Time.unscaledTime < lastInputTime + inputCooldown) return;
        Vector2 navInput = playerControls.UI.Navigate.ReadValue<Vector2>();

        if (navInput.y > 0.5f) {
            currentMenuSelection--;
            lastInputTime = Time.unscaledTime;
        }
        else if (navInput.y < -0.5f) {
            currentMenuSelection++;
            lastInputTime = Time.unscaledTime;
        }

        if (currentMenuSelection < 0) currentMenuSelection = visibleButtonCount - 1;
        if (currentMenuSelection >= visibleButtonCount) currentMenuSelection = 0;
    }

    private void ActivateSelectedButton()
    {
        if (!isPaused && IsGamePlaying) return;
        if (isPaused) {
            switch (currentMenuSelection) {
                case 0: ResumeGame(); break;
                case 1: RestartGame(); break;
                case 2: GoToMainMenu(); break;
            }
        }
        else if (!IsGamePlaying) {
            switch (currentMenuSelection) {
                case 0: RestartGame(); break;
                case 1: GoToNextScene(); break;
                case 2: GoToMainMenu(); break;
            }
        }
    }

    // ✨ OnGUI를 패드 지원하도록 수정 ✨
    void OnGUI()
    {
        // 상단 UI (점수, 시간, 조작법)
        GUIStyle style = new GUIStyle { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        GUIStyle shadowStyle = new GUIStyle(style) { normal = { textColor = Color.black } };
        Rect shadowOffset = new Rect(0, 0, 0, 0);
        string scoreText = $"{leftTeamScore} : {rightTeamScore}";
        Rect scoreRect = new Rect(Screen.width / 2 - 100, 10, 200, 50);
        shadowOffset.x = scoreRect.x + 2; shadowOffset.y = scoreRect.y + 2; shadowOffset.width = scoreRect.width; shadowOffset.height = scoreRect.height;
        GUI.Label(shadowOffset, scoreText, shadowStyle);
        style.normal.textColor = Color.white;
        GUI.Label(scoreRect, scoreText, style);
        int minutes = (int)(gameTimeSeconds / 60);
        int seconds = (int)(gameTimeSeconds % 60);
        string timerText = $"Time: {minutes:00}:{seconds:00}";
        Rect timerRect = new Rect(Screen.width / 2 - 100, 50, 200, 50);
        shadowOffset.x = timerRect.x + 2; shadowOffset.y = timerRect.y + 2; shadowOffset.width = timerRect.width; shadowOffset.height = timerRect.height;
        GUI.Label(shadowOffset, timerText, shadowStyle);
        style.normal.textColor = Color.white;
        GUI.Label(timerRect, timerText, style);
        GUIStyle controlStyle = new GUIStyle { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft, normal = { textColor = Color.white } };
        GUIStyle controlShadowStyle = new GUIStyle(controlStyle) { normal = { textColor = Color.black } };
        string controlsText = "Move: WASD / Left Stick\nCamera: Mouse / Right Stick\nKick: Spacebar / A Button";
        Rect controlsRect = new Rect(15, 15, 400, 100);
        Rect controlShadowRect = new Rect(controlsRect.x + 2, controlsRect.y + 2, controlsRect.width, controlsRect.height);
        GUI.Label(controlShadowRect, controlsText, controlShadowStyle);
        GUI.Label(controlsRect, controlsText, controlStyle);

        // 메뉴 UI (일시정지 또는 게임 종료 시)
        if (!IsGamePlaying || isPaused)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
            Color originalColor = GUI.color; // 기존 색상 저장
            int buttonIndex = 0; // 버튼 인덱스 추적

            // PAUSED 또는 Game Over 메시지
            if (!IsGamePlaying) {
                Rect resultRect = new Rect(Screen.width / 2 - 200, Screen.height / 2 - 100, 400, 100);
                string resultText = $"Game Over\n{winnerMessage}";
                shadowOffset.x = resultRect.x + 2; shadowOffset.y = resultRect.y + 2; shadowOffset.width = resultRect.width; shadowOffset.height = resultRect.height;
                GUI.Label(shadowOffset, resultText, shadowStyle);
                style.normal.textColor = Color.yellow;
                GUI.Label(resultRect, resultText, style);
            }
            else {
                Rect pausedRect = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 150, 300, 50);
                string pausedText = "PAUSED";
                shadowOffset.x = pausedRect.x + 2; shadowOffset.y = pausedRect.y + 2; shadowOffset.width = pausedRect.width; shadowOffset.height = pausedRect.height;
                GUI.Label(shadowOffset, pausedText, shadowStyle);
                style.normal.textColor = Color.cyan;
                GUI.Label(pausedRect, pausedText, style);
            }

            float buttonYOffset = !IsGamePlaying ? Screen.height / 2 + 20 : Screen.height / 2 - 50;
            
            // 버튼들 그리기
            if (isPaused) {
                GUI.color = (buttonIndex == currentMenuSelection) ? Color.yellow : originalColor;
                if (GUI.Button(new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50), "재개하기", buttonStyle)) { ResumeGame(); }
                buttonYOffset += 60;
                buttonIndex++;
            }

            GUI.color = (buttonIndex == currentMenuSelection) ? Color.yellow : originalColor;
            if (GUI.Button(new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50), "다시하기", buttonStyle)) { RestartGame(); }
            buttonYOffset += 60;
            buttonIndex++;

            if (!IsGamePlaying) {
                GUI.color = (buttonIndex == currentMenuSelection) ? Color.yellow : originalColor;
                if (GUI.Button(new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50), "다음 씬으로", buttonStyle)) { GoToNextScene(); }
                buttonYOffset += 60;
                buttonIndex++;
            }

            GUI.color = (buttonIndex == currentMenuSelection) ? Color.yellow : originalColor;
            if (GUI.Button(new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50), "메인 메뉴", buttonStyle)) { GoToMainMenu(); }
            
            // 보이는 버튼 개수 업데이트 및 색상 복원
            visibleButtonCount = buttonIndex + 1;
            GUI.color = originalColor;
        }
    }
}