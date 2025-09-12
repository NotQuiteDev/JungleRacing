using UnityEngine;

public class Ball : MonoBehaviour
{
    private Rigidbody rb;

    [SerializeField] private float power = 5f;

    private bool isFirst = false;
    private float xRange = 6f;
    private float yRange = 2f;
    private float zOffset = 10f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        //Invoke("Shoot", 3f);
        PenaltyManager.Instance.ChangeKickerEvent += (a, b) => Init();
    }

    public void Init()
    {
        transform.position = PenaltyManager.Instance.ballPos;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        isFirst = false;
    }

    public void Shoot(Vector3 playerPos)
    {
        Debug.Log("슛 체크");

        Vector3 vec;
        float x, y;

        if (PenaltyManager.Instance.isPlayerKick)
        {
            y = Random.Range(0, yRange + 1);
            if(playerPos.x < transform.position.x) // right
            {
                x = Random.Range(0.5f, xRange + 1);
            }
            else x = Random.Range(-xRange, -0.5f);
        }
        else
        {
            x = Random.Range(-xRange, xRange + 1);
            y = Random.Range(0, yRange + 1);
        }

        vec = new Vector3(x, y, zOffset);
        vec = vec - transform.position;
        //vec.z = zOffset;
        vec.y *= 2; // y축 보정
        Debug.Log(vec);

        rb.AddForce(vec.normalized * power, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (PenaltyManager.Instance.isComplete) return;

        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy"))
        {
            if (!isFirst) // 퍼스트 터치라면, 아마 플레이어나 AI가 먼저 닿을일은 없을듯
            {
                isFirst = true;
                Shoot(collision.gameObject.transform.position);
                return;
            }

            Vector3 dir = transform.position - collision.gameObject.transform.position;
            dir.Normalize();

            if (PenaltyManager.Instance.isPlayerKick) rb.AddForce(dir * power/3, ForceMode.Impulse);
            else rb.AddForce(dir * power/5, ForceMode.Impulse);
        }

        if (PenaltyManager.Instance.isCeremonyTime) return;
        
        if(collision.gameObject.CompareTag("Line")) // 골이라면
        {
            PenaltyManager.Instance.ChangeScore();
            ResetBall();
        }
        else if (collision.gameObject.CompareTag("Finish")) // 골이라면
        {
            ResetBall();
        }
    }

    private void ResetBall()
    {
        PenaltyManager.Instance.ChangeKicker();
    }
}
