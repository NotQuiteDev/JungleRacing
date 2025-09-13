using UnityEngine;

// 오리를 따라가는 카메라 컨트롤러
public class DuckCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // 따라갈 오리 오브젝트

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0, 5, 8); // 카메라 오프셋 (Z를 -8에서 8로 변경)
    public float followSpeed = 5f; // 따라가는 속도
    public float rotationSpeed = 3f; // 회전 속도

    [Header("Options")]
    public bool smoothFollow = true; // 부드러운 움직임
    public bool lookAtTarget = true; // 타겟을 바라보기

    void LateUpdate()
    {
        if (target == null) return;

        // 목표 위치 계산
        Vector3 desiredPosition = target.position + target.TransformDirection(offset);

        // 카메라 위치 이동
        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = desiredPosition;
        }

        // 카메라 회전 (타겟을 바라보기)
        if (lookAtTarget)
        {
            Vector3 lookDirection = target.position - transform.position;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
}