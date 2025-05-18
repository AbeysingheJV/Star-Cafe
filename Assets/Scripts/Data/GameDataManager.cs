using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.SceneManagement; // Required for scene management

public class GameDataManager : MonoBehaviour
{
	public static GameDataManager Instance { get; private set; }

	public static int CurrentSaveSlot { get; private set; } = -1;

	// Current game session data
	public int TotalDishesCompleted { get; private set; }
	public List<string> UnlockedRecipeNames { get; private set; } = new List<string>();
	public List<string> UnlockedMusicTrackNames { get; private set; } = new List<string>();

	[Header("System References (Can be auto-found if not assigned)")]
	[SerializeField] private OrderManager orderManager;
	[SerializeField] private BackgroundMusicPlayer musicPlayer;
	[SerializeField] private RewardManager rewardManager;

	[Header("Default New Game Unlocks")]
	public List<RecipeData> defaultInitialRecipes;
	public List<AudioClip> defaultInitialMusicTracks;

	private SaveData pendingDataToApply = null;
	private bool hasPendingDataToApply = false;
	private string sceneToLoadAfterDataPrep = "";


	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);

		SaveSystem.Initialize();

		// Attempt to find managers if not assigned in Inspector,
		// but this might be too early if they are in a scene not yet loaded.
		// We'll re-check in OnSceneLoaded.
		if (orderManager == null) orderManager = FindObjectOfType<OrderManager>();
		if (musicPlayer == null) musicPlayer = FindObjectOfType<BackgroundMusicPlayer>();
		if (rewardManager == null) rewardManager = FindObjectOfType<RewardManager>();
	}

	void OnEnable()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		// Re-acquire references to scene-specific managers
		if (orderManager == null) orderManager = FindObjectOfType<OrderManager>();
		if (musicPlayer == null) musicPlayer = FindObjectOfType<BackgroundMusicPlayer>(); // Though BGMusicPlayer is often DDOL too
		if (rewardManager == null) rewardManager = FindObjectOfType<RewardManager>();

		if (hasPendingDataToApply && pendingDataToApply != null && scene.name == sceneToLoadAfterDataPrep)
		{
			Debug.Log($"GameDataManager: Scene '{scene.name}' loaded. Applying pending save data.");
			ApplySaveDataToGameInternal(pendingDataToApply);
			pendingDataToApply = null;
			hasPendingDataToApply = false;
			sceneToLoadAfterDataPrep = "";
		}
	}

	public void PrepareNewGame(int slotNumber, string gameSceneName)
	{
		CurrentSaveSlot = slotNumber;
		sceneToLoadAfterDataPrep = gameSceneName;
		SaveData newSave = new SaveData();

		// Populate initial recipes from GameDataManager's own list
		if (defaultInitialRecipes != null)
		{
			foreach (RecipeData recipe in defaultInitialRecipes)
			{
				if (recipe != null && !string.IsNullOrEmpty(recipe.name) && !newSave.unlockedRecipeNames.Contains(recipe.name))
				{
					newSave.unlockedRecipeNames.Add(recipe.name);
				}
			}
		}
		else Debug.LogWarning("GameDataManager: 'defaultInitialRecipes' list not assigned in Inspector. New game might miss initial recipes.");

		// Populate initial music from GameDataManager's own list
		if (defaultInitialMusicTracks != null)
		{
			foreach (AudioClip track in defaultInitialMusicTracks)
			{
				if (track != null && !string.IsNullOrEmpty(track.name) && !newSave.unlockedMusicTrackNames.Contains(track.name))
				{
					newSave.unlockedMusicTrackNames.Add(track.name);
				}
			}
		}
		else Debug.LogWarning("GameDataManager: 'defaultInitialMusicTracks' list not assigned in Inspector. New game might miss initial music.");
		pendingDataToApply = newSave;
		hasPendingDataToApply = true;
		SaveCurrentDataToSlot(newSave);
		Debug.Log($"GameDataManager: New game prepared for slot {CurrentSaveSlot}. Initial data created and saved. Will apply after scene '{gameSceneName}' loads.");
	}

	public bool PrepareLoadGameFromSlot(int slotNumber, string gameSceneName)
	{
		SaveData loadedData = SaveSystem.LoadGame(slotNumber);
		if (loadedData != null)
		{
			CurrentSaveSlot = slotNumber;
			sceneToLoadAfterDataPrep = gameSceneName;
			pendingDataToApply = loadedData;
			hasPendingDataToApply = true;
			Debug.Log($"GameDataManager: Game data prepared for load from slot {CurrentSaveSlot}. Will apply after scene '{gameSceneName}' loads.");
			return true;
		}
		Debug.LogError($"GameDataManager: Failed to prepare load game from slot {slotNumber}.");
		return false;
	}

	private void ApplySaveDataToGameInternal(SaveData data)
	{
		if (data == null)
		{
			Debug.LogError("GameDataManager: ApplySaveDataToGameInternal called with null data!");
			return;
		}

		TotalDishesCompleted = data.totalDishesCompleted;
		UnlockedRecipeNames = new List<string>(data.unlockedRecipeNames ?? new List<string>());
		UnlockedMusicTrackNames = new List<string>(data.unlockedMusicTrackNames ?? new List<string>());

		Debug.Log($"Applying Save Data: Dishes={TotalDishesCompleted}, Recipes={UnlockedRecipeNames.Count}, Music={UnlockedMusicTrackNames.Count}");

		if (rewardManager != null)
		{
			rewardManager.SetTotalDishesCompletedFromLoad(TotalDishesCompleted);
		}
		else Debug.LogWarning("GameDataManager: RewardManager instance not found during ApplySaveDataToGameInternal.");

		if (orderManager != null)
		{
			orderManager.ApplyUnlockedRecipesFromLoad(UnlockedRecipeNames);
		}
		else Debug.LogWarning("GameDataManager: OrderManager instance not found during ApplySaveDataToGameInternal.");

		if (musicPlayer != null)
		{
			musicPlayer.ApplyUnlockedMusicFromLoad(UnlockedMusicTrackNames);
		}
		else Debug.LogWarning("GameDataManager: BackgroundMusicPlayer instance not found during ApplySaveDataToGameInternal.");
	}

	public void SaveActiveGameState()
	{
		if (CurrentSaveSlot == -1)
		{
			Debug.LogWarning("GameDataManager: No active save slot. Cannot save game.");
			return;
		}
		SaveData currentData = new SaveData
		{
			totalDishesCompleted = this.TotalDishesCompleted,
			unlockedRecipeNames = new List<string>(this.UnlockedRecipeNames),
			unlockedMusicTrackNames = new List<string>(this.UnlockedMusicTrackNames)
		};
		SaveCurrentDataToSlot(currentData);
	}

	private void SaveCurrentDataToSlot(SaveData dataToSave)
	{
		if (CurrentSaveSlot == -1) return;
		SaveSystem.SaveGame(dataToSave, CurrentSaveSlot);
	}

	public void UpdateTotalDishesCompleted(int count)
	{
		TotalDishesCompleted = count;
	}

	public void IncrementDishesAndUpdate()
	{
		TotalDishesCompleted++;
		Debug.Log($"GameDataManager: TotalDishesCompleted incremented to {TotalDishesCompleted}");
	}

	public void AddUnlockedRecipe(string recipeName)
	{
		if (!string.IsNullOrEmpty(recipeName) && !UnlockedRecipeNames.Contains(recipeName))
		{
			UnlockedRecipeNames.Add(recipeName);
			Debug.Log($"GameDataManager: Added unlocked recipe to session: {recipeName}");
		}
	}

	public void AddUnlockedMusicTrack(string trackName)
	{
		if (!string.IsNullOrEmpty(trackName) && !UnlockedMusicTrackNames.Contains(trackName))
		{
			UnlockedMusicTrackNames.Add(trackName);
			Debug.Log($"GameDataManager: Added unlocked music track to session: {trackName}");
		}
	}

	void OnApplicationQuit()
	{
		Debug.Log("GameDataManager: OnApplicationQuit() called. Saving game state if a slot is active.");
		if (CurrentSaveSlot != -1)
		{
			SaveActiveGameState();
		}
	}
}