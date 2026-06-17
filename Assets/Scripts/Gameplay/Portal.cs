using UnityEngine;
using System;

public class Portal : Objective
{   
    [SerializeField] private float timerMinute = 3f;
    [HideInInspector] public float spawnTime;
    public void UpdateSpawnTime()
    {
        spawnTime = GameTimeManager.Instance.GetTimerSecondsLeft();
    }
    protected override void HandleObjectiveClosed()
    {
        Destroy(gameObject);
    }
    public float GetDangerLevel()
    {
        float secondsPased = spawnTime - GameTimeManager.Instance.GetTimerSecondsLeft();
        return Mathf.InverseLerp(0f, 1f, secondsPased / 60f / timerMinute);
    }

    public bool IsTimerExpired()
    {
        if (GameTimeManager.Instance == null)
            return false;

        float secondsPassed = spawnTime - GameTimeManager.Instance.GetTimerSecondsLeft();
        return secondsPassed >= timerMinute * 60f;
    }
}