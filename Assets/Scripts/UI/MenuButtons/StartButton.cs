using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    [SerializeField] private int sceneIndex = 1;

    public void OnClick()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }
}
