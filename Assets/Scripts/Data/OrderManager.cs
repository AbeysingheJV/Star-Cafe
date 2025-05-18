using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For Linq operations like Contains
using TMPro;

public class OrderManager : MonoBehaviour
{
	public static OrderManager Instance { get; private set; }

	// Make this public so GameDataManager can access it for new game setup
	[Header("Order Settings")]
	public List<RecipeData> initialPossibleOrders; // Recipes available at the start of a brand new game
	[SerializeField] private bool randomOrder = true;

	[Header("UI")]
	[SerializeField] private TextMeshProUGUI currentOrderText;

	[Header("Audio")]
	[SerializeField] private AudioClip newOrderSound;
	[SerializeField] private AudioClip orderCompleteSound;

	[Header("State (Read Only)")]
	[SerializeField] private RecipeData currentOrder;
	private int currentSequentialIndex = -1;
	private bool noMoreOrdersAvailable = false;

	private AudioSource audioSource;
	private List<RecipeData> activeOrderPool = new List<RecipeData>();

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;

		audioSource = GetComponent<AudioSource>();
		if (audioSource == null) { Debug.LogWarning("OrderManager requires an AudioSource component for sounds!", this); }
	}

	void Start()
	{
		if (currentOrderText == null)
		{
			Debug.LogError("OrderManager: Current Order Text UI not assigned!");
		}

		// GameDataManager should call ApplyUnlockedRecipesFromLoad.
		// This Start() logic is a fallback if GameDataManager hasn't initialized the pool yet
		// (e.g., if running this scene directly in editor without the main menu flow).
		if (activeOrderPool.Count == 0)
		{
			if (initialPossibleOrders != null && initialPossibleOrders.Count > 0)
			{
				Debug.LogWarning("OrderManager: Active order pool empty at Start. Populating with initialPossibleOrders. GameDataManager should ideally manage this for new/loaded games.");
				// Convert RecipeData list to list of names for ApplyUnlockedRecipesFromLoad
				// This ensures consistency with how loaded save data is handled.
				List<string> initialRecipeNames = new List<string>();
				foreach (RecipeData recipe in initialPossibleOrders)
				{
					if (recipe != null && !string.IsNullOrEmpty(recipe.name))
					{
						initialRecipeNames.Add(recipe.name);
					}
				}
				// Call ApplyUnlockedRecipesFromLoad to process these initial names
				// This will use RewardManager's pool to get the actual RecipeData objects.
				ApplyUnlockedRecipesFromLoad(initialRecipeNames);
			}
			else
			{
				Debug.LogWarning("OrderManager: No recipes available at Start (initial list also empty or null).");
				UpdateOrderUI(); // To show "No Orders Available"
			}
		}
		// If activeOrderPool was populated by GDM's ApplyUnlockedRecipesFromLoad, 
		// GetNextOrder would have been called from there.
	}

	// Called by GameDataManager when loading a save or starting a new game
	public void ApplyUnlockedRecipesFromLoad(List<string> unlockedRecipeNames)
	{
		activeOrderPool.Clear();
		bool appliedAnyFromSave = false;
		bool appliedAnyFromInitial = false;

		// Attempt to populate from unlockedRecipeNames first (from save data)
		if (unlockedRecipeNames != null && unlockedRecipeNames.Count > 0)
		{
			if (RewardManager.Instance != null && RewardManager.Instance.allUnlockableRecipesPool != null)
			{
				foreach (string recipeName in unlockedRecipeNames)
				{
					if (string.IsNullOrEmpty(recipeName)) continue;
					// Find the RecipeData asset from RewardManager's master list
					RecipeData recipe = RewardManager.Instance.allUnlockableRecipesPool.Find(r => r != null && r.name == recipeName);
					if (recipe != null)
					{
						if (!activeOrderPool.Contains(recipe))
						{
							activeOrderPool.Add(recipe);
							appliedAnyFromSave = true;
						}
					}
					else
					{
						Debug.LogWarning($"OrderManager: Could not find RecipeData asset named '{recipeName}' in RewardManager's allUnlockableRecipesPool during load.");
					}
				}
				if (appliedAnyFromSave) Debug.Log($"OrderManager: Applied {activeOrderPool.Count} recipes from save data's unlocked list.");
			}
			else
			{
				Debug.LogError("OrderManager: RewardManager.Instance or its allUnlockableRecipesPool is not available to resolve recipe names from save data. Initial recipes might not be used if save data had names.");
				// Critical issue if this happens and unlockedRecipeNames was not empty.
			}
		}

		// If the pool is still empty (e.g., new game where unlockedRecipeNames was empty, or saved recipes couldn't be found)
		// OR if unlockedRecipeNames was null/empty to begin with, then use initialPossibleOrders.
		if (activeOrderPool.Count == 0 && initialPossibleOrders != null && initialPossibleOrders.Count > 0)
		{
			Debug.Log("OrderManager: Active pool empty after processing saved names (or no saved names given), using initialPossibleOrders.");
			foreach (RecipeData recipe in initialPossibleOrders)
			{
				if (recipe != null && !activeOrderPool.Contains(recipe))
				{
					activeOrderPool.Add(recipe);
					appliedAnyFromInitial = true;
				}
			}
			if (appliedAnyFromInitial && !(unlockedRecipeNames != null && unlockedRecipeNames.Count > 0))
				Debug.Log($"OrderManager: Populated with {activeOrderPool.Count} initial recipes.");
		}

		currentSequentialIndex = -1;
		if (activeOrderPool.Count > 0)
		{
			GetNextOrder();
		}
		else
		{
			noMoreOrdersAvailable = true;
			currentOrder = null;
			UpdateOrderUI();
		}
	}


	void GetNextOrder()
	{
		if (activeOrderPool.Count == 0)
		{
			noMoreOrdersAvailable = true;
			currentOrder = null;
			// Debug.Log("OrderManager: No recipes in active pool to choose from."); // Can be noisy
			UpdateOrderUI();
			return;
		}

		noMoreOrdersAvailable = false;

		if (randomOrder)
		{
			RecipeData previousOrder = currentOrder;
			if (activeOrderPool.Count > 1 && previousOrder != null)
			{
				int attempts = 0;
				do
				{
					currentOrder = activeOrderPool[Random.Range(0, activeOrderPool.Count)];
					attempts++;
				} while (currentOrder == previousOrder && attempts < activeOrderPool.Count * 2); // Avoid infinite loop if all remaining orders are the same
			}
			else if (activeOrderPool.Count > 0) // Ensure there's at least one to pick
			{
				currentOrder = activeOrderPool[Random.Range(0, activeOrderPool.Count)];
			}
			// else: activeOrderPool.Count is 0, caught by the initial check
		}
		else // Sequential 
		{
			if (activeOrderPool.Count > 0) // Ensure there's at least one to pick
			{
				currentSequentialIndex++;
				if (currentSequentialIndex >= activeOrderPool.Count)
				{
					currentSequentialIndex = 0; // Loop
				}
				currentOrder = activeOrderPool[currentSequentialIndex];
			}
			// else: activeOrderPool.Count is 0, caught by the initial check
		}

		UpdateOrderUI();

		if (audioSource != null && newOrderSound != null && currentOrder != null)
		{
			audioSource.PlayOneShot(newOrderSound);
		}
	}

	void UpdateOrderUI()
	{
		if (currentOrderText != null)
		{
			if (currentOrder != null)
			{
				currentOrderText.text = $"Order: {currentOrder.recipeName}";
			}
			else // Handles noMoreOrdersAvailable or empty activeOrderPool
			{
				currentOrderText.text = "No Orders Available";
			}
		}
	}

	public bool CheckOrderCompletion(GameObject submittedDish)
	{
		if (currentOrder == null || submittedDish == null) return false;
		return !string.IsNullOrEmpty(currentOrder.resultDishTag) && submittedDish.CompareTag(currentOrder.resultDishTag);
	}

	public void OrderCompleted()
	{
		if (currentOrder == null) return; // Should not happen if CheckOrderCompletion was true
		Debug.Log($"Order '{currentOrder.recipeName}' Completed!");

		if (audioSource != null && orderCompleteSound != null) audioSource.PlayOneShot(orderCompleteSound);

		// Notify RewardManager (which then notifies GameDataManager)
		if (RewardManager.Instance != null)
		{
			RewardManager.Instance.IncrementDishesCompleted();
		}

		if (CatAI.Instance != null) CatAI.Instance.TriggerMiauAndSound(); // If you have a cat
		GetNextOrder();
	}

	// Called by RewardManager (which is called by GameDataManager) to add a newly unlocked recipe
	// to the current session's active pool.
	public void UnlockNewRecipe(RecipeData newRecipe)
	{
		if (newRecipe != null && !activeOrderPool.Contains(newRecipe))
		{
			activeOrderPool.Add(newRecipe);
			Debug.Log($"OrderManager: New recipe '{newRecipe.recipeName}' added to active order pool for current session.");
			if (currentOrder == null || noMoreOrdersAvailable) // If we were out of orders, try to get a new one
			{
				GetNextOrder();
			}
		}
	}
}
