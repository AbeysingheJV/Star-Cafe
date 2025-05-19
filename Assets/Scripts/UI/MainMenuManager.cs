using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System;

public class MainMenuManager : MonoBehaviour
{
	[Header("Scene Configuration")]
	public string mainGameSceneName = "KitchenScene"; // Name of the main game scene.

	[Header("Main Menu Panels (Assign in Inspector)")]
	public GameObject mainMenuButtonPanel; // Panel holding main menu buttons.
	public GameObject settingsPanel_MainMenu; // Settings panel for the main menu.
	public GameObject creditsPanel_MainMenu; // Credits panel.
	public GameObject saveLoadPanel; // Panel for save/load slot selection.

	[Header("Save/Load Slot UI (Assign in Inspector)")]
	public Button[] saveSlotButtons; // Array of UI buttons for save slots.
	public TextMeshProUGUI[] saveSlotInfoTexts; // Array of UI texts for save slot info.
	[SerializeField] private TextMeshProUGUI saveLoadPanelTitleText; // Title text for save/load panel.

	[Header("Settings Panel UI - Main Menu (Assign in Inspector)")]
	public Slider masterVolumeSlider_MainMenu; // Master volume slider in main menu settings.
	public Slider musicVolumeSlider_MainMenu; // Music volume slider.
	public Slider sfxVolumeSlider_MainMenu; // SFX volume slider.

	private bool isLoadingGame; // Flag to indicate if the current operation is loading a game.

	// Called before the first frame update.
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

	// Called when the "New Game" button is clicked.
	public void OnNewGameClicked()
	{
		Debug.Log("MainMenuManager: OnNewGameClicked() called.");
		isLoadingGame = false;
		ShowSaveLoadPanel("CHOOSE SLOT FOR NEW GAME");
	}

	// Called when the "Continue" or "Load Game" button is clicked.
	public void OnContinueClicked()
	{
		Debug.Log("MainMenuManager: OnContinueClicked() called.");
		isLoadingGame = true;
		ShowSaveLoadPanel("SELECT SLOT TO LOAD");
	}

	// Opens the settings panel in the main menu.
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

	// Closes the settings panel in the main menu.
	public void CloseSettings_MainMenu()
	{
		Debug.Log("MainMenuManager: Closing Main Menu Settings panel.");
		if (settingsPanel_MainMenu != null) settingsPanel_MainMenu.SetActive(false);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(true);
	}

	// Opens the credits panel in the main menu.
	public void OpenCredits_MainMenu()
	{
		Debug.Log("MainMenuManager: Credits button pressed.");
		if (creditsPanel_MainMenu != null) creditsPanel_MainMenu.SetActive(true);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(false);
	}

	// Closes the credits panel in the main menu.
	public void CloseCredits_MainMenu()
	{
		Debug.Log("MainMenuManager: Closing Main Menu Credits panel.");
		if (creditsPanel_MainMenu != null) creditsPanel_MainMenu.SetActive(false);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(true);
	}

	// Exits the game application.
	public void ExitGame()
	{
		Debug.Log("MainMenuManager: Exit Game button pressed.");
		Application.Quit();
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}

	// Shows the save/load panel with a given title and populates save slot info.
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

	// Closes the save/load panel.
	public void CloseSaveLoadPanel()
	{
		if (saveLoadPanel != null) saveLoadPanel.SetActive(false);
		if (mainMenuButtonPanel != null) mainMenuButtonPanel.SetActive(true);
	}

	// Populates the UI for save slots with their current status.
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

	// Called when a save slot button is clicked by the player.
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
		else
		{
			if (SaveSystem.DoesSaveExist(slotNumber))
			{
				Debug.LogWarning($"MainMenuManager: Slot {slotNumber} contains save data. Overwriting. (Implement confirmation pop-up later)");
			}
			GameDataManager.Instance.PrepareNewGame(slotNumber, mainGameSceneName);
			SceneManager.LoadScene(mainGameSceneName);
		}
	}

	// Loads current audio settings to the main menu's settings UI sliders.
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

	// Sets up listeners for the volume sliders in the main menu settings.
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