using UnityEngine;

public enum UIMenuType
{
    None,
    Map,
    Pause
}

public class UIMenuChanger : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject pausePanel;

    public UIMenuType CurrentMenu { get; private set; }

    private void Awake()
    {
        SetMenu(UIMenuType.None);
    }

    public void OnUIMap()
    {
        if (CurrentMenu == UIMenuType.Map)
        {
            SetMenu(UIMenuType.None);
        }
        else
        {
            SetMenu(UIMenuType.Map);
        }
    }

    public void OnUIPause()
    {
        if (CurrentMenu == UIMenuType.Pause)
        {
            SetMenu(UIMenuType.None);
        }
        else if (CurrentMenu != UIMenuType.None)
        {
            SetMenu(UIMenuType.None);
        }
        else
        {
            SetMenu(UIMenuType.Pause);
        }
    }

    public void OnUIBack()
    {
        if (CurrentMenu == UIMenuType.None)
        {
            return;
        }

        SetMenu(UIMenuType.None);
    }

    private void SetMenu(UIMenuType menuType)
    {
        CurrentMenu = menuType;
        ApplyVisibility();
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.SetPauseEnabled(CurrentMenu != UIMenuType.None);
        }
    }

    private void ApplyVisibility()
    {
        if (mapPanel != null)
        {
            mapPanel.SetActive(CurrentMenu == UIMenuType.Map);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(CurrentMenu == UIMenuType.Pause);
        }
    }
}
