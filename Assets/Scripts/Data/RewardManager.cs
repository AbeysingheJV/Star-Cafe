using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Added for .Select in debug log
using TMPro;

// Defines the types of rewards available.
public enum RewardType
{
	CatFact,
	MusicTrackUnlock,
	NewRecipeUnlock
}

// Defines the structure for a milestone reward.
[System.Serializable]
public class MilestoneReward
{
	public string milestoneName = "New Milestone"; // Name of the milestone.
	public int dishesRequired = 1; // Number of dishes needed to achieve this milestone.
	public RewardType rewardType = RewardType.CatFact; // Type of reward for this milestone.
	public string rewardValue = ""; // Value of the reward (e.g., cat fact text, asset name for music/recipe).
}

public class RewardManager : MonoBehaviour
{
	public static RewardManager Instance { get; private set; }

	[Header("Milestone Rewards Configuration")]
	[SerializeField] private List<MilestoneReward> milestoneRewards = new List<MilestoneReward>(); // List of all milestone rewards.

	[Header("Reward Notification UI (Assign in Inspector)")]
	[SerializeField] private GameObject rewardNotificationPanel; // UI panel for reward notifications.
	[SerializeField] private TextMeshProUGUI autoRewardNotificationText; // Text element for reward messages.
	[SerializeField] private float notificationDisplayDuration = 5f; // How long notifications stay on screen.

	[Header("Resource Pools for Unlocks (Assign in Inspector)")]
	public List<RecipeData> allUnlockableRecipesPool; // Master list of all recipes that can be unlocked.
	public List<AudioClip> allUnlockableMusicTracksPool; // Master list of all music tracks that can be unlocked.

	private const string MilestoneAwardedKeyPrefix = "MilestoneAwarded_"; // Prefix for PlayerPrefs keys.
	private Coroutine activeNotificationCoroutine; // Stores the active notification coroutine.

	// Called when the script instance is being loaded.
	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;

		if (milestoneRewards != null)
		{
			milestoneRewards.Sort((r1, r2) => r1.dishesRequired.CompareTo(r2.dishesRequired));
		}
		else
		{
			Debug.LogWarning("RewardManager: MilestoneRewards list is not initialized!");
			milestoneRewards = new List<MilestoneReward>();
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

		if (allUnlockableRecipesPool == null)
		{
			Debug.LogWarning("RewardManager: allUnlockableRecipesPool is not assigned in Inspector! Recipe unlocking might fail to find assets.");
			allUnlockableRecipesPool = new List<RecipeData>();
		}
		if (allUnlockableMusicTracksPool == null)
		{
			Debug.LogWarning("RewardManager: allUnlockableMusicTracksPool is not assigned in Inspector! Music unlocking might fail to find assets.");
			allUnlockableMusicTracksPool = new List<AudioClip>();
		}
	}

	// Sets the total dishes completed count when a game is loaded.
	public void SetTotalDishesCompletedFromLoad(int count)
	{
		Debug.Log($"RewardManager: Total dishes count set to {count} from load by GameDataManager.");
		CheckForRewards(count, true);
	}

	// Called when a dish is completed to update progress and check for rewards.
	public void IncrementDishesCompleted()
	{
		if (GameDataManager.Instance == null)
		{
			Debug.LogError("RewardManager: GameDataManager.Instance is null. Cannot increment dishes.");
			return;
		}
		GameDataManager.Instance.IncrementDishesAndUpdate();
		int newTotalDishes = GameDataManager.Instance.TotalDishesCompleted;

		Debug.Log($"RewardManager: Dish completed! New total (from GDM): {newTotalDishes}");
		CheckForRewards(newTotalDishes, false);
	}

	// Checks if any milestone rewards have been achieved based on the dish count.
	private void CheckForRewards(int currentTotalDishes, bool isInitialLoadCheck)
	{
		if (milestoneRewards == null) return;

		foreach (MilestoneReward reward in milestoneRewards)
		{
			if (reward == null || string.IsNullOrEmpty(reward.milestoneName))
			{
				Debug.LogWarning("RewardManager: Encountered a null or unnamed milestone reward. Skipping.");
				continue;
			}

			if (currentTotalDishes >= reward.dishesRequired)
			{
				if (!IsMilestoneAwardedPlayerPrefs(reward))
				{
					if (!isInitialLoadCheck)
					{
						GrantRewardPopupAndLogic(reward);
					}
					MarkMilestoneAsAwardedPlayerPrefs(reward);
				}
			}
			else
			{
				break;
			}
		}
	}

	// Grants the specified reward, updates game data, and triggers UI notifications.
	private void GrantRewardPopupAndLogic(MilestoneReward reward)
	{
		Debug.Log($"Attempting to grant reward for milestone: '{reward.milestoneName}'. Type: {reward.rewardType}, Value: '{reward.rewardValue}'");
		string notificationMessage = "New Reward Unlocked!";

		switch (reward.rewardType)
		{
			case RewardType.CatFact:
				notificationMessage = "New Cat Fact Unlocked!";
				Debug.Log($"[RewardManager] CatFact: '{reward.rewardValue}'");
				break;

			case RewardType.MusicTrackUnlock:
				notificationMessage = "New Music Track Unlocked!";
				Debug.Log($"[RewardManager] Music unlock: Reward Value is '{reward.rewardValue}'");
				if (GameDataManager.Instance != null) GameDataManager.Instance.AddUnlockedMusicTrack(reward.rewardValue);

				if (BackgroundMusicPlayer.Instance != null) BackgroundMusicPlayer.Instance.UnlockAndPlayTrackByName(reward.rewardValue);
				else Debug.LogWarning($"BackgroundMusicPlayer.Instance not found while trying to unlock track: {reward.rewardValue}");
				break;

			case RewardType.NewRecipeUnlock:
				notificationMessage = "New Recipe Unlocked!";
				Debug.Log($"[RewardManager] Recipe unlock: Reward Value is '{reward.rewardValue}'");
				if (GameDataManager.Instance != null) GameDataManager.Instance.AddUnlockedRecipe(reward.rewardValue);

				if (OrderManager.Instance != null && allUnlockableRecipesPool != null)
				{
					Debug.Log($"[RewardManager] Searching for recipe '{reward.rewardValue}' in allUnlockableRecipesPool (count: {allUnlockableRecipesPool.Count})");
					if (allUnlockableRecipesPool.Count > 0)
					{
						Debug.Log($"[RewardTest] Searching in allUnlockableRecipesPool. Available names: {string.Join(", ", allUnlockableRecipesPool.Where(r => r != null).Select(r => "'" + r.name + "'"))}");
					}

					RecipeData recipeToUnlock = allUnlockableRecipesPool.Find(r => r != null && r.name == reward.rewardValue);
					if (recipeToUnlock != null)
					{
						Debug.Log($"[RewardManager] Found recipe: {recipeToUnlock.name}. Unlocking via OrderManager.");
						OrderManager.Instance.UnlockNewRecipe(recipeToUnlock);
					}
					else Debug.LogWarning($"[RewardManager] Recipe asset named '{reward.rewardValue}' NOT FOUND in allUnlockableRecipesPool.");
				}
				else Debug.LogWarning($"OrderManager.Instance or allUnlockableRecipesPool not set/available while trying to unlock recipe: {reward.rewardValue}");
				break;
		}
		ShowAutoClosingRewardNotification(notificationMessage);
	}

	// Shows a reward notification message on the UI.
	private void ShowAutoClosingRewardNotification(string message)
	{
		if (rewardNotificationPanel == null || autoRewardNotificationText == null)
		{
			Debug.LogError("RewardManager: RewardNotificationPanel or AutoRewardNotificationText is not assigned. Cannot show notification.");
			return;
		}
		if (activeNotificationCoroutine != null) StopCoroutine(activeNotificationCoroutine);

		autoRewardNotificationText.text = message;
		rewardNotificationPanel.SetActive(true);

		activeNotificationCoroutine = StartCoroutine(HideRewardNotificationAfterDelay());
	}

	// Coroutine to hide the reward notification panel after a delay.
	private IEnumerator HideRewardNotificationAfterDelay()
	{
		yield return new WaitForSeconds(notificationDisplayDuration);
		if (rewardNotificationPanel != null)
		{
			rewardNotificationPanel.SetActive(false);
		}
		activeNotificationCoroutine = null;
	}

	// Checks PlayerPrefs to see if a milestone has already been awarded.
	private bool IsMilestoneAwardedPlayerPrefs(MilestoneReward reward)
	{
		if (reward == null || string.IsNullOrEmpty(reward.milestoneName))
		{
			Debug.LogWarning("IsMilestoneAwardedPlayerPrefs called with null or unnamed reward.");
			return true;
		}
		return PlayerPrefs.GetInt(MilestoneAwardedKeyPrefix + reward.milestoneName.Replace(" ", "_"), 0) == 1;
	}

	// Marks a milestone as awarded in PlayerPrefs.
	private void MarkMilestoneAsAwardedPlayerPrefs(MilestoneReward reward)
	{
		if (reward == null || string.IsNullOrEmpty(reward.milestoneName)) return;
		PlayerPrefs.SetInt(MilestoneAwardedKeyPrefix + reward.milestoneName.Replace(" ", "_"), 1);
		PlayerPrefs.Save();
	}

	// Editor-only function to reset all milestone awarded statuses in PlayerPrefs.
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