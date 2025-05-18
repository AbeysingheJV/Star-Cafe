using UnityEngine;
using System.Collections.Generic; // For List
using System.Linq; // For Linq operations like .ToList()
using System; // For DateTime

public class GameDataManager : MonoBehaviour
{
	public static GameDataManager Instance { get; private set; }

	public static int CurrentSaveSlot { get; private set; } = -1;

	// Current game session data
	public int TotalDishesCompleted { get; private set; }
	public List<string> UnlockedRecipeNames { get; private set; } = new List<string>();
	public List<string> UnlockedMusicTrackNames { get; private set; } = new List<string>();
	// Add list for awarded milestone names if you want to save that centrally
	// public List<string> AwardedMilestoneNames { get; private set; } = new List<string>();


	[Header("System References (Optional - for direct application)")]
	[SerializeField] private OrderManager orderManager;
	[SerializeField] private BackgroundMusicPlayer musicPlayer;
	[SerializeField] private RewardManager rewardManager;

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
	}

	public void StartNewGame(int slotNumber)
	{
		CurrentSaveSlot = slotNumber;
		SaveData newSave = new SaveData(); // Creates data with default values (0 dishes, empty lists)

		// If you have initial recipes/music that should always be available for a new game,
		// populate them here before applying and saving.
		// Example:
		// if (OrderManager.Instance != null && OrderManager.Instance.initialPossibleOrders != null)
		// {
		//     foreach (RecipeData recipe in OrderManager.Instance.initialPossibleOrders)
		//     {
		//         if (recipe != null && !newSave.unlockedRecipeNames.Contains(recipe.name))
		//         {
		//             newSave.unlockedRecipeNames.Add(recipe.name);
		//         }
		//     }
		// }
		// Similarly for initial music tracks if BackgroundMusicPlayer has an initial list.


		ApplySaveDataToGame(newSave);
		SaveCurrentGameState(); // Save this new game state immediately
		Debug.Log($"GameDataManager: New game started in slot {CurrentSaveSlot}. Initial save created.");
	}

	public bool LoadGameFromSlot(int slotNumber)
	{
		SaveData loadedData = SaveSystem.LoadGame(slotNumber);
		if (loadedData != null)
		{
			CurrentSaveSlot = slotNumber;
			ApplySaveDataToGame(loadedData);
			Debug.Log($"GameDataManager: Game loaded from slot {CurrentSaveSlot}.");
			return true;
		}
		Debug.LogError($"GameDataManager: Failed to load game from slot {slotNumber}.");
		return false;
	}

	public void ApplySaveDataToGame(SaveData data)
	{
		if (data == null)
		{
			Debug.LogError("GameDataManager: ApplySaveDataToGame called with null data! Starting fresh for this session.");
			data = new SaveData(); // Fallback to new save data for current session
		}

		TotalDishesCompleted = data.totalDishesCompleted;
		UnlockedRecipeNames = new List<string>(data.unlockedRecipeNames ?? new List<string>());
		UnlockedMusicTrackNames = new List<string>(data.unlockedMusicTrackNames ?? new List<string>());
		// AwardedMilestoneNames = new List<string>(data.awardedMilestoneNames ?? new List<string>());


		Debug.Log($"Applying Save Data: Dishes={TotalDishesCompleted}, Recipes={UnlockedRecipeNames.Count}, Music={UnlockedMusicTrackNames.Count}");

		// Ensure instances are found if not assigned in Inspector
		if (rewardManager == null) rewardManager = RewardManager.Instance;
		if (orderManager == null) orderManager = OrderManager.Instance;
		if (musicPlayer == null) musicPlayer = BackgroundMusicPlayer.Instance;

		if (rewardManager != null)
		{
			rewardManager.SetTotalDishesCompletedFromLoad(TotalDishesCompleted);
			// If storing milestone status in SaveData:
			// rewardManager.ApplyAwardedMilestonesFromLoad(AwardedMilestoneNames);
		}
		else Debug.LogWarning("GameDataManager: RewardManager instance not found during ApplySaveDataToGame.");

		if (orderManager != null)
		{
			orderManager.ApplyUnlockedRecipesFromLoad(UnlockedRecipeNames);
		}
		else Debug.LogWarning("GameDataManager: OrderManager instance not found during ApplySaveDataToGame.");


		if (musicPlayer != null)
		{
			musicPlayer.ApplyUnlockedMusicFromLoad(UnlockedMusicTrackNames); // This method needs to be added to BackgroundMusicPlayer
		}
		else Debug.LogWarning("GameDataManager: BackgroundMusicPlayer instance not found during ApplySaveDataToGame.");
	}

	public void SaveCurrentGameState()
	{
		if (CurrentSaveSlot == -1)
		{
			Debug.LogWarning("GameDataManager: No active save slot. Cannot save game. Call StartNewGame or LoadGameFromSlot first.");
			return;
		}

		SaveData currentData = new SaveData
		{
			totalDishesCompleted = this.TotalDishesCompleted,
			unlockedRecipeNames = new List<string>(this.UnlockedRecipeNames),
			unlockedMusicTrackNames = new List<string>(this.UnlockedMusicTrackNames)
			// awardedMilestoneNames = new List<string>(this.AwardedMilestoneNames)
		};

		SaveSystem.SaveGame(currentData, CurrentSaveSlot);
	}

	// --- Methods to be called by other systems to update game data ---
	public void UpdateTotalDishesCompleted(int count) // Called by RewardManager
	{
		TotalDishesCompleted = count;
		Debug.Log($"GameDataManager: TotalDishesUpdated to {TotalDishesCompleted}");
		// Consider auto-saving here or at specific checkpoints, e.g., SaveCurrentGameState();
	}

	// NEW METHOD that RewardManager will call
	public void IncrementDishesAndUpdate()
	{
		TotalDishesCompleted++;
		Debug.Log($"GameDataManager: TotalDishesCompleted incremented to {TotalDishesCompleted}");
		// Optionally save after every dish, or batch saves
		// SaveCurrentGameState(); // Uncomment if you want to save after every dish
	}

	public void AddUnlockedRecipe(string recipeName) // Called by RewardManager
	{
		if (!UnlockedRecipeNames.Contains(recipeName))
		{
			UnlockedRecipeNames.Add(recipeName);
			Debug.Log($"GameDataManager: Added unlocked recipe: {recipeName}");
			// SaveCurrentGameState(); // Optional: save after every unlock
		}
	}

	public void AddUnlockedMusicTrack(string trackName) // Called by RewardManager
	{
		if (!UnlockedMusicTrackNames.Contains(trackName))
		{
			UnlockedMusicTrackNames.Add(trackName);
			Debug.Log($"GameDataManager: Added unlocked music track: {trackName}");
			// SaveCurrentGameState(); // Optional: save after every unlock
		}
	}

	// Example if you move milestone tracking here
	// public void MarkMilestoneAsAwardedInSaveData(string milestoneName)
	// {
	//     if (!AwardedMilestoneNames.Contains(milestoneName))
	//     {
	//         AwardedMilestoneNames.Add(milestoneName);
	//         Debug.Log($"GameDataManager: Marked milestone '{milestoneName}' as awarded in save data.");
	//     }
	// }
}