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
    }

    public void Shoot()
    {
        Debug.Log("슛 체크");

        float x = Random.Range(-xRange, xRange + 1);
        float y = Random.Range(0, yRange + 1);

        Vector3 vec = new Vector3(x, y, zOffset);
        vec = vec - transform.position;
        //vec.z = zOffset;
        vec.y *= 2; // y축 보정
        Debug.Log(vec);

        rb.AddForce(vec.normalized * power, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            Vector3 dir = transform.position - collision.gameObject.transform.position;
            dir.Normalize();

            rb.AddForce(dir * power/3, ForceMode.Impulse);
        }

        if(collision.gameObject.CompareTag("Enemy"))
        {
            if (!isFirst)
            {
                isFirst = true;
                Shoot();
                return;
            }

            Vector3 dir = transform.position - collision.gameObject.transform.position;
            dir.Normalize();

            rb.AddForce(dir * power / 3, ForceMode.Impulse);
        }

        if(collision.gameObject.CompareTag("Line"))
        {
            //Destroy(gameObject);
            gameObject.SetActive(false);
        }
    }
}
