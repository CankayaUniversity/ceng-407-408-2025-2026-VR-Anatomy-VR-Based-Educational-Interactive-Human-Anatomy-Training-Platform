using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Ana menü
    public void LoadMenu()
    {
        SceneManager.LoadScene("01_Menu");
    }

    // Hareket Sistemi VR sahnesi
    public void LoadMotionSystem()
    {
        SceneManager.LoadScene("02_MotionSystem");
    }

    // Dolaşım Sistemi VR sahnesi
    public void LoadCirculationSystem()
    {
        SceneManager.LoadScene("03_CirculationSystem");
    }

    // Quiz sahnesi
    public void LoadQuiz()
    {
        SceneManager.LoadScene("04_Quiz");
    }

    // Yapay Zeka sahnesi
    public void LoadAIChat()
    {
        SceneManager.LoadScene("05_AIChat");
    }

    // Hakkında
    public void LoadAbout()
    {
        SceneManager.LoadScene("06_About");
    }

    // Ayarlar
    public void LoadSettings()
    {
        SceneManager.LoadScene("07_Settings");
    }

    // Uygulamadan çıkmak için (PC build’inde)
    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
