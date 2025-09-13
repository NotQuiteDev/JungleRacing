using UnityEngine;

/// <summary>
/// 골대 트리거 - 공이 골대 안에 들어왔는지 감지
/// </summary>
public class GoalTrigger : MonoBehaviour
{
    #region Events
    /// <summary>골 성공 이벤트</summary>
    public System.Action OnGoalScored;
    #endregion

    #region Settings
    [Header("== 트리거 설정 ==")]
    [Tooltip("골대 트리거 활성화 여부")]
    public bool isActive = true;

    [Tooltip("트리거 시각화")]
    public bool showTrigger = true;

    [Tooltip("트리거 색상")]
    public Color triggerColor = Color.green;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// 트리거 충돌 감지
    /// </summary>
    /// <param name="other">충돌한 오브젝트</param>
    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        if (other.CompareTag("Ball"))
        {
            Debug.Log("[GoalTrigger] 골!!");
            OnGoalScored?.Invoke();
        }
    }

    /// <summary>
    /// 트리거 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        if (showTrigger)
        {
            Gizmos.color = triggerColor;
            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                if (triggerCollider is BoxCollider box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (triggerCollider is SphereCollider sphere)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                }
            }
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 트리거 활성화/비활성화
    /// </summary>
    /// <param name="active">활성화 여부</param>
    public void SetActive(bool active)
    {
        isActive = active;
        Debug.Log($"[GoalTrigger] 트리거 {(active ? "활성화" : "비활성화")}");
    }
    #endregion
}