using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem; // Though not fully used, kept for potential future use.

public class PauseMenuManager : MonoBehaviour
{
	public static PauseMenuManager Instance { get; private set; }

	[Header("UI Panels")]
	public GameObject pauseMenuPanel; // Main panel for the pause menu.
	public GameObject settingsPanel_InGame; // Panel for in-game settings.
	public GameObject helpPanel_InGame; // Panel for in-game help.

	[Header("Settings Panel UI (Assign in Inspector)")]
	public Slider masterVolumeSlider; // Slider for master volume.
	public Slider musicVolumeSlider; // Slider for music volume.
	public Slider sfxVolumeSlider; // Slider for SFX volume.
	public Toggle bloomToggle; // Toggle for Bloom graphics effect.

	[Header("Main Menu Scene")]
	public string mainMenuSceneName = "MainMenu_SimpleScene"; // Name of the main menu scene.

	public static bool isGamePaused = false; // Static flag to check if game is paused.

	[Header("Input")]
	public KeyCode pauseKey = KeyCode.Escape; // Key to toggle pause menu.
											  // public InputActionReference pauseInputAction; // For new Input System (currently commented out).

	// Called when the script instance is being loaded.
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

	// Called before the first frame update.
	void Start()
	{
		SetupVolumeSliderListeners();
		SetupGraphicsSettingListeners();

		if (settingsPanel_InGame != null && settingsPanel_InGame.activeSelf)
		{
			LoadAllSettingsToUI();
		}
	}

	// Called every frame.
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

	// Toggles the pause state of the game.
	public void TogglePause()
	{
		if (isGamePaused) ResumeGame();
		else PauseGame();
	}

	// Resumes the game from a paused state.
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

	// Pauses the game.
	private void PauseGame()
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

	// Opens the in-game settings panel.
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

	// Closes the in-game settings panel.
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

	// Loads all current settings (volume, graphics) to the UI elements.
	private void LoadAllSettingsToUI()
	{
		LoadVolumeSettingsToUI();
		LoadGraphicsSettingsToUI();
	}

	// Loads current volume settings to the UI sliders.
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

	// Loads current graphics settings (e.g., Bloom) to the UI elements.
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

	// Sets up event listeners for the volume sliders.
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

	// Sets up event listeners for graphics settings UI elements.
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

	// Opens the in-game help panel.
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

	// Closes the in-game help panel.
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

	// Exits the current game session and returns to the main menu.
	public void ExitToMainMenu()
	{
		Debug.Log("Exiting to Main Menu: " + mainMenuSceneName);
		Time.timeScale = 1f;
		isGamePaused = false;

		if (GameDataManager.Instance != null && GameDataManager.CurrentSaveSlot != -1)
		{
			GameDataManager.Instance.SaveActiveGameState();
		}
		SceneManager.LoadScene(mainMenuSceneName);
	}
}