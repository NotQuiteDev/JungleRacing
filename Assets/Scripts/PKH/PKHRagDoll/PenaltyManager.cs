using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class PenaltyManager : MonoBehaviour
{
    public static PenaltyManager Instance { get; private set; }

    // Component
    [SerializeField] private TextMeshProUGUI timerText;

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
    public bool isPlayerKick = false; // 플레이어 공격 차례인지 확인
    private bool kickComplete = false;

    public bool isComplete = false;


    private void Awake()
    {
        if(Instance == null) Instance = this;
    }

    private void Update()
    {
        curStateTimer += Time.deltaTime;
        if(curStateTimer >= stageTimer)
        {
            kickComplete = true;
            ChangeKicker();
        }
    }

    // 키커 체인지
    public void ChangeKicker()
    {
        isComplete = true;
        isPlayerKick = !isPlayerKick;
        //kickComplete = true;
        curStateTimer = 0;

        ChangeKickerEvent?.Invoke(this, isPlayerKick);
        StartCoroutine(Complete());
    }

    private IEnumerator Complete()
    {
        yield return null;
        isComplete = false;
    }
}
