using System.Collections;
using UnityEngine;

public class Joker : MonoBehaviour
{
    // Component
    private Animator anim;
    private Rigidbody rb;
    private Rigidbody[] ragsRigid;
    private Collider col;
    private Collider[] ragColls;

    // Const
    private const string WALKANIM = "isWalk";

    [SerializeField] private float speed = 5f;

    

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        ragsRigid = GetComponentsInChildren<Rigidbody>();
        col = GetComponent<Collider>();
        //ragColls = GetComponentsInChildren<Collider>();

        //foreach (var r in ragsRigid) r.useGravity = false;
        //rb.useGravity = true;

       DisableRagdoll();
    }

    void Start()
    {
        InputManager.Instance.OnAttack += (a, b) => Attack();
    }


    private void FixedUpdate()
    {
        Vector2 moveDir = InputManager.Instance.MoveDirNormalized();
        Vector3 dir = new Vector3(moveDir.x, 0, moveDir.y);

        if (dir == Vector3.zero)
        {
            anim.SetBool(WALKANIM, false);
        }
        else
        {
            anim.SetBool(WALKANIM, true);
            rb.MovePosition(rb.position + dir * speed * Time.fixedDeltaTime);

            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            rb.MoveRotation(targetRot);
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void DisableRagdoll()
    {
        foreach (var r in ragsRigid)
        {
            //r.isKinematic = true;
            //r.freezeRotation = true;
        }

        //rb.useGravity = true;
        //col.enabled = true;
        //foreach (var c in ragColls) c.enabled = false;

        //rb.isKinematic = false;
        //col.enabled = true;
    }

    private void EnableRagdoll()
    {
        foreach (var r in ragsRigid)
        {
            //r.freezeRotation = false;
        }

        //rb.useGravity = false;
       // col.enabled = false;
        //foreach (var c in ragColls) c.enabled = true;

        //rb.isKinematic = true;
        //col.enabled = false;
    }

    private void Attack()
    {
        StartCoroutine(AttackCoroutine());
    }


    private IEnumerator AttackCoroutine()
    {
        EnableRagdoll();
        yield return new WaitForSeconds(1.5f);
        DisableRagdoll();

        Debug.Log("이제 일어나기");

    }
}
