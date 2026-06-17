using UnityEngine;

public enum UIMenuType
{
    None,
    Map,
    Pause,
    Lose,
    Win
}

public class UIMenuChanger : MonoBehaviour
{
    public static UIMenuChanger Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject winPanel;

    public UIMenuType CurrentMenu { get; private set; }
    public bool IsMenuOpen => CurrentMenu != UIMenuType.None;

    private bool IsGameOver =>
        GameplayManager.Instance != null && GameplayManager.Instance.IsGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (losePanel == null)
        {
            GameObject foundPanel = GameObject.Find("Lose Panel");
            if (foundPanel != null)
                losePanel = foundPanel;
        }

        if (winPanel == null)
        {
            GameObject foundPanel = GameObject.Find("Win Panel");
            if (foundPanel != null)
                winPanel = foundPanel;
        }

        SetMenu(UIMenuType.None);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowLose()
    {
        SetMenu(UIMenuType.Lose);
    }

    public void ShowWin()
    {
        SetMenu(UIMenuType.Win);
    }

    public void OnUIMap()
    {
        if (IsGameOver)
            return;

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
        if (IsGameOver)
            return;

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
        if (IsGameOver)
            return;

        if (CurrentMenu == UIMenuType.None)
        {
            return;
        }

        SetMenu(UIMenuType.None);
    }

    private void SetMenu(UIMenuType menuType)
    {
        CurrentMenu = menuType;
        if (GameTimeManager.Instance != null)
        {
            if (IsGameOver)
                GameTimeManager.Instance.SetGameOver();
            else
                GameTimeManager.Instance.SetPauseEnabled(CurrentMenu != UIMenuType.None);
        }
        ApplyVisibility();
        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        Cursor.visible = IsMenuOpen;
        Cursor.lockState = IsMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
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

        if (losePanel != null)
        {
            losePanel.SetActive(CurrentMenu == UIMenuType.Lose);
        }

        if (winPanel != null)
        {
            winPanel.SetActive(CurrentMenu == UIMenuType.Win);
        }
    }
}
