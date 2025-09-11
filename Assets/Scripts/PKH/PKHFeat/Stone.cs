using UnityEngine;
using static UnityEditor.PlayerSettings;

public class Stone : MonoBehaviour
{
    /*[SerializeField] private float speed = 10;
    [SerializeField] private Rigidbody rb;

    private Vector3 target;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        target = PKHPlayerController.Instance.transform.position;
    }

    private void FixedUpdate()
    {
        Vector3 dir = (target - rb.position).normalized;

        rb.MovePosition(rb.position + dir * speed * Time.fixedDeltaTime);
    }

    */
}
