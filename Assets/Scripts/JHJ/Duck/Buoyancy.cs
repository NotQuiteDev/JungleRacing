using UnityEngine;

public class Buoyancy : MonoBehaviour
{
    public Rigidbody rb;
    public float waterLevel = 0.0f;     // 물 표면의 Y축 높이
    public float buoyancyForce = 15f;   // 오리를 위로 밀어 올리는 힘

    // 물리 효과는 FixedUpdate에서 처리하는 것이 더 안정적입니다.
    void FixedUpdate()
    {
        // 오리의 현재 위치가 물 높이보다 낮으면
        if (transform.position.y < waterLevel)
        {
            // 중력을 이기는 힘을 위로 가합니다.
            rb.AddForce(Vector3.up * buoyancyForce, ForceMode.Acceleration);
        }
    }
}