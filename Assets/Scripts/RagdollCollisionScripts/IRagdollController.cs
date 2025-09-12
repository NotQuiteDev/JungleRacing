using UnityEngine;

/// <summary>
/// 래그돌 파츠의 충돌 정보를 처리할 수 있는 모든 컨트롤러가 가져야 할 기능 명세입니다.
/// </summary>
public interface IRagdollController
{
    /// <summary>
    /// 래그돌의 자식 파츠에서 충돌이 발생했을 때 호출될 함수입니다.
    /// </summary>
    /// <param name="collision">자식 파츠에서 발생한 충돌 정보</param>
    void HandleRagdollCollision(Collision collision);
}