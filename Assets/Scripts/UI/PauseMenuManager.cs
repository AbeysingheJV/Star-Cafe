using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Required for Slider and Toggle
using TMPro;          // Required for TextMeshProUGUI (if displaying slider values)
using UnityEngine.InputSystem; // If using new Input System for pause

public class PauseMenuManager : MonoBehaviour
{
	public static PauseMenuManager Instance { get; private set; }

	[Header("UI Panels")]
	public GameObject pauseMenuPanel;       // Assign your main PauseMenuPanel
	public GameObject settingsPanel_InGame; // Assign your in-game SettingsPanel
	public GameObject helpPanel_InGame;     // Assign your in-game HelpPanel

	[Header("Settings Panel UI (Assign in Inspector)")]
	public Slider masterVolumeSlider;
	public Slider musicVolumeSlider;
	public Slider sfxVolumeSlider;
	public Toggle bloomToggle;

	[Header("Main Menu Scene")]
	public string mainMenuSceneName = "MainMenu_SimpleScene"; // Or your main menu scene name

	public static bool isGamePaused = false;

	[Header("Input")]
	public KeyCode pauseKey = KeyCode.Escape;
	// public InputActionReference pauseInputAction; // For new Input System

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;

		if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
		else Debug.LogError("PauseMenuManager: PauseMenuPanel not assigned!");

		if (settingsPanel_InGame != null) settingsPanel_InGame.SetActive(false);
		else Debug.LogError("PauseMenuManager: SettingsPanel_InGame not assigned!");

		if (helpPanel_InGame != null) helpPanel_InGame.SetActive(false);
		else Debug.LogWarning("PauseMenuManager: HelpPanel_InGame not assigned (optional).");

		isGamePaused = false;
		Time.timeScale = 1f;
	}

	void Start()
	{
		SetupVolumeSliderListeners();
		SetupGraphicsSettingListeners();

		if (settingsPanel_InGame != null && settingsPanel_InGame.activeSelf)
		{
			LoadAllSettingsToUI();
		}
	}

	void Update()
	{
		if (Input.GetKeyDown(pauseKey))
		{
			TogglePause();
		}
		// if (pauseInputAction != null && pauseInputAction.action.WasPressedThisFrame())
		// {
		//    TogglePause();
		// }
	}

	public void TogglePause()
	{
		if (isGamePaused) ResumeGame();
		else PauseGame();
	}

	public void ResumeGame()
	{
		if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
		if (settingsPanel_InGame != null) settingsPanel_InGame.SetActive(false);
		if (helpPanel_InGame != null) helpPanel_InGame.SetActive(false);

		Time.timeScale = 1f;
		isGamePaused = false;
		Debug.Log("Game Resumed");

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void PauseGame()
	{
		if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
		if (settingsPanel_InGame != null) settingsPanel_InGame.SetActive(false);
		if (helpPanel_InGame != null) helpPanel_InGame.SetActive(false);

		Time.timeScale = 0f;
		isGamePaused = true;
		Debug.Log("Game Paused");

		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	public void OpenSettings_InGame()
	{
		Debug.Log("In-Game Settings button pressed.");
		if (settingsPanel_InGame != null)
		{
			settingsPanel_InGame.SetActive(true);
			LoadAllSettingsToUI();
			if (pauseMenuPanel != null)
			{
				Transform buttonContainer = pauseMenuPanel.transform.Find("PauseButtonContainer");
				if (buttonContainer != null) buttonContainer.gameObject.SetActive(false);
			}
		}
	}

	public void CloseSettings_InGame()
	{
		Debug.Log("Closing In-Game Settings panel.");
		if (settingsPanel_InGame != null) settingsPanel_InGame.SetActive(false);
		if (pauseMenuPanel != null)
		{
			Transform buttonContainer = pauseMenuPanel.transform.Find("PauseButtonContainer");
			if (buttonContainer != null) buttonContainer.gameObject.SetActive(true);
		}
	}

	private void LoadAllSettingsToUI()
	{
		LoadVolumeSettingsToUI();
		LoadGraphicsSettingsToUI();
	}

	private void LoadVolumeSettingsToUI()
	{
		if (AudioManager.Instance == null)
		{
			Debug.LogError("PauseMenuManager: AudioManager.Instance is null. Cannot load volume settings to UI.");
			return;
		}
		if (masterVolumeSlider != null) masterVolumeSlider.value = AudioManager.Instance.MasterVolumeSetting;
		if (musicVolumeSlider != null) musicVolumeSlider.value = AudioManager.Instance.MusicVolumeSetting;
		if (sfxVolumeSlider != null) sfxVolumeSlider.value = AudioManager.Instance.SFXVolumeSetting;
	}

	private void LoadGraphicsSettingsToUI()
	{
		if (GraphicsSettingsManager.Instance == null)
		{
			Debug.LogError("PauseMenuManager: GraphicsSettingsManager.Instance is null. Cannot load graphics settings to UI.");
			return;
		}
		if (bloomToggle != null)
		{
			bloomToggle.onValueChanged.RemoveListener(GraphicsSettingsManager.Instance.SetBloom);
			bloomToggle.isOn = GraphicsSettingsManager.Instance.IsBloomActive;
			bloomToggle.onValueChanged.AddListener(GraphicsSettingsManager.Instance.SetBloom);
		}
	}

	private void SetupVolumeSliderListeners()
	{
		if (AudioManager.Instance == null)
		{
			Debug.LogWarning("PauseMenuManager: AudioManager.Instance is null during SetupVolumeSliderListeners. Volume listeners not set.");
			return;
		}
		if (masterVolumeSlider != null) { masterVolumeSlider.onValueChanged.RemoveAllListeners(); masterVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume); }
		if (musicVolumeSlider != null) { musicVolumeSlider.onValueChanged.RemoveAllListeners(); musicVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume); }
		if (sfxVolumeSlider != null) { sfxVolumeSlider.onValueChanged.RemoveAllListeners(); sfxVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume); }
	}

	private void SetupGraphicsSettingListeners()
	{
		if (GraphicsSettingsManager.Instance == null)
		{
			Debug.LogWarning("PauseMenuManager: GraphicsSettingsManager.Instance is null during SetupGraphicsSettingListeners. Bloom listener not set.");
			return;
		}
		if (bloomToggle != null)
		{
			bloomToggle.onValueChanged.RemoveAllListeners();
			bloomToggle.onValueChanged.AddListener(GraphicsSettingsManager.Instance.SetBloom);
		}
	}

	public void OpenHelp_InGame()
	{
		Debug.Log("In-Game Help button pressed.");
		if (helpPanel_InGame != null) helpPanel_InGame.SetActive(true);
		if (pauseMenuPanel != null)
		{
			Transform buttonContainer = pauseMenuPanel.transform.Find("PauseButtonContainer");
			if (buttonContainer != null) buttonContainer.gameObject.SetActive(false);
		}
	}

	public void CloseHelp_InGame()
	{
		Debug.Log("Closing In-Game Help panel.");
		if (helpPanel_InGame != null) helpPanel_InGame.SetActive(false);
		if (pauseMenuPanel != null)
		{
			Transform buttonContainer = pauseMenuPanel.transform.Find("PauseButtonContainer");
			if (buttonContainer != null) buttonContainer.gameObject.SetActive(true);
		}
	}

	public void ExitToMainMenu()
	{
		Debug.Log("Exiting to Main Menu: " + mainMenuSceneName);
		Time.timeScale = 1f;
		isGamePaused = false;

		if (GameDataManager.Instance != null && GameDataManager.CurrentSaveSlot != -1)
		{
			// Corrected method name:
			GameDataManager.Instance.SaveActiveGameState();
		}
		SceneManager.LoadScene(mainMenuSceneName);
	}
}