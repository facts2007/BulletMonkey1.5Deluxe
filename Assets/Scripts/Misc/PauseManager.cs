using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pausePanel;
    public GameObject pauseTitle;
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
        if (GameSceneFlow.IsLoading) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(TutorialOverlay.Instance!=null && TutorialOverlay.Instance.IsOpen){TutorialOverlay.Instance.Close();return;}
            if (GameIsPaused)
            {
                if (MenuPanelMotion.IsVisible(settingsPanel)) CloseSettings();
                else Resume();
            }
            else if (Time.timeScale > 0f) Pause();
        }
    }

    public void Pause()
    {
        GameIsPaused = true;
        Time.timeScale = 0f;

        MenuPanelMotion.Show(pausePanel);
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

        MenuPanelMotion.Hide(pausePanel);
        SetEscHintVisible(true);
        if (gameAudio != null) gameAudio.SetPaused(false);

        LockCursor();
    }

    public void BackToMainMenu()
    {
        PlayerPrefs.Save();
        GameIsPaused = false;
        Time.timeScale = 1f;

        GameSceneFlow.Load(mainMenuSceneName);
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
        if(pauseTitle!=null)pauseTitle.SetActive(false);
        SetMenuButtonsVisible(false);
        MenuPanelMotion.Show(settingsPanel);
    }

    public void CloseSettings()
    {
        bool wasOpen = MenuPanelMotion.IsVisible(settingsPanel);
        MenuPanelMotion.Hide(settingsPanel, true);
        if (wasOpen) MenuPanelMotion.Show(pauseTitle);
        else if(pauseTitle!=null)pauseTitle.SetActive(true);
        SetMenuButtonsVisible(true);
        PlayerPrefs.Save();
    }

    private void SetMenuButtonsVisible(bool visible)
    {
        if (menuButtons == null) return;
        foreach (GameObject button in menuButtons)
            if (visible) MenuPanelMotion.Show(button);
            else MenuPanelMotion.Hide(button, true);
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


