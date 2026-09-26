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

    [Header("Audio settings")]
    public GameObject settingsPanel;
    public GameObject[] menuButtons;
    public Slider musicSlider;
    public Slider sfxSlider;
    public TMP_Text musicValue;
    public TMP_Text sfxValue;
    public GameAudio gameAudio;

    [Header("Scenes")]
    public string mainMenuSceneName = "MainMenu";

    public static bool GameIsPaused { get; private set; }

    private void Start()
    {
        GameIsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (gameAudio == null) gameAudio = GameAudio.Instance;
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(gameAudio != null ? gameAudio.MusicVolume : 0.7f);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(gameAudio != null ? gameAudio.SfxVolume : 0.8f);
            sfxSlider.onValueChanged.AddListener(SetSfxVolume);
        }
        UpdateVolumeLabels();
        SetEscHintVisible(true);

        LockCursor();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                if (settingsPanel != null && settingsPanel.activeSelf) CloseSettings();
                else Resume();
            }
            else if (Time.timeScale > 0f) Pause();
        }
    }

    public void Pause()
    {
        GameIsPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null) pausePanel.SetActive(true);
        CloseSettings();
        if (gameAudio != null) gameAudio.SetPaused(true);
        SetEscHintVisible(false);

        UnlockCursor();
    }

    public void Resume()
    {
        CloseSettings();
        GameIsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        SetEscHintVisible(true);
        if (gameAudio != null) gameAudio.SetPaused(false);

        LockCursor();
    }

    public void BackToMainMenu()
    {
        PlayerPrefs.Save();
        GameIsPaused = false;
        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OpenSettings()
    {
        if (settingsPanel == null) return;
        SetMenuButtonsVisible(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        SetMenuButtonsVisible(true);
        PlayerPrefs.Save();
    }

    private void SetMenuButtonsVisible(bool visible)
    {
        if (menuButtons == null) return;
        foreach (GameObject button in menuButtons) if (button != null) button.SetActive(visible);
    }

    public void SetMusicVolume(float value)
    {
        if (gameAudio != null) gameAudio.SetMusicVolume(value);
        UpdateVolumeLabels();
    }

    public void SetSfxVolume(float value)
    {
        if (gameAudio != null) gameAudio.SetSfxVolume(value);
        UpdateVolumeLabels();
    }

    private void UpdateVolumeLabels()
    {
        if (musicValue != null && musicSlider != null) musicValue.text = $"{musicSlider.value:P0}";
        if (sfxValue != null && sfxSlider != null) sfxValue.text = $"{sfxSlider.value:P0}";
    }

    private void OnDestroy()
    {
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
        if (GameIsPaused)
        {
            GameIsPaused = false;
            Time.timeScale = 1f;
        }
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
