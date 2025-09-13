using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// SoccerGameManager에 이미 'Team' enum이 있으므로 여기서는 정의하지 않습니다.
// public enum Team { Left, Right } // 이 줄을 삭제하여 중복 정의 에러를 해결합니다.

public class CloneSoccerGameManager : MonoBehaviour
{
    // 다른 스크립트에서 쉽게 접근할 수 있도록 싱글톤 인스턴스를 만듭니다.
    public static CloneSoccerGameManager Instance { get; private set; }

    [Header("게임 설정")]
    [Tooltip("가상 게임 시간(분)입니다. 실제 시간과 다릅니다.")]
    public float gameDurationMinutes = 90f;
    [Tooltip("위의 가상 게임 시간이 흘러가는 데 걸리는 실제 시간(분)입니다.")]
    public float realTimeMinutesToCompleteGame = 1.5f; // 1.5분 = 90초
    
    [Tooltip("게임 종료 후 이동할 씬의 이름을 입력하세요.")]
    public string nextSceneName;
    [Tooltip("메인 메뉴로 이동할 씬의 이름을 입력하세요.")]
    public string mainMenuSceneName;

    [Header("게임 오브젝트 연결")]
    public GameObject ball;
    public Transform player;
    public Transform ai;
    public Transform leftGoalPost;
    public Transform rightGoalPost;

    [Header("프리팹 및 스폰 설정")]
    [Tooltip("왼쪽 팀(Player)이 골을 먹었을 때 소환될 프리팹입니다.")]
    public GameObject leftTeamPrefab;
    [Tooltip("오른쪽 팀(AI)이 골을 먹었을 때 소환될 프리팹입니다.")]
    public GameObject rightTeamPrefab;
    [Tooltip("왼쪽 팀 프리팹이 소환될 위치입니다.")]
    public Transform leftTeamSpawnPoint;
    [Tooltip("오른쪽 팀 프리팹이 소환될 위치입니다.")]
    public Transform rightTeamSpawnPoint;
    
    // ✨ --- 클론 생성 개수 설정 변수 추가 --- ✨
    [Tooltip("골이 들어갈 때마다 한 번에 소환할 클론의 수입니다.")]
    public int clonesToSpawnPerGoal = 1;
    // ✨ --- 여기까지 추가 --- ✨


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
        ResumeGame();

        timeScale = (gameDurationMinutes * 60) / (realTimeMinutesToCompleteGame * 60);

        ballInitialPos = ball.transform.position;
        playerInitialPos = player.position;
        aiInitialPos = ai.position;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
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
        }

        if (IsGamePlaying && !isPaused)
        {
            gameTimeSeconds += Time.deltaTime * timeScale;

            if (gameTimeSeconds >= gameDurationMinutes * 60)
            {
                EndGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

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

    // ✨ --- 클론 생성 개수를 반영하도록 함수 수정 --- ✨
    private void SpawnPrefab(Team teamToSpawn)
    {
        GameObject prefabToSpawn = null;
        Transform spawnPoint = null;

        if (teamToSpawn == Team.Left)
        {
            prefabToSpawn = leftTeamPrefab;
            spawnPoint = leftTeamSpawnPoint;
        }
        else
        {
            prefabToSpawn = rightTeamPrefab;
            spawnPoint = rightTeamSpawnPoint;
        }

        if (prefabToSpawn != null && spawnPoint != null)
        {
            // 설정된 클론 수만큼 반복해서 생성
            for (int i = 0; i < clonesToSpawnPerGoal; i++)
            {
                // 클론들이 겹치지 않도록 스폰 위치에 약간의 랜덤 값을 더함
                Vector3 spawnPosition = spawnPoint.position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                Instantiate(prefabToSpawn, spawnPosition, spawnPoint.rotation);
            }
        }
        else
        {
            Debug.LogWarning(teamToSpawn.ToString() + " 팀의 프리팹 또는 스폰 포인트가 설정되지 않았습니다.");
        }
    }
    // ✨ --- 여기까지 수정 --- ✨

    private IEnumerator ResetAfterGoal()
    {
        yield return new WaitForSecondsRealtime(resetDelayAfterGoal);

        ball.transform.position = ballInitialPos;
        ball.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        ball.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        player.position = playerInitialPos;
        player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        player.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        ai.position = aiInitialPos;
        ai.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        ai.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        isGoalScored = false;
    }

    private void EndGame()
    {
        IsGamePlaying = false;
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (leftTeamScore > rightTeamScore) { winnerMessage = "Player Wins!"; }
        else if (rightTeamScore > leftTeamScore) { winnerMessage = "A.I. Wins!"; }
        else { winnerMessage = "Draw!"; }
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
            Debug.LogError("다음 씬 이름이 지정되지 않았습니다! Build Settings에 씬을 추가하고 이름을 확인하세요.");
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
            Debug.LogError("메인 메뉴 씬 이름이 지정되지 않았습니다! Build Settings에 씬을 추가하고 이름을 확인하세요.");
        }
    }

    // OnGUI 함수는 변경 사항 없음
    void OnGUI()
    {
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

            if (isPaused)
            {
                Rect resumeButtonRect = new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50);
                if (GUI.Button(resumeButtonRect, "재개하기", buttonStyle))
                {
                    ResumeGame();
                }
                buttonYOffset += 60;
            }

            Rect restartButtonRect = new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50);
            if (GUI.Button(restartButtonRect, "다시하기", buttonStyle))
            {
                RestartGame();
            }
            buttonYOffset += 60;

            if (!IsGamePlaying)
            {
                Rect nextSceneButtonRect = new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50);
                if (GUI.Button(nextSceneButtonRect, "다음 씬으로", buttonStyle))
                {
                    GoToNextScene();
                }
                buttonYOffset += 60;
            }

            Rect mainMenuButtonRect = new Rect(Screen.width / 2 - 100, buttonYOffset, 200, 50);
            if (GUI.Button(mainMenuButtonRect, "메인 메뉴", buttonStyle))
            {
                GoToMainMenu();
            }
        }
    }
}