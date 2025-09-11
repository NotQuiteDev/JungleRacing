using UnityEngine;

public class PKHPlayerController : MonoBehaviour
{/*
    public static PKHPlayerController Instance {  get; private set; }
    
    // Component
    private Rigidbody rb;
    [SerializeField] private Transform playerForm; // 플레이어 형태
    

    [SerializeField] private float defaultSpeed = 20f; // 기본 속도
    private float curAmountSpeed = 0f; // 스피드 변화량
    private float maxAmountSpeed = 3f; // 최대 증감량
    private float rotateSpeed = 75f; // 회전 속도
    private float maxRotateAngle = 75f; // 최대 회전각

    //Vector3 moveDir;
    private float inertiaSpeed = 5f;

    private void Awake()
    {
        if(Instance == null) Instance = this;

        rb = GetComponent<Rigidbody>();
        //oveDir = transform.forward;
    }

    private void FixedUpdate()
    {
        float rotationInput = InputManager.Instance.GetRotationDir();
        Debug.Log(rotationInput);

        float yRotation = playerForm.eulerAngles.y; // -180에서 180으로 전환
        if (yRotation > 180f)
            yRotation -= 360f;


        float currentY = yRotation + rotationInput * rotateSpeed * Time.fixedDeltaTime;
        currentY = Mathf.Clamp(currentY, -maxRotateAngle, maxRotateAngle);
        playerForm.localRotation = Quaternion.Euler(0f, currentY, 0f);


        //playerForm.Rotate(Vector3.up * rotationInput * rotateSpeed * Time.fixedDeltaTime);

        // 관성


        float speed = InputManager.Instance.GetMoveSpeed();

        if (speed != 0f)
        {
            curAmountSpeed += speed * Time.fixedDeltaTime;

            if (curAmountSpeed > maxAmountSpeed) curAmountSpeed = maxAmountSpeed;
            else if (curAmountSpeed < -maxAmountSpeed) curAmountSpeed = -maxAmountSpeed;
        }

        Vector3 moveDir = transform.forward * (defaultSpeed + curAmountSpeed) +  playerForm.forward * inertiaSpeed;
        moveDir.y = 0;

        rb.MovePosition(rb.position + moveDir * Time.fixedDeltaTime);
    }*/
}
