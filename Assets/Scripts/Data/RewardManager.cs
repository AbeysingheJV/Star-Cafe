using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// Define RewardType enum here so it's accessible
public enum RewardType
{
	CatFact,
	MusicTrackUnlock,
	NewRecipeUnlock
}

// Define MilestoneReward class here so it's accessible
[System.Serializable]
public class MilestoneReward
{
	public string milestoneName = "New Milestone"; // Default name
	public int dishesRequired = 1;
	public RewardType rewardType = RewardType.CatFact;
	public string rewardValue = ""; // For CatFact: The fact itself. For Music/Recipe: Asset name.
}

public class RewardManager : MonoBehaviour
{
	public static RewardManager Instance { get; private set; }

	[Header("Milestone Rewards Configuration")]
	[SerializeField] private List<MilestoneReward> milestoneRewards = new List<MilestoneReward>();

	[Header("Reward Notification UI (Assign in Inspector)")]
	[SerializeField] private GameObject rewardNotificationPanel;
	[SerializeField] private TextMeshProUGUI autoRewardNotificationText;
	[SerializeField] private float notificationDisplayDuration = 5f;

	[Header("Resource Pools for Unlocks (Assign in Inspector)")]
	// These are public so other managers (like OrderManager via GameDataManager) can reference them
	// to resolve asset names to actual assets if needed.
	public List<RecipeData> allUnlockableRecipesPool;
	public List<AudioClip> allUnlockableMusicTracksPool;

	// PlayerPrefs key for individual milestone awarded status (tracks if pop-up was shown)
	private const string MilestoneAwardedKeyPrefix = "MilestoneAwarded_";
	private Coroutine activeNotificationCoroutine;

	// TotalDishesCompleted is now primarily managed by GameDataManager

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		// DontDestroyOnLoad(gameObject); // GameDataManager is DDOL. RewardManager can be scene-specific.
		// If it needs to persist, ensure its references are handled across scenes.

		if (milestoneRewards != null)
		{
			milestoneRewards.Sort((r1, r2) => r1.dishesRequired.CompareTo(r2.dishesRequired));
		}
		else
		{
			Debug.LogWarning("RewardManager: MilestoneRewards list is not initialized!");
			milestoneRewards = new List<MilestoneReward>(); // Initialize to prevent null errors
		}


		if (rewardNotificationPanel != null)
		{
			rewardNotificationPanel.SetActive(false);
		}
		else Debug.LogError("RewardManager: RewardNotificationPanel not assigned in Inspector!");

		if (autoRewardNotificationText == null)
		{
			Debug.LogWarning("RewardManager: AutoRewardNotificationText not assigned in Inspector.");
		}

		// Check if pools are assigned, as other scripts might rely on them
		if (allUnlockableRecipesPool == null)
		{
			Debug.LogWarning("RewardManager: allUnlockableRecipesPool is not assigned in Inspector! Recipe unlocking might fail to find assets.");
			allUnlockableRecipesPool = new List<RecipeData>(); // Initialize to prevent null errors
		}
		if (allUnlockableMusicTracksPool == null)
		{
			Debug.LogWarning("RewardManager: allUnlockableMusicTracksPool is not assigned in Inspector! Music unlocking might fail to find assets.");
			allUnlockableMusicTracksPool = new List<AudioClip>(); // Initialize
		}
	}

	// Called by GameDataManager when a game is loaded or a new game starts
	// to set the initial state based on loaded data.
	public void SetTotalDishesCompletedFromLoad(int count)
	{
		Debug.Log($"RewardManager: Total dishes count set to {count} from load by GameDataManager.");
		// Check for any milestones that should have been awarded based on this count
		// but whose PlayerPrefs flag might have been cleared or not set.
		CheckForRewards(count, true); // 'true' indicates it's an initial load check (don't re-show popups)
	}

	// Called by OrderManager when a dish is successfully completed
	public void IncrementDishesCompleted()
	{
		if (GameDataManager.Instance == null)
		{
			Debug.LogError("RewardManager: GameDataManager.Instance is null. Cannot increment dishes.");
			return;
		}

		// GameDataManager holds the authoritative count.
		// Tell it to increment, then get the new count.
		GameDataManager.Instance.IncrementDishesAndUpdate();
		int newTotalDishes = GameDataManager.Instance.TotalDishesCompleted;

		Debug.Log($"RewardManager: Dish completed! New total (from GDM): {newTotalDishes}");
		CheckForRewards(newTotalDishes, false); // 'false' means it's a new event, show pop-up if applicable
	}

	// Checks for rewards based on the current dish count
	private void CheckForRewards(int currentTotalDishes, bool isInitialLoadCheck)
	{
		if (milestoneRewards == null) return;

		foreach (MilestoneReward reward in milestoneRewards)
		{
			// Basic null check for safety, though list elements should ideally always be valid
			if (reward == null || string.IsNullOrEmpty(reward.milestoneName))
			{
				Debug.LogWarning("RewardManager: Encountered a null or unnamed milestone reward. Skipping.");
				continue;
			}

			if (currentTotalDishes >= reward.dishesRequired)
			{
				if (!IsMilestoneAwardedPlayerPrefs(reward)) // Check if this specific milestone's pop-up was shown
				{
					if (!isInitialLoadCheck) // Only grant pop-up and logic for new achievements, not on initial load for already passed milestones
					{
						GrantRewardPopupAndLogic(reward);
					}
					MarkMilestoneAsAwardedPlayerPrefs(reward); // Mark this milestone's pop-up as shown

					// GameDataManager should be informed about the actual unlock (recipe/music name)
					// This is already handled inside GrantRewardPopupAndLogic
				}
			}
			else
			{
				// Since milestones are sorted by dishesRequired, no need to check further
				break;
			}
		}
	}

	// Grants the reward (unlocks item, shows pop-up)
	private void GrantRewardPopupAndLogic(MilestoneReward reward)
	{
		Debug.Log($"RewardManager: Granting reward & POPUP for milestone: '{reward.milestoneName}'. Type: {reward.rewardType}, Value: '{reward.rewardValue}'");
		string notificationMessage = "New Reward Unlocked!"; // Default

		switch (reward.rewardType)
		{
			case RewardType.CatFact:
				notificationMessage = "New Cat Fact Unlocked!";
				// Actual cat fact (reward.rewardValue) isn't displayed in this simple pop-up.
				// GameDataManager could store it if SaveData is expanded:
				// if (GameDataManager.Instance != null) { GameDataManager.Instance.AddCollectedCatFact(reward.rewardValue); }
				break;

			case RewardType.MusicTrackUnlock:
				notificationMessage = "New Music Track Unlocked!";
				if (GameDataManager.Instance != null) GameDataManager.Instance.AddUnlockedMusicTrack(reward.rewardValue);

				if (BackgroundMusicPlayer.Instance != null) BackgroundMusicPlayer.Instance.UnlockAndPlayTrackByName(reward.rewardValue);
				else Debug.LogWarning($"BackgroundMusicPlayer.Instance not found while trying to unlock track: {reward.rewardValue}");
				break;

			case RewardType.NewRecipeUnlock:
				notificationMessage = "New Recipe Unlocked!";
				if (GameDataManager.Instance != null) GameDataManager.Instance.AddUnlockedRecipe(reward.rewardValue);

				if (OrderManager.Instance != null && allUnlockableRecipesPool != null)
				{
					RecipeData recipeToUnlock = allUnlockableRecipesPool.Find(r => r != null && r.name == reward.rewardValue);
					if (recipeToUnlock != null) OrderManager.Instance.UnlockNewRecipe(recipeToUnlock);
					else Debug.LogWarning($"Recipe asset named '{reward.rewardValue}' not found in allUnlockableRecipesPool.");
				}
				else Debug.LogWarning($"OrderManager.Instance or allUnlockableRecipesPool not set/available while trying to unlock recipe: {reward.rewardValue}");
				break;
		}

		ShowAutoClosingRewardNotification(notificationMessage);
	}

	private void ShowAutoClosingRewardNotification(string message)
	{
		if (rewardNotificationPanel == null || autoRewardNotificationText == null)
		{
			Debug.LogError("RewardManager: RewardNotificationPanel or AutoRewardNotificationText is not assigned. Cannot show notification.");
			return;
		}
		if (activeNotificationCoroutine != null) StopCoroutine(activeNotificationCoroutine); // Stop previous one if any

		autoRewardNotificationText.text = message;
		rewardNotificationPanel.SetActive(true);

		activeNotificationCoroutine = StartCoroutine(HideRewardNotificationAfterDelay());
	}

	private IEnumerator HideRewardNotificationAfterDelay()
	{
		// Using WaitForSeconds because game time is NOT paused for this notification type
		yield return new WaitForSeconds(notificationDisplayDuration);

		if (rewardNotificationPanel != null)
		{
			rewardNotificationPanel.SetActive(false);
		}
		activeNotificationCoroutine = null;
	}

	// --- PlayerPrefs for individual milestone awarded status (tracks if pop-up was shown) ---
	private bool IsMilestoneAwardedPlayerPrefs(MilestoneReward reward)
	{
		// Added null check for reward itself
		if (reward == null || string.IsNullOrEmpty(reward.milestoneName))
		{
			Debug.LogWarning("IsMilestoneAwardedPlayerPrefs called with null or unnamed reward.");
			return true; // Treat as awarded to prevent errors
		}
		return PlayerPrefs.GetInt(MilestoneAwardedKeyPrefix + reward.milestoneName.Replace(" ", "_"), 0) == 1;
	}

	private void MarkMilestoneAsAwardedPlayerPrefs(MilestoneReward reward)
	{
		if (reward == null || string.IsNullOrEmpty(reward.milestoneName)) return;
		PlayerPrefs.SetInt(MilestoneAwardedKeyPrefix + reward.milestoneName.Replace(" ", "_"), 1);
		PlayerPrefs.Save(); // Save PlayerPrefs immediately after marking
	}

	[ContextMenu("Reset Milestone Award Status (PlayerPrefs)")]
	public void ResetMilestoneAwardStatusPlayerPrefs()
	{
		if (milestoneRewards == null) return;
		foreach (MilestoneReward reward in milestoneRewards)
		{
			if (reward != null && !string.IsNullOrEmpty(reward.milestoneName))
			{
				PlayerPrefs.DeleteKey(MilestoneAwardedKeyPrefix + reward.milestoneName.Replace(" ", "_"));
			}
		}
		PlayerPrefs.Save();
		Debug.Log("PlayerPrefs for individual milestone awarded status has been reset.");
	}
}
