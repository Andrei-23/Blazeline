using System;
using UnityEngine;

public class GameTimeManager : MonoBehaviour
{
    public static GameTimeManager Instance { get; private set; }

    [Header("Game Time Settings")]
    [SerializeField] private int gameDurationMinutes = 10;
    [SerializeField] private float menuWeakPauseTimerDurationSeconds = 10f;
    [SerializeField] private float menuWeakPauseTimerChargeMult = 0.5f;

    [SerializeField] private float debugSpeedUpMult = 1f;

    // [SerializeField] private bool gameTimerRunsOnUnscaledTime = true;

    [Header("Time Scale Settings")]
    [SerializeField] private float defaultAimingTimeScale = 0.1f;
    [SerializeField] private float hardAimingTimeScale = 0f;
    [SerializeField] private float defaultPauseTimeScale = 0f;
    [SerializeField] private float weakPauseTimeScale = 0.05f;

    public enum AimMode {
        None,
        Default,
        Hard,
    }
    public enum PauseMode {
        None,
        Default,
        Weak,
    }
    private AimMode aimMode = AimMode.None;
    private PauseMode pauseMode = PauseMode.None;

    private float defaultFixedDeltaTime;
    private float gameTimeLeftSeconds;
    private int lastBroadcastedMinutesLeft = -1;
    private float menuWeakPauseTimerCurrent;

    public static event Action<int, int> OnTimerMinutesChanged;
    public static event Action<float, float> OnGameTimerUpdated;
    public static event Action<float, float> OnMenuWeakPauseTimerUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        menuWeakPauseTimerCurrent = Mathf.Max(0f, menuWeakPauseTimerDurationSeconds);
        
        gameTimeLeftSeconds = Mathf.Max(0f, gameDurationMinutes * 60f);
        BroadcastGameMinuteIfChanged(forceBroadcast: true);
        BroadcastTimerUpdates();
    }

    private void Start()
    {
        
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SetAimingMode(AimMode.None);
            SetPauseMode(PauseMode.None);
            Instance = null;
        }
    }

    private void UpdateTimeScale()
    {
        if (pauseMode == PauseMode.Default) Time.timeScale = Mathf.Max(0f, defaultPauseTimeScale);
        else if (pauseMode == PauseMode.Weak) Time.timeScale = Mathf.Max(0f, weakPauseTimeScale);
        else if (aimMode == AimMode.Hard) Time.timeScale = Mathf.Max(0f, hardAimingTimeScale);
        else if (aimMode == AimMode.Default) Time.timeScale = Mathf.Max(0f, defaultAimingTimeScale);
        else Time.timeScale = 1f;

        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
    }

    private void Update()
    {
        float unscaledDelta = Time.unscaledDeltaTime;
        UpdateMenuWeakPauseTimer(unscaledDelta);

        float delta;
        if (pauseMode == PauseMode.Default){
            delta = 0f;
        }
        else
        {
            delta = unscaledDelta * debugSpeedUpMult;
        }
        
        if (delta <= 0f)
            return;
        UpdateGameTimer(delta);
    }

    private bool CanSetDefaultPauseMode()
    {
        return menuWeakPauseTimerDurationSeconds <= 0f;
    }

    private void UpdateGameTimer(float delta)
    {
        if (gameTimeLeftSeconds <= 0f)
            return;

        gameTimeLeftSeconds = Mathf.Max(0f, gameTimeLeftSeconds - delta);
        BroadcastGameMinuteIfChanged(forceBroadcast: false);
        OnGameTimerUpdated?.Invoke(gameTimeLeftSeconds, gameDurationMinutes * 60f);
    }

    private void UpdateMenuWeakPauseTimer(float delta)
    {
        if (menuWeakPauseTimerDurationSeconds <= 0f)
            return;

        float previous = menuWeakPauseTimerCurrent;
        if (pauseMode == PauseMode.None)
        {
            float recharge = delta * menuWeakPauseTimerChargeMult;
            menuWeakPauseTimerCurrent = Mathf.Min(menuWeakPauseTimerDurationSeconds, menuWeakPauseTimerCurrent + recharge);
        }
        else
        {
            menuWeakPauseTimerCurrent = Mathf.Max(0f, menuWeakPauseTimerCurrent - delta);

            if (pauseMode == PauseMode.Default && menuWeakPauseTimerCurrent <= 0f)
            {
                SetPauseMode(PauseMode.Weak);
            }
        }

        if (!Mathf.Approximately(previous, menuWeakPauseTimerCurrent))
        {
            OnMenuWeakPauseTimerUpdated?.Invoke(menuWeakPauseTimerCurrent, menuWeakPauseTimerDurationSeconds);
        }
    }

    private void BroadcastGameMinuteIfChanged(bool forceBroadcast)
    {
        int minutesLeft = Mathf.CeilToInt(gameTimeLeftSeconds / 60f);
        int minutesPassed = gameDurationMinutes - minutesLeft;
        if (forceBroadcast || minutesLeft != lastBroadcastedMinutesLeft)
        {
            lastBroadcastedMinutesLeft = minutesLeft;
            OnTimerMinutesChanged?.Invoke(minutesPassed, minutesLeft);
        }
    }

    private void BroadcastTimerUpdates()
    {
        OnGameTimerUpdated?.Invoke(gameTimeLeftSeconds, gameDurationMinutes * 60f);
        OnMenuWeakPauseTimerUpdated?.Invoke(menuWeakPauseTimerCurrent, menuWeakPauseTimerDurationSeconds);
    }

    public void SetAimingMode(AimMode mode)
    {
        if (mode != aimMode)
        {
            aimMode = mode;
            UpdateTimeScale();
        }
    }
    public void SetPauseMode(PauseMode mode)
    {
        if (mode != pauseMode)
        {
            if(pauseMode == PauseMode.Default && !CanSetDefaultPauseMode())
            {
                pauseMode = PauseMode.Weak;
            }
            pauseMode = mode;
            UpdateTimeScale();
        }
    }
    public void SetPauseEnabled(bool enabled)
    {
        if (enabled)
        {
            SetPauseMode(PauseMode.Default);
        }
        else
        {
            SetPauseMode(PauseMode.None);
        }
    }

    public float GetTimerSecondsLeft()
    {
        return gameTimeLeftSeconds;
    }
}
