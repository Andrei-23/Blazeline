using UnityEngine;

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<GameplayManager>() != null)
            return;

        GameObject managerObject = new GameObject("Gameplay Manager");
        managerObject.AddComponent<GameplayManager>();
    }

    [Header("References")]
    [SerializeField] private UIMenuChanger menuChanger;
    [SerializeField] private PlayerControls playerControls;
    [SerializeField] private UIControls uiControls;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ObjectiveManager objectiveManager;

    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (menuChanger == null)
            menuChanger = FindFirstObjectByType<UIMenuChanger>();
        if (playerControls == null)
            playerControls = FindFirstObjectByType<PlayerControls>();
        if (uiControls == null)
            uiControls = FindFirstObjectByType<UIControls>();
        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (objectiveManager == null)
            objectiveManager = FindFirstObjectByType<ObjectiveManager>();
    }

    private void OnEnable()
    {
        GameTimeManager.OnGameTimerExpired += OnMainTimerExpired;
    }

    private void OnDisable()
    {
        GameTimeManager.OnGameTimerExpired -= OnMainTimerExpired;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (IsGameOver || objectiveManager == null)
            return;

        foreach (Portal portal in objectiveManager.GetActivePortals())
        {
            if (portal != null && portal.IsTimerExpired())
            {
                TriggerLose();
                return;
            }
        }
    }

    private void OnMainTimerExpired()
    {
        TriggerLose();
    }

    public void TriggerLose()
    {
        EndGame(() => menuChanger?.ShowLose());
    }

    public void TriggerWin()
    {
        EndGame(() => menuChanger?.ShowWin());
    }

    private void EndGame(System.Action showEndPanel)
    {
        if (IsGameOver)
            return;

        IsGameOver = true;

        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.SetGameOver();

        if (playerMovement != null)
        {
            playerMovement.SetControlEnabled(false);
            playerMovement.SetVelocity(Vector2.zero);
        }

        if (playerControls != null)
            playerControls.enabled = false;

        if (uiControls != null)
            uiControls.enabled = false;

        showEndPanel?.Invoke();
    }
}
