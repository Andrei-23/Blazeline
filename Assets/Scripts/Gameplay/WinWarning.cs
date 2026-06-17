using Unity.VisualScripting;
using UnityEngine;

public class WinWarning : MonoBehaviour
{
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private GameObject UIPanel;
    [SerializeField] private MainPortal mainPortal;
    bool warned = false;
    void Update()
    {
        if(!warned && objectiveManager.CanWin())
        {
            warned = true;
            UIPanel.SetActive(true);
            mainPortal.SetWinnableColor();
        }
    }
}
