using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections; // For cat facts later, can be removed if not used

// Define what kind of rewards we can have
public enum RewardType
{
	CatFact, // We'll add UI for this later if you want
	MusicTrackUnlock,
	NewRecipeUnlock // We'll add logic for this later if you want
}

// Structure to define each reward milestone
[System.Serializable]
public class MilestoneReward
{
	public string milestoneName; // e.g., "5th Dish - New Song"
	public int dishesRequired;
	public RewardType rewardType;
	public string rewardValue; // For Music: Name of the AudioClip asset. For CatFact: The fact itself. For Recipe: Name of RecipeData asset.
							   // We'll use PlayerPrefs to track if it's awarded, no need for a visible bool here
}

public class RewardManager : MonoBehaviour
{
	public static RewardManager Instance { get; private set; }

	[Header("Milestone Rewards Configuration")]
	[SerializeField] private List<MilestoneReward> milestoneRewards = new List<MilestoneReward>();

	// --- Fields for other reward types (we'll use them later) ---
	[Header("Cat Fact Display (Optional - Assign if using CatFact rewards)")]
	[SerializeField] private GameObject catFactPanel;
	[SerializeField] private TextMeshProUGUI catFactText;
	[SerializeField] private float catFactDisplayDuration = 5f;

	[Header("Recipe Unlock Pool (Optional - Assign if using Recipe rewards)")]
	[SerializeField] private List<RecipeData> allUnlockableRecipesPool;

	// --- Player Progress Tracking ---
	private const string TotalDishesCompletedKey = "TotalDishesCompleted";
	private const string MilestoneAwardedKeyPrefix = "MilestoneAwarded_"; // e.g., MilestoneAwarded_5thDishNewSong
	private int totalDishesCompleted;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject); // Make sure rewards persist across scene loads

		LoadProgress();

		// Sort rewards by dishes required to process them in order
		milestoneRewards.Sort((r1, r2) => r1.dishesRequired.CompareTo(r2.dishesRequired));

		if (catFactPanel != null) catFactPanel.SetActive(false);
	}

	private void LoadProgress()
	{
		totalDishesCompleted = PlayerPrefs.GetInt(TotalDishesCompletedKey, 0);
		Debug.Log($"Loaded Total Dishes Completed: {totalDishesCompleted}");
	}

	private void SaveTotalDishes()
	{
		PlayerPrefs.SetInt(TotalDishesCompletedKey, totalDishesCompleted);
		PlayerPrefs.Save(); // Ensure it's written to disk
	}

	private bool IsMilestoneAwarded(MilestoneReward reward)
	{
		// Use a consistent key format. Replace spaces in milestoneName for safety.
		return PlayerPrefs.GetInt(MilestoneAwardedKeyPrefix + reward.milestoneName.Replace(" ", "_"), 0) == 1;
	}

	private void MarkMilestoneAsAwarded(MilestoneReward reward)
	{
		PlayerPrefs.SetInt(MilestoneAwardedKeyPrefix + reward.milestoneName.Replace(" ", "_"), 1);
		PlayerPrefs.Save(); // Ensure it's written to disk
	}

	public void IncrementDishesCompleted()
	{
		totalDishesCompleted++;
		Debug.Log($"Dish completed! Total dishes now: {totalDishesCompleted}");
		SaveTotalDishes(); // Save immediately
		CheckForRewards();
	}

	private void CheckForRewards()
	{
		Debug.Log("Checking for rewards...");
		foreach (MilestoneReward reward in milestoneRewards)
		{
			if (totalDishesCompleted >= reward.dishesRequired)
			{
				if (!IsMilestoneAwarded(reward))
				{
					GrantReward(reward);
					MarkMilestoneAsAwarded(reward);
				}
				else
				{
					// Debug.Log($"Milestone '{reward.milestoneName}' already awarded.");
				}
			}
			else
			{
				// Stop checking further milestones if this one isn't met (since they are sorted)
				// Debug.Log($"Milestone '{reward.milestoneName}' not yet reached ({totalDishesCompleted}/{reward.dishesRequired}).");
				break;
			}
		}
	}

	private void GrantReward(MilestoneReward reward)
	{
		Debug.Log($"Granting reward for milestone: '{reward.milestoneName}' ({reward.dishesRequired} dishes). Type: {reward.rewardType}, Value: '{reward.rewardValue}'");

		switch (reward.rewardType)
		{
			case RewardType.CatFact:
				if (catFactPanel != null && catFactText != null)
				{
					DisplayCatFact(reward.rewardValue);
				}
				else Debug.LogWarning("CatFactPanel or CatFactText not assigned in RewardManager to display CatFact.");
				break;

			case RewardType.MusicTrackUnlock:
				if (BackgroundMusicPlayer.Instance != null)
				{
					// The rewardValue should be the NAME of the AudioClip asset
					BackgroundMusicPlayer.Instance.UnlockAndPlayTrackByName(reward.rewardValue);
				}
				else Debug.LogWarning("BackgroundMusicPlayer instance not found. Cannot unlock music track.");
				break;

			case RewardType.NewRecipeUnlock:
				if (OrderManager.Instance != null && allUnlockableRecipesPool != null)
				{
					RecipeData recipeToUnlock = allUnlockableRecipesPool.Find(r => r.name == reward.rewardValue);
					if (recipeToUnlock != null)
					{
						OrderManager.Instance.UnlockNewRecipe(recipeToUnlock);
					}
					else Debug.LogWarning($"Recipe '{reward.rewardValue}' not found in allUnlockableRecipesPool.");
				}
				else Debug.LogWarning("OrderManager instance or allUnlockableRecipesPool not set. Cannot unlock recipe.");
				break;
		}
	}

	private void DisplayCatFact(string fact)
	{
		catFactText.text = fact;
		catFactPanel.SetActive(true);
		StartCoroutine(HideCatFactPanelAfterDelay(catFactDisplayDuration));
	}

	private IEnumerator HideCatFactPanelAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		if (catFactPanel != null) catFactPanel.SetActive(false);
	}

	[ContextMenu("Reset All Reward Progress (PlayerPrefs)")]
	public void ResetAllRewardProgress()
	{
		PlayerPrefs.DeleteKey(TotalDishesCompletedKey);
		foreach (MilestoneReward reward in milestoneRewards)
		{
			PlayerPrefs.DeleteKey(MilestoneAwardedKeyPrefix + reward.milestoneName.Replace(" ", "_"));
		}
		PlayerPrefs.Save();
		LoadProgress(); // Reload to reflect changes in the editor state
		Debug.Log("All reward progress (PlayerPrefs) has been reset. Restart game to see full effect if music was already added to list.");
	}
}