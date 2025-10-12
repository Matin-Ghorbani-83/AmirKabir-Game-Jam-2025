using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] Text text;
    public void StartGame() => SceneManager.LoadScene(1);
    private void Start()
    {
        text.text = PlayerPrefs.GetString("Timer");
    }

}
