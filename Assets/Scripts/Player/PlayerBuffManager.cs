using UnityEngine;

public class PlayerBuffManager : MonoBehaviour
{
    public static PlayerBuffManager Instance { get; private set; }

    [HideInInspector] public int speedBonusCount = 0;

    [HideInInspector] private float dangerLevelPortals = 0f;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    
    public void SetCurrentDangerLevel(float danger)
    {
        dangerLevelPortals = danger;
    }
    public float GetSteeringBoost()
    {
        return 1f + 0.2f * speedBonusCount + 0.5f * dangerLevelPortals;
    }
    
    public float GetSpeedBoost()
    {
        return 1f + 0.5f * speedBonusCount + 0.5f * dangerLevelPortals;
    }
}
