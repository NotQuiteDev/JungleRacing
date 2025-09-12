using System.Collections;
using UnityEngine;

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
    private bool isGamePlaying = true;
    private bool isGoalScored = false; // 골이 들어간 직후 중복 처리를 막기 위한 플래그
    private string winnerMessage = "";

    // 초기 위치 저장용
    private Vector3 ballInitialPos;
    private Vector3 playerInitialPos;
    private Vector3 aiInitialPos;

    [Tooltip("골이 들어간 후 게임이 리셋되기까지 기다리는 시간(초)입니다.")]
    public float resetDelayAfterGoal = 2f;
    // =================================
    
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
        // 실제 시간 대비 가상 시간의 배율을 계산합니다.
        timeScale = (gameDurationMinutes * 60) / (realTimeMinutesToCompleteGame * 60);

        // 게임 시작 시 각 오브젝트의 초기 위치를 저장합니다.
        ballInitialPos = ball.transform.position;
        playerInitialPos = player.position;
        aiInitialPos = ai.position;
    }

    void Update()
    {
        if (isGamePlaying)
        {
            // 시간이 흐르게 합니다. Time.deltaTime에 배율을 곱해 더 빨리 흐르게 만듭니다.
            gameTimeSeconds += Time.deltaTime * timeScale;

            // 정해진 게임 시간이 다 되면 게임을 종료합니다.
            if (gameTimeSeconds >= gameDurationMinutes * 60)
            {
                EndGame();
            }
        }
    }

    // 골 스크립트에서 이 함수를 호출하여 골을 기록합니다.
    public void GoalScored(Team scoringTeam)
    {
        if (isGoalScored) return; // 이미 골 처리 중이면 무시

        isGoalScored = true;

        if (scoringTeam == Team.Left)
        {
            leftTeamScore++;
        }
        else if (scoringTeam == Team.Right)
        {
            rightTeamScore++;
        }

        // 골이 들어간 후 리셋 코루틴을 시작합니다.
        StartCoroutine(ResetAfterGoal());
    }

    private IEnumerator ResetAfterGoal()
    {
        // 2초간 대기
        yield return new WaitForSeconds(resetDelayAfterGoal);

        // 플레이어와 공을 초기 위치로 리셋
        ball.transform.position = ballInitialPos;
        //player.position = playerInitialPos;
        //ai.position = aiInitialPos;
        
        // 물리적 움직임도 모두 초기화
        ball.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        ball.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        player.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        ai.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        ai.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        // 다시 골을 넣을 수 있도록 플래그를 해제
        isGoalScored = false;
    }

    private void EndGame()
    {
        isGamePlaying = false;

        if (leftTeamScore > rightTeamScore)
        {
            winnerMessage = "Left Team Wins!";
        }
        else if (rightTeamScore > leftTeamScore)
        {
            winnerMessage = "Right Team Wins!";
        }
        else
        {
            winnerMessage = "Draw!";
        }
    }

    // 게임 화면에 텍스트를 직접 그리는 GUI 함수
    // ================== [ 수정된 OnGUI 함수 ] ==================
    // 게임 화면에 텍스트를 직접 그리는 GUI 함수
    void OnGUI()
    {
        // 텍스트 스타일 설정 (크기, 정렬 등)
        GUIStyle style = new GUIStyle();
        style.fontSize = 30;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter; // 텍스트를 가운데 정렬

        // --- 그림자 효과를 위한 설정 ---
        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = Color.black; // 그림자 색상
        Rect shadowOffset = new Rect(0, 0, 0, 0);

        // 1. 점수 표시 (화면 상단 중앙)
        string scoreText = $"{leftTeamScore} : {rightTeamScore}";
        Rect scoreRect = new Rect(Screen.width / 2 - 100, 10, 200, 50);
        
        // 그림자 먼저 그리기
        shadowOffset.x = scoreRect.x + 2;
        shadowOffset.y = scoreRect.y + 2;
        shadowOffset.width = scoreRect.width;
        shadowOffset.height = scoreRect.height;
        GUI.Label(shadowOffset, scoreText, shadowStyle);
        // 원본 텍스트 그리기
        style.normal.textColor = Color.white;
        GUI.Label(scoreRect, scoreText, style);


        // 2. 시간 표시 (점수 바로 아래)
        int minutes = (int)(gameTimeSeconds / 60);
        int seconds = (int)(gameTimeSeconds % 60);
        string timerText = $"Time: {minutes:00}:{seconds:00}";
        Rect timerRect = new Rect(Screen.width / 2 - 100, 50, 200, 50);

        // 그림자 먼저 그리기
        shadowOffset.x = timerRect.x + 2;
        shadowOffset.y = timerRect.y + 2;
        shadowOffset.width = timerRect.width;
        shadowOffset.height = timerRect.height;
        GUI.Label(shadowOffset, timerText, shadowStyle);
        // 원본 텍스트 그리기
        style.normal.textColor = Color.white;
        GUI.Label(timerRect, timerText, style);


        // 3. 게임 종료 시 승리 메시지 표시
        if (!isGamePlaying)
        {
            Rect resultRect = new Rect(Screen.width / 2 - 200, Screen.height / 2 - 50, 400, 100);
            string resultText = $"Game Over\n{winnerMessage}";

            // 그림자 먼저 그리기
            shadowOffset.x = resultRect.x + 2;
            shadowOffset.y = resultRect.y + 2;
            shadowOffset.width = resultRect.width;
            shadowOffset.height = resultRect.height;
            GUI.Label(shadowOffset, resultText, shadowStyle);
            // 원본 텍스트 그리기
            style.normal.textColor = Color.yellow;
            GUI.Label(resultRect, resultText, style);
        }
    }
}