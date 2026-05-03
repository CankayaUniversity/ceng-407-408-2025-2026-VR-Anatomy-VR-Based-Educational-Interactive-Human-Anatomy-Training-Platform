using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    [System.Serializable]
    public struct IntroStep
    {
        public Button button;
        public AudioClip clip;
    }

    [Header("Configuration")]
    [SerializeField] private IntroStep[] introSteps;
    [SerializeField] private GameObject inputBlocker;
    [SerializeField] private Button skipButton;

    private AudioSource _audioSource;
    private bool _introRunning = false;

    void Awake()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        if (skipButton != null)
            skipButton.onClick.AddListener(StopIntro);

        StartCoroutine(RunIntro());
    }

    private IEnumerator RunIntro()
    {
        _introRunning = true;

        if (inputBlocker != null) inputBlocker.SetActive(true);
        if (skipButton != null) skipButton.gameObject.SetActive(true);

        foreach (var step in introSteps)
        {
            if (!_introRunning) break;
            if (step.clip == null) continue;

            MenuItemHighlighter highlighter = null;
            if (step.button != null)
            {
                highlighter = step.button.GetComponent<MenuItemHighlighter>();
                if (highlighter != null) highlighter.Highlight();
            }

            _audioSource.clip = step.clip;
            _audioSource.Play();
            yield return new WaitForSeconds(step.clip.length);

            if (highlighter != null) highlighter.Unhighlight();
        }

        StopIntro();
    }

    public void StopIntro()
    {
        _introRunning = false;

        if (_audioSource != null) _audioSource.Stop();
        if (inputBlocker != null) inputBlocker.SetActive(false);
        if (skipButton != null) skipButton.gameObject.SetActive(false);

    
        ResetAllButtons();
    }

    private void ResetAllButtons()
    {
        foreach (var step in introSteps)
        {
            if (step.button != null)
            {
                var highlighter = step.button.GetComponent<MenuItemHighlighter>();
                if (highlighter != null)
                {
                    highlighter.Unhighlight();
                }
            }
        }
    }
}