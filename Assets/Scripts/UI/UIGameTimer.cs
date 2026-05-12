using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIGameTimer : MonoBehaviour
{
    [Header("Timer UI")]
    [SerializeField] private bool isWeakTimer;
    [SerializeField] private Slider timerSlider;
    [SerializeField] private TMP_Text timerText;

    private void OnEnable()
    {
        GameTimeManager.OnGameTimerUpdated += HandleGameTimerUpdated;
        GameTimeManager.OnMenuWeakPauseTimerUpdated += HandleMenuWeakPauseTimerUpdated;
    }

    private void OnDisable()
    {
        GameTimeManager.OnGameTimerUpdated -= HandleGameTimerUpdated;
        GameTimeManager.OnMenuWeakPauseTimerUpdated -= HandleMenuWeakPauseTimerUpdated;
    }

    private void HandleGameTimerUpdated(float currentSeconds, float maxSeconds)
    {
        if (!isWeakTimer)
        {
            UpdateTime(currentSeconds, maxSeconds);
        }
    }

    private void HandleMenuWeakPauseTimerUpdated(float currentSeconds, float maxSeconds)
    {
        if (isWeakTimer)
        {
            UpdateTime(currentSeconds, maxSeconds);
        }
    }

    private void UpdateTime(float currentSeconds, float maxSeconds)
    {
        SetSliderValue(currentSeconds, maxSeconds);
        SetTimerText(currentSeconds);
    }

    private void SetSliderValue(float current, float max)
    {
        if (timerSlider == null)
            return;

        float safeMax = Mathf.Max(0.0001f, max);
        timerSlider.value = Mathf.Clamp01(current / safeMax);
    }

    private void SetTimerText(float currentSeconds)
    {
        if (timerText == null)
            return;

        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(currentSeconds));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
