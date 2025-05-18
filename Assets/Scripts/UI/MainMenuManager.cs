using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes

public class MainMenuManager : MonoBehaviour
{
	[Header("Scene Configuration")]
	public string mainGameSceneName = "KitchenScene"; // IMPORTANT: Change to your actual game scene name!

	[Header("UI Panels")]
	public GameObject mainMenuButtonPanel; // Assign your main button container/panel (e.g., ButtonContainer)
	public GameObject settingsPanel;       // Assign your SettingsPanel GameObject
	public GameObject creditsPanel;        // Assign your CreditsPanel GameObject

	void Start()
	{
		// Ensure only the main menu panel is visible at the start
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(true);
		if (settingsPanel != null) settingsPanel.SetActive(false);
		if (creditsPanel != null) creditsPanel.SetActive(false);
	}

	// --- NEW GAME ---
	public void NewGame()
	{
		Debug.Log("New Game button pressed. Loading scene: " + mainGameSceneName);
		// Optional: Reset PlayerPrefs or other game state for a new game
		// PlayerPrefs.DeleteAll();
		// PlayerPrefs.Save();
		SceneManager.LoadScene(mainGameSceneName);
	}

	// --- CONTINUE ---
	public void ContinueGame() // Add a ContinueButton if you want this functionality
	{
		Debug.Log("Continue button pressed. Loading scene: " + mainGameSceneName);
		// Implement save game loading logic here if you have one
		SceneManager.LoadScene(mainGameSceneName);
	}

	// --- SETTINGS ---
	public void OpenSettings()
	{
		Debug.Log("Settings button pressed.");
		if (settingsPanel != null) settingsPanel.SetActive(true);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(false);
	}

	public void CloseSettings()
	{
		Debug.Log("Closing Settings panel.");
		if (settingsPanel != null) settingsPanel.SetActive(false);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(true);
	}

	// --- CREDITS ---
	public void OpenCredits()
	{
		Debug.Log("Credits button pressed.");
		if (creditsPanel != null) creditsPanel.SetActive(true);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(false);
	}

	public void CloseCredits()
	{
		Debug.Log("Closing Credits panel.");
		if (creditsPanel != null) creditsPanel.SetActive(false);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(true);
	}

	// --- EXIT GAME ---
	public void ExitGame()
	{
		Debug.Log("Exit Game button pressed.");
		Application.Quit();

#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}
}