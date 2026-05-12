using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIMenuChanger menuChanger;

    private void Awake()
    {
        if (menuChanger == null)
        {
            menuChanger = GetComponent<UIMenuChanger>();
        }
    }

    public void OnUIBack()
    {
        menuChanger?.OnUIBack();
    }

    public void OnUIPause()
    {
        menuChanger?.OnUIPause();
    }

    public void OnUIMap()
    {
        menuChanger?.OnUIMap();
    }

    public void OnUIZoomIn()
    {
    }

    public void OnUIZoomOut()
    {
    }

    public void OnUIZoom(float delta)
    {
    }

    public void OnUIPin()
    {
    }
}
