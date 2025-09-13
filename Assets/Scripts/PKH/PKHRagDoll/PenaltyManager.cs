using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PenaltyManager : MonoBehaviour
{
    public static PenaltyManager Instance { get; private set; }

    // Component
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject manual;
    [SerializeField] private GameObject gameOver;
    [SerializeField] private GameObject gameClear;
    [SerializeField] private GameObject pauseUI;

    // Event
    public event EventHandler<bool> ChangeKickerEvent;

    // Status
    [field: SerializeField] public Vector3 ballPos { get; private set; }
    [field: SerializeField] public Vector3 kickerPos { get; private set; }
    [field: SerializeField] public Vector3 kickerRotate { get; private set; }
    [field: SerializeField] public Vector3 goalKeeperPos { get; private set; }
    [field: SerializeField] public Vector3 goalKeeperRotate { get; private set; }

    private float curStateTimer = 0;
    private float stageTimer = 10f;

    // State
    public bool isGameEnd { get; private set; }
    public bool isPlayerKick = false; // 플레이어 공격 차례인지 확인
    public bool isGoal = false; // 골을 넣었는지

    public bool isComplete = false;
    public bool isCeremonyTime = false;

    private int aiScore = 0;
    private int playerScore = 0;

    private Coroutine changeCoroutine;

    public float senemonySpeed = 1f; // 기본값

    private bool isManual = false;

    public bool isPause = false;

    private void Awake()
    {
        if(Instance == null) Instance = this;
    }

    private void Start()
    {
        InputManager.Instance.OnPause += InputManager_OnPause;
    }

    private void InputManager_OnPause(object sender, EventArgs e)
    {
        PauseGame();
    }

    private void Update()
    {
        if (isGameEnd) return;
        if (isCeremonyTime) return;

        curStateTimer += Time.deltaTime;
        if(curStateTimer >= stageTimer)
        {
            curStateTimer = stageTimer;
            ChangeKicker();
        }

        TimerUpdate();
    }

    // 키커 체인지
    public void ChangeKicker()
    {
        isCeremonyTime = true;
        senemonySpeed = 2.5f;
        //isComplete = true;

        if (changeCoroutine != null) StopCoroutine(changeCoroutine);
        changeCoroutine = StartCoroutine(Complete());
    }

    private IEnumerator Complete()
    {
        yield return new WaitForSeconds(5f); // 세레머니 타임

        if (isGameEnd) yield break;

        isCeremonyTime = false;
        isComplete = true;
        isPlayerKick = !isPlayerKick;
        curStateTimer = 0;
        isGoal = false;
        senemonySpeed = 1f;
        yield return null;

        ChangeKickerEvent?.Invoke(this, isPlayerKick);

        yield return null;
        isComplete = false;
    }

    private void TimerUpdate()
    {
        timerText.text = "Timer: " + (10 - curStateTimer).ToString("F2") ;
    }

    public void ChangeScore()
    {
        isGoal = true;

        if (isPlayerKick)
        {
            playerScore++;
            if (playerScore >= 5)
            {
                isGameEnd = true;
                gameClear.SetActive(true);
            }
        }
        else
        {
            aiScore++;
            if (aiScore >= 5)
            {
                isGameEnd = true;
                gameOver.SetActive(true);
            }
        }
        scoreText.text = playerScore + " : " + aiScore;
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void ManualChange()
    {
        isManual = !isManual;
        manual.SetActive(isManual);
    }

    public void PauseGame()
    {
        Debug.Log("Pause Game");

        if (isPause) // 이미 켜진 상태
        {
            Resume();
        }
        else // 꺼진 상태
        {
            pauseUI.gameObject.SetActive(true);
            Time.timeScale = 0f;
            isPause = true;
        }
    }

    public void Resume()
    {
        pauseUI.gameObject.SetActive(false);
        Time.timeScale = 1f;
        isPause = false;
    }

    public void Restart()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(2);
        Destroy(InputManager.Instance.gameObject);
    }
}
