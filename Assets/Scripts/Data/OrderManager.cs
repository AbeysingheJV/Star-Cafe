using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class OrderManager : MonoBehaviour
{
	[Header("Order Settings")]
	[SerializeField] private List<RecipeData> initialPossibleOrders; // Recipes available at the start
	[SerializeField] private bool randomOrder = true;

	[Header("UI")]
	[SerializeField] private TextMeshProUGUI currentOrderText;

	[Header("Audio")]
	[SerializeField] private AudioClip newOrderSound;
	[SerializeField] private AudioClip orderCompleteSound;

	[Header("State (Read Only)")]
	[SerializeField] private RecipeData currentOrder;
	private int currentOrderIndex = -1; // Used for sequential non-random orders
	private bool noMoreOrdersAvailable = false; // If all sequential are done or pool is empty

	public static OrderManager Instance { get; private set; }
	private AudioSource audioSource;
	private List<RecipeData> activeOrderPool = new List<RecipeData>(); // Current pool of recipes being used

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

		// Initialize active pool with starting recipes
		if (initialPossibleOrders != null)
		{
			activeOrderPool.AddRange(initialPossibleOrders);
		}
	}

	void Start()
	{
		if (currentOrderText == null)
		{
			Debug.LogError("OrderManager: Current Order Text UI not assigned!");
		}
		GetNextOrder();
	}

	void GetNextOrder()
	{
		if (activeOrderPool.Count == 0)
		{
			noMoreOrdersAvailable = true;
			currentOrder = null;
			if (currentOrderText != null) currentOrderText.text = "No more orders for now!";
			Debug.Log("OrderManager: No recipes in active pool to choose from.");
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
				{ // Try to avoid picking the same order twice in a row
					currentOrder = activeOrderPool[Random.Range(0, activeOrderPool.Count)];
					attempts++;
				} while (currentOrder == previousOrder && attempts < activeOrderPool.Count * 2);
			}
			else
			{
				currentOrder = activeOrderPool[Random.Range(0, activeOrderPool.Count)];
			}
		}
		else // Sequential (from the current active pool)
		{
			currentOrderIndex++;
			if (currentOrderIndex >= activeOrderPool.Count)
			{
				// This means all orders in the current pool have been done sequentially.
				// Depending on design, you might loop, or stop, or wait for unlocks.
				// For now, let's just log and potentially stop if no new recipes are unlocked.
				Debug.Log("OrderManager: Reached end of sequential list. Looping or waiting for unlocks.");
				currentOrderIndex = 0; // Simple loop for now
			}
			currentOrder = activeOrderPool[currentOrderIndex];
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
				currentOrderText.text = $"{currentOrder.recipeName}";
			}
			else if (noMoreOrdersAvailable)
			{
				currentOrderText.text = "Waiting for new recipes...";
			}
			else
			{
				currentOrderText.text = ""; // Should not happen if GetNextOrder is robust
			}
		}
	}

	public bool CheckOrderCompletion(GameObject submittedDish)
	{
		if (currentOrder == null || submittedDish == null)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(currentOrder.resultDishTag) && submittedDish.CompareTag(currentOrder.resultDishTag))
		{
			return true;
		}
		return false;
	}

	public void OrderCompleted()
	{
		if (currentOrder == null) return;

		Debug.Log($"Order '{currentOrder.recipeName}' Completed!");

		if (audioSource != null && orderCompleteSound != null)
		{
			audioSource.PlayOneShot(orderCompleteSound);
		}

		// --- Notify RewardManager ---
		if (RewardManager.Instance != null)
		{
			RewardManager.Instance.IncrementDishesCompleted();
		}
		// --------------------------

		if (CatAI.Instance != null)
		{
			CatAI.Instance.TriggerMiauAndSound();
		}

		GetNextOrder();
	}

	// Called by RewardManager to add newly unlocked recipes
	public void UnlockNewRecipe(RecipeData newRecipe)
	{
		if (newRecipe != null && !activeOrderPool.Contains(newRecipe))
		{
			activeOrderPool.Add(newRecipe);
			Debug.Log($"OrderManager: New recipe '{newRecipe.recipeName}' added to available orders.");
			if (noMoreOrdersAvailable || currentOrder == null) // If we were out of orders, try to get a new one
			{
				GetNextOrder();
			}
		}
	}
}