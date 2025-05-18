using UnityEngine;
using UnityEngine.SceneManagement; // For loading scenes
using UnityEngine.InputSystem; // If you use the new Input System for the pause key

public class PauseMenuManager : MonoBehaviour
{
	public static PauseMenuManager Instance { get; private set; }

	[Header("UI Panels")]
	public GameObject pauseMenuPanel;       // Assign your main PauseMenuPanel
	public GameObject settingsPanel_InGame; // Assign your in-game SettingsPanel
	public GameObject recipesPanel_InGame;  // Assign your in-game RecipesPanel

	[Header("Main Menu Scene")]
	public string mainMenuSceneName = "MainMenu_SimpleScene"; // Or your main menu scene name

	public static bool isGamePaused = false;

	// Reference to PlayerInputHandler if pause is handled there
	// public PlayerInputHandler playerInputHandler; // Assign if needed

	// Or, handle pause input directly in this script
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
		if (recipesPanel_InGame != null) recipesPanel_InGame.SetActive(false);
		isGamePaused = false; // Ensure game starts unpaused
		Time.timeScale = 1f; // Ensure time scale is normal at start
	}

	void Update()
	{
		// --- Input Handling for Pause ---
		// If using old Input Manager:
		if (Input.GetKeyDown(pauseKey))
		{
			TogglePause();
		}

		// If using new Input System and an InputActionReference:
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
		if (settingsPanel_InGame != null) settingsPanel_InGame.SetActive(false); // Ensure sub-panels are also hidden
		if (recipesPanel_InGame != null) recipesPanel_InGame.SetActive(false);

		Time.timeScale = 1f; // Resume game time
		isGamePaused = false;
		Debug.Log("Game Resumed");

		// Optional: Lock cursor again for gameplay
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void PauseGame()
	{
		if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
		Time.timeScale = 0f; // Pause game time
		isGamePaused = true;
		Debug.Log("Game Paused");

		// Optional: Unlock cursor for menu navigation
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	// --- Button Functions ---
	public void OpenSettings_InGame()
	{
		Debug.Log("In-Game Settings button pressed.");
		if (settingsPanel_InGame != null) settingsPanel_InGame.SetActive(true);
		// Optionally hide the main pause buttons if settings is a full overlay
		// if (pauseMenuPanel.transform.Find("PauseButtonContainer") != null)
		//    pauseMenuPanel.transform.Find("PauseButtonContainer").gameObject.SetActive(false);
	}

	public void CloseSettings_InGame()
	{
		Debug.Log("Closing In-Game Settings panel.");
		if (settingsPanel_InGame != null) settingsPanel_InGame.SetActive(false);
		// if (pauseMenuPanel.transform.Find("PauseButtonContainer") != null)
		//    pauseMenuPanel.transform.Find("PauseButtonContainer").gameObject.SetActive(true);
	}

	public void OpenRecipes_InGame()
	{
		Debug.Log("In-Game Recipes button pressed.");
		if (recipesPanel_InGame != null) recipesPanel_InGame.SetActive(true);
	}

	public void CloseRecipes_InGame()
	{
		Debug.Log("Closing In-Game Recipes panel.");
		if (recipesPanel_InGame != null) recipesPanel_InGame.SetActive(false);
	}

	public void ExitToMainMenu()
	{
		Debug.Log("Exiting to Main Menu: " + mainMenuSceneName);
		Time.timeScale = 1f; // IMPORTANT: Reset time scale before leaving the scene
		isGamePaused = false; // Reset pause state
		SceneManager.LoadScene(mainMenuSceneName);
	}
}
