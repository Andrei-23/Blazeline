using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButton : MonoBehaviour
{
    [SerializeField] private int sceneIndex = 0;

    public void OnClick()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }
}
