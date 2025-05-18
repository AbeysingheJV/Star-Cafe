using UnityEngine;
using UnityEngine.UI; // Required for Button, Slider
using UnityEngine.SceneManagement;
using TMPro; // Required for TextMeshProUGUI
using System.Collections.Generic; // Required for List
using System; // For DateTime

public class MainMenuManager : MonoBehaviour
{
	[Header("Scene Configuration")]
	public string mainGameSceneName = "KitchenScene"; // IMPORTANT: Change to your actual game scene name!

	[Header("Main Menu Panels (Assign in Inspector)")]
	public GameObject mainMenuButtonPanel;
	public GameObject settingsPanel_MainMenu;
	public GameObject creditsPanel_MainMenu;
	public GameObject saveLoadPanel;

	[Header("Save/Load Slot UI (Assign in Inspector)")]
	public Button[] saveSlotButtons;
	public TextMeshProUGUI[] saveSlotInfoTexts;
	[SerializeField] private TextMeshProUGUI saveLoadPanelTitleText;

	[Header("Settings Panel UI - Main Menu (Assign in Inspector)")]
	public Slider masterVolumeSlider_MainMenu;
	public Slider musicVolumeSlider_MainMenu;
	public Slider sfxVolumeSlider_MainMenu;

	private bool isLoadingGame;

	void Start()
	{
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(true);
		if (settingsPanel_MainMenu != null) settingsPanel_MainMenu.SetActive(false);
		if (creditsPanel_MainMenu != null) creditsPanel_MainMenu.SetActive(false);
		if (saveLoadPanel != null) saveLoadPanel.SetActive(false);

		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		SetupMainMenuSliderListeners();
		if (AudioManager.Instance != null && settingsPanel_MainMenu != null && settingsPanel_MainMenu.activeSelf)
		{
			LoadSettingsToMainMenuUI();
		}
	}

	// --- Main Menu Button Actions ---
	public void OnNewGameClicked()
	{
		Debug.Log("MainMenuManager: OnNewGameClicked() called.");
		isLoadingGame = false;
		ShowSaveLoadPanel("CHOOSE SLOT FOR NEW GAME");
	}

	public void OnContinueClicked()
	{
		Debug.Log("MainMenuManager: OnContinueClicked() called.");
		isLoadingGame = true;
		ShowSaveLoadPanel("SELECT SLOT TO LOAD");
	}

	public void OpenSettings_MainMenu()
	{
		Debug.Log("MainMenuManager: Settings button pressed.");
		if (settingsPanel_MainMenu != null)
		{
			settingsPanel_MainMenu.SetActive(true);
			LoadSettingsToMainMenuUI();
			if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(false);
		}
	}

	public void CloseSettings_MainMenu()
	{
		Debug.Log("MainMenuManager: Closing Main Menu Settings panel.");
		if (settingsPanel_MainMenu != null) settingsPanel_MainMenu.SetActive(false);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(true);
	}

	public void OpenCredits_MainMenu()
	{
		Debug.Log("MainMenuManager: Credits button pressed.");
		if (creditsPanel_MainMenu != null) creditsPanel_MainMenu.SetActive(true);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(false);
	}

	public void CloseCredits_MainMenu()
	{
		Debug.Log("MainMenuManager: Closing Main Menu Credits panel.");
		if (creditsPanel_MainMenu != null) creditsPanel_MainMenu.SetActive(false);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(true);
	}

	public void ExitGame()
	{
		Debug.Log("MainMenuManager: Exit Game button pressed.");
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
			Debug.LogError("MainMenuManager Error: SaveLoadPanel is not assigned in the Inspector!");
			return;
		}

		if (saveLoadPanelTitleText != null) saveLoadPanelTitleText.text = title;

		PopulateSaveSlots();

		saveLoadPanel.SetActive(true);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(false);
	}

	public void CloseSaveLoadPanel()
	{
		if (saveLoadPanel != null) saveLoadPanel.SetActive(false);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(true);
	}

	private void PopulateSaveSlots()
	{
		if (saveSlotButtons == null || saveSlotInfoTexts == null ||
			saveSlotButtons.Length != saveSlotInfoTexts.Length ||
			saveSlotButtons.Length == 0)
		{
			Debug.LogError("MainMenuManager Error: Save slot UI elements not configured correctly or are empty in the Inspector!");
			if (saveLoadPanel != null && saveLoadPanelTitleText != null) saveLoadPanelTitleText.text = "SAVE SLOT UI ERROR";
			return;
		}

		List<SaveSlotInfo> slotsData = SaveSystem.GetSaveSlotsInfo();
		if (slotsData == null)
		{
			Debug.LogError("MainMenuManager Error: SaveSystem.GetSaveSlotsInfo() returned null!");
			if (saveLoadPanel != null && saveLoadPanelTitleText != null) saveLoadPanelTitleText.text = "ERROR LOADING SLOTS";
			return;
		}

		for (int i = 0; i < saveSlotButtons.Length; i++)
		{
			Button slotButton = saveSlotButtons[i];
			TextMeshProUGUI infoText = saveSlotInfoTexts[i];

			if (slotButton == null) { Debug.LogError($"MainMenuManager Error: SaveSlotButtons element {i} is null!"); continue; }

			TextMeshProUGUI slotButtonLabel = slotButton.GetComponentInChildren<TextMeshProUGUI>();
			if (slotButtonLabel == null) Debug.LogError($"MainMenuManager Error: Button for slot {i + 1} is missing TextMeshProUGUI child for label!");
			else slotButtonLabel.text = $"SLOT {i + 1}";

			if (i < slotsData.Count)
			{
				SaveSlotInfo slotInfo = slotsData[i];
				if (slotInfo == null)
				{
					Debug.LogError($"MainMenuManager Error: slotInfo at index {i} is null from SaveSystem.GetSaveSlotsInfo()!");
					if (infoText != null) infoText.text = "Error";
					slotButton.interactable = false;
					continue;
				}

				if (slotInfo.IsUsed)
				{
					if (infoText != null) infoText.text = $"Last Saved: {slotInfo.LastSaved.ToString("g")}";
					slotButton.interactable = true;
				}
				else
				{
					if (infoText != null) infoText.text = "Empty Slot";
					slotButton.interactable = !isLoadingGame;
				}

				int slotIndex = i;
				slotButton.onClick.RemoveAllListeners();
				slotButton.onClick.AddListener(() => OnSaveSlotClicked(slotIndex));
			}
			else
			{
				Debug.LogWarning($"MainMenuManager: Not enough data in slotsData for UI button index {i}. Disabling button.");
				if (infoText != null) infoText.text = "N/A";
				slotButton.interactable = false;
				if (slotButton.gameObject.activeSelf) slotButton.gameObject.SetActive(false);
			}
		}
	}

	public void OnSaveSlotClicked(int slotNumber)
	{
		Debug.Log($"MainMenuManager: Save Slot {slotNumber + 1} clicked. isLoadingGame: {isLoadingGame}");
		if (GameDataManager.Instance == null)
		{
			Debug.LogError("MainMenuManager Error: GameDataManager.Instance is null! Ensure GameDataManager_Handler is in this scene and active.");
			if (saveLoadPanelTitleText != null) saveLoadPanelTitleText.text = "SYSTEM ERROR";
			return;
		}

		if (isLoadingGame)
		{
			if (SaveSystem.DoesSaveExist(slotNumber))
			{
				// Use PrepareLoadGameFromSlot and pass the scene name
				bool prepared = GameDataManager.Instance.PrepareLoadGameFromSlot(slotNumber, mainGameSceneName);
				if (prepared)
				{
					SceneManager.LoadScene(mainGameSceneName);
				}
				else
				{
					Debug.LogError($"MainMenuManager: Failed to prepare load game from slot {slotNumber}.");
					if (saveLoadPanelTitleText != null) saveLoadPanelTitleText.text = "LOAD FAILED";
				}
			}
		}
		else // New Game
		{
			if (SaveSystem.DoesSaveExist(slotNumber))
			{
				Debug.LogWarning($"MainMenuManager: Slot {slotNumber} contains save data. Overwriting. (Implement confirmation pop-up later)");
			}
			// Use PrepareNewGame and pass the scene name
			GameDataManager.Instance.PrepareNewGame(slotNumber, mainGameSceneName);
			SceneManager.LoadScene(mainGameSceneName);
		}
	}

	// --- Main Menu Settings UI Logic ---
	private void LoadSettingsToMainMenuUI()
	{
		if (AudioManager.Instance == null)
		{
			Debug.LogError("MainMenuManager: AudioManager.Instance is null. Cannot load settings to Main Menu UI.");
			return;
		}
		if (masterVolumeSlider_MainMenu != null) masterVolumeSlider_MainMenu.value = AudioManager.Instance.MasterVolumeSetting;
		if (musicVolumeSlider_MainMenu != null) musicVolumeSlider_MainMenu.value = AudioManager.Instance.MusicVolumeSetting;
		if (sfxVolumeSlider_MainMenu != null) sfxVolumeSlider_MainMenu.value = AudioManager.Instance.SFXVolumeSetting;
	}

	private void SetupMainMenuSliderListeners()
	{
		if (AudioManager.Instance == null)
		{
			Debug.LogWarning("MainMenuManager: AudioManager.Instance is null during SetupMainMenuSliderListeners. Listeners will not be set if AudioManager is not ready.");
			return;
		}

		if (masterVolumeSlider_MainMenu != null)
		{
			masterVolumeSlider_MainMenu.onValueChanged.RemoveAllListeners();
			masterVolumeSlider_MainMenu.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);
		}
		if (musicVolumeSlider_MainMenu != null)
		{
			musicVolumeSlider_MainMenu.onValueChanged.RemoveAllListeners();
			musicVolumeSlider_MainMenu.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
		}
		if (sfxVolumeSlider_MainMenu != null)
		{
			sfxVolumeSlider_MainMenu.onValueChanged.RemoveAllListeners();
			sfxVolumeSlider_MainMenu.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
		}
	}
}