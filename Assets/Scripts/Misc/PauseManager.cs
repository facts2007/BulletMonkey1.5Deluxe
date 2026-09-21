using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pausePanel;
    public Image escIcon;
    public TMP_Text escText;

    [Header("Scenes")]
    public string mainMenuSceneName = "MainMenu";

    public static bool GameIsPaused { get; private set; }

    private void Start()
    {
        GameIsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        SetEscHintVisible(true);

        LockCursor();
    }

    private void Update()
    {
        if (!GameIsPaused && Input.GetKeyDown(KeyCode.Escape))
        {
            Pause();
        }
    }

    public void Pause()
    {
        GameIsPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null) pausePanel.SetActive(true);
        SetEscHintVisible(false);

        UnlockCursor();
    }

    public void Resume()
    {
        GameIsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        SetEscHintVisible(true);

        LockCursor();
    }

    public void BackToMainMenu()
    {
        GameIsPaused = false;
        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetEscHintVisible(bool visible)
    {
        if (escIcon != null) escIcon.gameObject.SetActive(visible);
        if (escText != null) escText.gameObject.SetActive(visible);
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}