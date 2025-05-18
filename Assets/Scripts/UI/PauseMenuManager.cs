using UnityEngine;
using UnityEngine.SceneManagement; // For loading scenes
using UnityEngine.InputSystem; // If you use the new Input System for the pause key

public class PauseMenuManager : MonoBehaviour
{
	public static PauseMenuManager Instance { get; private set; }

	[Header("UI Panels")]
	public GameObject pauseMenuPanel;       // Assign your main PauseMenuPanel
	public GameObject settingsPanel_InGame; // Assign your in-game SettingsPanel
	public GameObject helpPanel_InGame;     // Assign your in-game HelpPanel (NEW)

	[Header("Main Menu Scene")]
	public string mainMenuSceneName = "MainMenu_SimpleScene"; // Or your main menu scene name

	public static bool isGamePaused = false;

	[Header("Input (Optional - if handling directly)")]
	public KeyCode pauseKey = KeyCode.Escape; // For old Input Manager
											  // public InputActionReference pauseInputAction; // For new Input System

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;

		// Ensure panels are correctly set at start
		if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
		if (settingsPanel_InGame != null) settingsPanel_InGame.SetActive(false);
		if (helpPanel_InGame != null) helpPanel_InGame.SetActive(false); // NEW

		isGamePaused = false;
		Time.timeScale = 1f;
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
		if (isGamePaused)
		{
			ResumeGame();
		}
		else
		{
			PauseGame();
		}
	}

	public void ResumeGame()
	{
		if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
		if (settingsPanel_InGame != null) settingsPanel_InGame.SetActive(false);
		if (helpPanel_InGame != null) helpPanel_InGame.SetActive(false); // NEW

		Time.timeScale = 1f;
		isGamePaused = false;
		Debug.Log("Game Resumed");

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void PauseGame()
	{
		if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
		// Ensure sub-panels are initially hidden when main pause panel appears
		if (settingsPanel_InGame != null) settingsPanel_InGame.SetActive(false);
		if (helpPanel_InGame != null) helpPanel_InGame.SetActive(false);

		Time.timeScale = 0f;
		isGamePaused = true;
		Debug.Log("Game Paused");

		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	// --- Button Functions ---
	public void OpenSettings_InGame()
	{
		Debug.Log("In-Game Settings button pressed.");
		if (settingsPanel_InGame != null) settingsPanel_InGame.SetActive(true);
		// Optionally hide the main pause buttons if settings is a full overlay
		// HideMainPauseButtons(); 
	}

	public void CloseSettings_InGame()
	{
		Debug.Log("Closing In-Game Settings panel.");
		if (settingsPanel_InGame != null) settingsPanel_InGame.SetActive(false);
		// ShowMainPauseButtons();
	}

	public void OpenHelp_InGame() // NEW
	{
		Debug.Log("In-Game Help button pressed.");
		if (helpPanel_InGame != null) helpPanel_InGame.SetActive(true);
		// HideMainPauseButtons();
	}

	public void CloseHelp_InGame() // NEW
	{
		Debug.Log("Closing In-Game Help panel.");
		if (helpPanel_InGame != null) helpPanel_InGame.SetActive(false);
		// ShowMainPauseButtons();
	}

	public void ExitToMainMenu()
	{
		Debug.Log("Exiting to Main Menu: " + mainMenuSceneName);
		Time.timeScale = 1f;
		isGamePaused = false;
		Debug.Log("Exiting to Main Menu: " + mainMenuSceneName);
		Time.timeScale = 1f;
		isGamePaused = false;

		// --- SAVE GAME STATE ---
		if (GameDataManager.Instance != null)
		{
			GameDataManager.Instance.SaveCurrentGameState();
		}
		SceneManager.LoadScene(mainMenuSceneName);
	}

	// Optional helper methods if you want to hide/show the main pause buttons when a sub-panel opens
	// void HideMainPauseButtons() {
	//     Transform buttonContainer = pauseMenuPanel.transform.Find("PauseButtonContainer");
	//     if (buttonContainer != null) buttonContainer.gameObject.SetActive(false);
	// }
	// void ShowMainPauseButtons() {
	//     Transform buttonContainer = pauseMenuPanel.transform.Find("PauseButtonContainer");
	//     if (buttonContainer != null) buttonContainer.gameObject.SetActive(true);
	// }
}