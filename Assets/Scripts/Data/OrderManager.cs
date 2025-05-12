using UnityEngine;
using System.Collections.Generic;
using TMPro; // Make sure TextMeshPro is imported in Package Manager

public class OrderManager : MonoBehaviour
{
	[Header("Order Settings")]
	[SerializeField] private List<RecipeData> possibleOrders; // Assign RecipeData assets in Inspector
	[SerializeField] private bool randomOrder = true; // Choose randomly or sequentially?

	[Header("UI")]
	[SerializeField] private TextMeshProUGUI currentOrderText; // Assign your UI Text element here

	[Header("State")]
	[SerializeField] private RecipeData currentOrder;
	private int currentOrderIndex = -1;

	public static OrderManager Instance { get; private set; } // Simple Singleton pattern

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
		}
		else
		{
			Instance = this;
		}
	}

	void Start()
	{
		if (possibleOrders == null || possibleOrders.Count == 0)
		{
			Debug.LogError("OrderManager: No possible orders assigned!");
			if (currentOrderText != null) currentOrderText.text = "No Orders Available";
			enabled = false;
			return;
		}
		if (currentOrderText == null)
		{
			Debug.LogError("OrderManager: Current Order Text UI not assigned!");
		}
		GetNextOrder();
	}

	void GetNextOrder()
	{
		if (possibleOrders.Count == 0) return; // Should not happen after Start check

		if (randomOrder)
		{
			currentOrderIndex = Random.Range(0, possibleOrders.Count);
		}
		else
		{
			currentOrderIndex++;
			if (currentOrderIndex >= possibleOrders.Count)
			{
				// Optional: Handle what happens when all orders are done (e.g., loop, stop, show message)
				Debug.Log("All orders completed!");
				if (currentOrderText != null) currentOrderText.text = "All Done!";
				currentOrder = null; // No more orders
				return;
				// Or loop: currentOrderIndex = 0;
			}
		}

		currentOrder = possibleOrders[currentOrderIndex];
		UpdateOrderUI();
	}

	void UpdateOrderUI()
	{
		if (currentOrderText != null && currentOrder != null)
		{
			currentOrderText.text = $"Order: {currentOrder.recipeName}";
		}
		else if (currentOrderText != null)
		{
			// Handle case where there might be no current order after completion
			currentOrderText.text = "";
		}
	}

	public bool CheckOrderCompletion(GameObject submittedDish)
	{
		if (currentOrder == null || submittedDish == null)
		{
			return false; // No active order or no dish submitted
		}

		// Check if the submitted dish has the tag defined in the current recipe's Result Dish Tag
		if (!string.IsNullOrEmpty(currentOrder.resultDishTag) && submittedDish.CompareTag(currentOrder.resultDishTag))
		{
			Debug.Log($"Correct dish submitted: {currentOrder.recipeName}");
			return true;
		}
		else
		{
			Debug.Log($"Incorrect dish submitted. Expected tag: {currentOrder.resultDishTag}, Submitted object tag: {submittedDish.tag}");
			return false;
		}
	}

	public void OrderCompleted()
	{
		// Potentially add score, rewards, sound effects here later
		Debug.Log($"Order '{currentOrder.recipeName}' Completed!");
		GetNextOrder(); // Immediately fetch the next order
	}
}