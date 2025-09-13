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
            // ✨✨✨ 바로 이 부분을 수정했습니다! ✨✨✨

            // 1. 먼저 SoccerGameManager가 있는지 확인하고, 있으면 그 매니저에게 골을 알립니다.
            if (SoccerGameManager.Instance != null)
            {
                SoccerGameManager.Instance.GoalScored(teamToAwardPoint);
            }
            // 2. 만약 없다면, CloneSoccerGameManager가 있는지 확인하고, 그 매니저에게 골을 알립니다.
            else if (CloneSoccerGameManager.Instance != null)
            {
                CloneSoccerGameManager.Instance.GoalScored(teamToAwardPoint);
            }
            // 3. 둘 다 없으면 에러 메시지를 띄웁니다.
            else
            {
                Debug.LogError("씬에 SoccerGameManager 또는 CloneSoccerGameManager가 없습니다!");
            }
        }
    }
}