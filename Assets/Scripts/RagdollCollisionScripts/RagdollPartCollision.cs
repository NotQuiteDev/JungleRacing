using UnityEngine;

/// <summary>
/// 래그돌의 각 파츠에 붙어서, 충돌이 발생하면 부모의 메인 컨트롤러에게 충돌 정보를 전달하는 프록시 스크립트입니다.
/// </summary>
public class RagdollPartCollision : MonoBehaviour
{
    // 나의 본체 스크립트 (Player든 AI든 '명찰'만 있으면 누구든 담을 수 있음)
    private IRagdollController mainController;

    void Awake()
    {
        // 내 부모 계층에서 'IRagdollController'라는 명찰을 가진 스크립트를 찾아서 연결합니다.
        mainController = GetComponentInParent<IRagdollController>();
    }

    // 충돌이 일어나면...
    void OnCollisionEnter(Collision collision)
    {
        // 본체가 있는지 확인하고, 있다면 충돌 정보를 그대로 넘겨줍니다.
        if (mainController != null)
        {
            mainController.HandleRagdollCollision(collision);
        }
    }
}