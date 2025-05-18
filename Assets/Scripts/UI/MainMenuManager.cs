using UnityEngine;
using UnityEngine.UI; // For Button
using UnityEngine.SceneManagement;
using TMPro; // For TextMeshProUGUI
using System.Collections.Generic; // For List
using System; // For DateTime

public class MainMenuManager : MonoBehaviour
{
	[Header("Scene Configuration")]
	public string mainGameSceneName = "KitchenScene"; // IMPORTANT: Change to your actual game scene name!

	[Header("Main Menu Panels")]
	public GameObject mainMenuButtonPanel; // Assign your main button container (New Game, Settings, etc.)
	public GameObject settingsPanel;
	public GameObject creditsPanel;
	public GameObject saveLoadPanel;       // Assign your new SaveLoadPanel GameObject

	[Header("Save/Load Slot UI (Assign in Inspector)")]
	public Button[] saveSlotButtons; // Assign your 3 (or MAX_SAVE_SLOTS) slot buttons here
	public TextMeshProUGUI[] saveSlotInfoTexts; // Assign the "SlotInfoText" child of each button

	// Optional: A TextMeshProUGUI on the SaveLoadPanel to show its title (e.g., "Select Slot to Load")
	[SerializeField] private TextMeshProUGUI saveLoadPanelTitleText;

	private bool isLoadingGame; // Flag to know if we opened the panel for "Continue" or "New Game"

	void Start()
	{
		// Ensure only the main menu panel is visible at the start
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(true);
		if (settingsPanel != null) settingsPanel.SetActive(false);
		if (creditsPanel != null) creditsPanel.SetActive(false);
		if (saveLoadPanel != null) saveLoadPanel.SetActive(false);

		// Disable Continue button if no saves exist at all (optional initial check)
		// UpdateContinueButtonState();
	}

	// --- Main Menu Button Actions ---
	public void OnNewGameClicked()
	{
		Debug.Log("New Game button pressed.");
		isLoadingGame = false;
		ShowSaveLoadPanel("CHOOSE A SLOT FOR NEW GAME");
	}

	public void OnContinueClicked()
	{
		Debug.Log("Continue button pressed.");
		isLoadingGame = true;
		ShowSaveLoadPanel("SELECT SLOT TO LOAD");
	}

	public void OpenSettings()
	{
		if (settingsPanel != null) settingsPanel.SetActive(true);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(false);
	}

	public void CloseSettings()
	{
		if (settingsPanel != null) settingsPanel.SetActive(false);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(true);
	}

	public void OpenCredits()
	{
		if (creditsPanel != null) creditsPanel.SetActive(true);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(false);
	}

	public void CloseCredits()
	{
		if (creditsPanel != null) creditsPanel.SetActive(false);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(true);
	}

	public void ExitGame()
	{
		Debug.Log("Exit Game button pressed.");
		Application.Quit();
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}

	// --- Save/Load Panel Logic ---
	private void ShowSaveLoadPanel(string title)
	{
		if (saveLoadPanel == null)
		{
			Debug.LogError("SaveLoadPanel is not assigned in MainMenuManager!");
			return;
		}

		if (saveLoadPanelTitleText != null) saveLoadPanelTitleText.text = title;
		PopulateSaveSlots();
		saveLoadPanel.SetActive(true);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(false);
	}

	public void CloseSaveLoadPanel() // Called by the "Back" button on SaveLoadPanel
	{
		if (saveLoadPanel != null) saveLoadPanel.SetActive(false);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(true);
	}

	private void PopulateSaveSlots()
	{
		if (saveSlotButtons == null || saveSlotInfoTexts == null || saveSlotButtons.Length != saveSlotInfoTexts.Length)
		{
			Debug.LogError("Save slot UI elements not configured correctly in MainMenuManager!");
			return;
		}

		List<SaveSlotInfo> slots = SaveSystem.GetSaveSlotsInfo();

		for (int i = 0; i < saveSlotButtons.Length; i++)
		{
			if (i < slots.Count) // Ensure we don't go out of bounds for slots list
			{
				SaveSlotInfo slotInfo = slots[i];
				Button slotButton = saveSlotButtons[i];
				TextMeshProUGUI slotButtonText = slotButton.GetComponentInChildren<TextMeshProUGUI>(); // Assuming main text is first TMP child
				TextMeshProUGUI infoText = saveSlotInfoTexts[i];

				if (slotButtonText != null) slotButtonText.text = $"SLOT {i + 1}";

				if (slotInfo.IsUsed)
				{
					infoText.text = $"Last Saved: {slotInfo.LastSaved.ToString("g")}"; // "g" for general date/short time
					slotButton.interactable = true; // Can always select a used slot
				}
				else
				{
					infoText.text = "Empty Slot";
					// If loading, an empty slot cannot be selected. If new game, it can.
					slotButton.interactable = !isLoadingGame;
				}

				// Remove previous listeners to avoid stacking them
				int slotIndex = i; // Capture slot index for the lambda
				slotButton.onClick.RemoveAllListeners();
				slotButton.onClick.AddListener(() => OnSaveSlotClicked(slotIndex));
			}
			else
			{
				// Should not happen if saveSlotButtons.Length matches SaveSystem.MAX_SAVE_SLOTS
				saveSlotButtons[i].gameObject.SetActive(false);
			}
		}
	}

	public void OnSaveSlotClicked(int slotNumber)
	{
		Debug.Log($"Save Slot {slotNumber + 1} clicked. isLoadingGame: {isLoadingGame}");
		if (GameDataManager.Instance == null)
		{
			Debug.LogError("GameDataManager.Instance is null! Cannot proceed.");
			return;
		}

		if (isLoadingGame) // Trying to Continue/Load
		{
			if (SaveSystem.DoesSaveExist(slotNumber))
			{
				bool success = GameDataManager.Instance.LoadGameFromSlot(slotNumber);
				if (success)
				{
					SceneManager.LoadScene(mainGameSceneName);
				}
				else
				{
					// Handle failed load, maybe show a message
					Debug.LogError($"Failed to load game from slot {slotNumber}.");
					if (saveLoadPanelTitleText != null) saveLoadPanelTitleText.text = "LOAD FAILED. TRY ANOTHER SLOT.";
				}
			}
			else
			{
				Debug.LogWarning($"Attempted to load empty or non-existent slot {slotNumber}.");
				// UI should ideally prevent this if button was not interactable
			}
		}
		else // Trying to Start a New Game
		{
			if (SaveSystem.DoesSaveExist(slotNumber))
			{
				// TODO: Implement a confirmation pop-up before overwriting
				Debug.LogWarning($"Slot {slotNumber} already contains save data. Overwriting for New Game.");
				// For now, we just overwrite. A real game would ask "Are you sure?"
				GameDataManager.Instance.StartNewGame(slotNumber);
				SceneManager.LoadScene(mainGameSceneName);
			}
			else
			{
				GameDataManager.Instance.StartNewGame(slotNumber);
				SceneManager.LoadScene(mainGameSceneName);
			}
		}
	}

	// Optional: Update Continue button interactability based on save files
	// public void UpdateContinueButtonState()
	// {
	//     if (continueButton != null) // Assuming you have a direct reference to the Continue button
	//     {
	//         bool anySaveExists = false;
	//         for (int i = 0; i < SaveSystem.MAX_SAVE_SLOTS; i++)
	//         {
	//             if (SaveSystem.DoesSaveExist(i))
	//             {
	//                 anySaveExists = true;
	//                 break;
	//             }
	//         }
	//         continueButton.interactable = anySaveExists;
	//     }
	// }
}