using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class OrderManager : MonoBehaviour
{
	public static OrderManager Instance { get; private set; }

	[Header("Order Settings")]
	public List<RecipeData> initialPossibleOrders; // Recipes available at the very start of a new game.
	[SerializeField] private bool randomOrder = true; // Whether to pick orders randomly or sequentially.

	[Header("UI")]
	[SerializeField] private TextMeshProUGUI currentOrderText; // UI element to display the current order.

	[Header("Audio")]
	[SerializeField] private AudioClip newOrderSound; // Sound when a new order appears.
	[SerializeField] private AudioClip orderCompleteSound; // Sound when an order is completed.

	[Header("State (Read Only)")]
	[SerializeField] private RecipeData currentOrder; // The recipe the player currently needs to make.

	private int currentSequentialIndex = -1; // Index for sequential order picking.
	private bool noMoreOrdersAvailable = false; // Flag if no recipes are left to order.
	private AudioSource audioSource; // Component to play audio clips.
	private List<RecipeData> activeOrderPool = new List<RecipeData>(); // Current list of recipes that can be ordered.

	// Called when the script instance is being loaded.
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

	// Called before the first frame update, after all Awake functions.
	void Start()
	{
		if (currentOrderText == null)
		{
			Debug.LogError("OrderManager: Current Order Text UI not assigned!");
		}

		if (activeOrderPool.Count == 0)
		{
			if (initialPossibleOrders != null && initialPossibleOrders.Count > 0)
			{
				Debug.LogWarning("OrderManager: Active order pool empty at Start. Populating with initialPossibleOrders. GameDataManager should ideally manage this for new/loaded games.");
				List<string> initialRecipeNames = new List<string>();
				foreach (RecipeData recipe in initialPossibleOrders)
				{
					if (recipe != null && !string.IsNullOrEmpty(recipe.name))
					{
						initialRecipeNames.Add(recipe.name);
					}
				}
				ApplyUnlockedRecipesFromLoad(initialRecipeNames);
			}
			else
			{
				Debug.LogWarning("OrderManager: No recipes available at Start (initial list also empty or null).");
				UpdateOrderUI();
			}
		}
	}

	// Updates the active pool of orderable recipes based on loaded save data.
	public void ApplyUnlockedRecipesFromLoad(List<string> unlockedRecipeNames)
	{
		activeOrderPool.Clear();
		bool appliedAnyFromSave = false;
		bool appliedAnyFromInitial = false;

		if (unlockedRecipeNames != null && unlockedRecipeNames.Count > 0)
		{
			if (RewardManager.Instance != null && RewardManager.Instance.allUnlockableRecipesPool != null)
			{
				foreach (string recipeName in unlockedRecipeNames)
				{
					if (string.IsNullOrEmpty(recipeName)) continue;
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
			}
		}

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

	// Selects and sets the next customer order.
	private void GetNextOrder()
	{
		if (activeOrderPool.Count == 0)
		{
			noMoreOrdersAvailable = true;
			currentOrder = null;
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
				} while (currentOrder == previousOrder && attempts < activeOrderPool.Count * 2);
			}
			else if (activeOrderPool.Count > 0)
			{
				currentOrder = activeOrderPool[Random.Range(0, activeOrderPool.Count)];
			}
		}
		else
		{
			if (activeOrderPool.Count > 0)
			{
				currentSequentialIndex++;
				if (currentSequentialIndex >= activeOrderPool.Count)
				{
					currentSequentialIndex = 0;
				}
				currentOrder = activeOrderPool[currentSequentialIndex];
			}
		}

		UpdateOrderUI();

		if (audioSource != null && newOrderSound != null && currentOrder != null)
		{
			audioSource.PlayOneShot(newOrderSound);
		}
	}

	// Updates the UI text to display the current order.
	private void UpdateOrderUI()
	{
		if (currentOrderText != null)
		{
			if (currentOrder != null)
			{
				currentOrderText.text = $"Order: {currentOrder.recipeName}";
			}
			else
			{
				currentOrderText.text = "No Orders Available";
			}
		}
	}

	// Checks if the submitted dish matches the current order.
	public bool CheckOrderCompletion(GameObject submittedDish)
	{
		if (currentOrder == null || submittedDish == null) return false;
		return !string.IsNullOrEmpty(currentOrder.resultDishTag) && submittedDish.CompareTag(currentOrder.resultDishTag);
	}

	// Called when an order is successfully completed.
	public void OrderCompleted()
	{
		if (currentOrder == null) return;
		Debug.Log($"Order '{currentOrder.recipeName}' Completed!");

		if (audioSource != null && orderCompleteSound != null) audioSource.PlayOneShot(orderCompleteSound);

		if (RewardManager.Instance != null)
		{
			RewardManager.Instance.IncrementDishesCompleted();
		}

		if (CatAI.Instance != null) CatAI.Instance.TriggerMiauAndSound();
		GetNextOrder();
	}

	// Adds a newly unlocked recipe to the active pool of orderable recipes.
	public void UnlockNewRecipe(RecipeData newRecipe)
	{
		if (newRecipe != null && !activeOrderPool.Contains(newRecipe))
		{
			activeOrderPool.Add(newRecipe);
			Debug.Log($"OrderManager: New recipe '{newRecipe.recipeName}' added to active order pool for current session.");
			if (currentOrder == null || noMoreOrdersAvailable)
			{
				GetNextOrder();
			}
		}
	}
}