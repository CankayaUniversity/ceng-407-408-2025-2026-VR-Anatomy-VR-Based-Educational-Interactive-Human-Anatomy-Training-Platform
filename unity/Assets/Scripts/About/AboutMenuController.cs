using UnityEngine;
using UnityEngine.SceneManagement;

public class AboutMenuController : MonoBehaviour
{
    public void GoToMainMenu()
    {
        SceneManager.LoadScene("01_Menu");
    }
}