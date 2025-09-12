using UnityEngine;

public class Goal : MonoBehaviour
{
    [Tooltip("이 골대에 공을 넣었을 때 점수를 얻는 팀입니다.")]
    public Team teamToAwardPoint;

    private void OnCollisionEnter(Collision collision)
    {
        // 부딪힌 오브젝트의 태그가 "Ball"인지 확인합니다.
        if (collision.gameObject.CompareTag("Ball"))
        {
            // SoccerGameManager에게 골이 들어왔다고 알립니다.
            SoccerGameManager.Instance.GoalScored(teamToAwardPoint);
        }
    }
}