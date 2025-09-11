using UnityEngine;

public class PenaltyManager : MonoBehaviour
{
    public static PenaltyManager Instance { get; private set; }

    private void Awake()
    {
        if(Instance == null) Instance = this;
    }
}
