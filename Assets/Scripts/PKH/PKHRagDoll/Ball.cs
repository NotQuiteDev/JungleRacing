using UnityEngine;

public class Ball : MonoBehaviour
{
    private Rigidbody rb;

    [SerializeField] private float power = 5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        Invoke("Shoot", 3f);
    }

    public void Shoot()
    {
        //vector3 dir = pos.position - transform.position;
        Vector3 dir = Joker.Instance.transform.position - transform.position;

        rb.AddForce(dir.normalized * power, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            Vector3 dir = transform.position - collision.gameObject.transform.position;
            dir.Normalize();

            rb.AddForce(dir * power/3, ForceMode.Impulse);
        }
    }
}
