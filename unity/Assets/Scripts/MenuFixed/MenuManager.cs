using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    [SerializeField] private GameObject menuRoot;
    [SerializeField] private KeyCode toggleKey = KeyCode.M;

    
    [SerializeField] private string menuSceneName = "MenuFixed";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneFinishedLoading;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneFinishedLoading;
    }

    private void OnSceneFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != menuSceneName)
        {
            if (menuRoot.activeSelf)
            {
                menuRoot.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(!menuRoot.activeSelf);
        }
    }
}