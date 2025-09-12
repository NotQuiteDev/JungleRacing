using UnityEngine;

/// <summary>
/// 이 스크립트가 붙은 오브젝트가 "SafeZone" 태그를 가진 트리거를 벗어나면
/// 지정된 위치로 위치만 강제로 리셋시키는 매우 단순한 컴포넌트입니다.
/// </summary>
public class OutOfBoundsResetter : MonoBehaviour
{
    [Header("Reset Settings")]
    [Tooltip("맵 밖으로 이탈했을 때 돌아올 리스폰 위치입니다.")]
    public Transform respawnPoint;

    [Tooltip("리스폰 위치로 이동한 후, 물리적 속도를 강제로 0으로 만들지 여부입니다.")]
    public bool resetVelocityOnRespawn = true;

    // 이 오브젝트의 Rigidbody (없을 수도 있음)
    private Rigidbody rb;

    void Awake()
    {
        // 성능을 위해 Rigidbody를 미리 찾아둡니다.
        rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerExit(Collider other)
    {
        // 내가 빠져나간 트리거가 "SafeZone" 태그를 가지고 있는지 확인합니다.
        if (other.CompareTag("SafeZone"))
        {
            // 리스폰 위치가 지정되어 있는지 확인합니다.
            if (respawnPoint == null)
            {
                Debug.LogError("리스폰 위치(Respawn Point)가 지정되지 않았습니다! " + gameObject.name + "의 인스펙터 창에서 설정해주세요.");
                return;
            }

            Debug.LogWarning(gameObject.name + "이(가) 맵 밖으로 이탈하여 지정된 위치로 복귀합니다.");

            // 그냥 위치만 리스폰 지점으로 옮깁니다. 이게 전부입니다.
            transform.position = respawnPoint.position;
            
            // 만약 물리 속도 리셋 옵션이 켜져있고, Rigidbody가 있다면 속도를 0으로 만듭니다.
            // (순간이동 후에도 계속 날아가는 현상 방지)
            if (resetVelocityOnRespawn && rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}