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
	// MAKE THESE PUBLIC so OrderManager and BackgroundMusicPlayer can access them (via GameDataManager if needed)
	public List<RecipeData> allUnlockableRecipesPool;
	public List<AudioClip> allUnlockableMusicTracksPool;

	private const string MilestoneAwardedKeyPrefix = "MilestoneAwarded_"; // PlayerPrefs key for individual milestone awarded status
	private Coroutine activeNotificationCoroutine;

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

		if (rewardNotificationPanel != null) rewardNotificationPanel.SetActive(false);
		else Debug.LogError("RewardManager: RewardNotificationPanel not assigned in Inspector!");

		if (autoRewardNotificationText == null) Debug.LogWarning("RewardManager: AutoRewardNotificationText not assigned.");

		if (allUnlockableRecipesPool == null) Debug.LogWarning("RewardManager: allUnlockableRecipesPool is not assigned!");
		if (allUnlockableMusicTracksPool == null) Debug.LogWarning("RewardManager: allUnlockableMusicTracksPool is not assigned!");
	}

	public void SetTotalDishesCompletedFromLoad(int count)
	{
		Debug.Log($"RewardManager: Total dishes count set to {count} from load by GameDataManager.");
		CheckForRewards(count, true);
	}

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

	private void CheckForRewards(int currentTotalDishes, bool isInitialLoadCheck)
	{
		if (milestoneRewards == null) return;
		foreach (MilestoneReward reward in milestoneRewards)
		{
			if (reward == null || string.IsNullOrEmpty(reward.milestoneName)) continue; // Skip invalid rewards

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

	private void GrantRewardPopupAndLogic(MilestoneReward reward)
	{
		Debug.Log($"RewardManager: Granting reward & POPUP for milestone: '{reward.milestoneName}'. Type: {reward.rewardType}, Value: '{reward.rewardValue}'");
		string notificationMessage = "New Reward Unlocked!";

		switch (reward.rewardType)
		{
			case RewardType.CatFact:
				notificationMessage = "New Cat Fact Unlocked!";
				if (GameDataManager.Instance != null) { /* GameDataManager.Instance.AddCollectedCatFact(reward.rewardValue); */ }
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
				else Debug.LogWarning($"OrderManager.Instance or allUnlockableRecipesPool not set while trying to unlock recipe: {reward.rewardValue}");
				break;
		}
		ShowAutoClosingRewardNotification(notificationMessage);
	}

	private void ShowAutoClosingRewardNotification(string message)
	{
		if (rewardNotificationPanel == null || autoRewardNotificationText == null) return;
		if (activeNotificationCoroutine != null) StopCoroutine(activeNotificationCoroutine);
		autoRewardNotificationText.text = message;
		rewardNotificationPanel.SetActive(true);
		activeNotificationCoroutine = StartCoroutine(HideRewardNotificationAfterDelay());
	}

	private IEnumerator HideRewardNotificationAfterDelay()
	{
		yield return new WaitForSeconds(notificationDisplayDuration);
		if (rewardNotificationPanel != null) rewardNotificationPanel.SetActive(false);
		activeNotificationCoroutine = null;
	}

	private bool IsMilestoneAwardedPlayerPrefs(MilestoneReward reward)
	{
		if (reward == null || string.IsNullOrEmpty(reward.milestoneName)) return true;
		return PlayerPrefs.GetInt(MilestoneAwardedKeyPrefix + reward.milestoneName.Replace(" ", "_"), 0) == 1;
	}

	private void MarkMilestoneAsAwardedPlayerPrefs(MilestoneReward reward)
	{
		if (reward == null || string.IsNullOrEmpty(reward.milestoneName)) return;
		PlayerPrefs.SetInt(MilestoneAwardedKeyPrefix + reward.milestoneName.Replace(" ", "_"), 1);
		PlayerPrefs.Save();
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