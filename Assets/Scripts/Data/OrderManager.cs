using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class OrderManager : MonoBehaviour
{
	[Header("Order Settings")]
	[SerializeField] private List<RecipeData> possibleOrders;
	[SerializeField] private bool randomOrder = true;

	[Header("UI")]
	[SerializeField] private TextMeshProUGUI currentOrderText;

	[Header("Audio")]
	[SerializeField] private AudioClip newOrderSound;
	[SerializeField] private AudioClip orderCompleteSound;

	[Header("State (Read Only)")]
	[SerializeField] private RecipeData currentOrder;
	private int currentOrderIndex = -1;
	private bool allOrdersDone = false;

	public static OrderManager Instance { get; private set; }
	private AudioSource audioSource;

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
		audioSource = GetComponent<AudioSource>(); // Get the AudioSource
		if (audioSource == null) { Debug.LogWarning("OrderManager requires an AudioSource component for sounds!", this); }
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
		if (allOrdersDone) return; // Stop if all orders were completed and not looping

		if (possibleOrders.Count == 0) return;

		if (randomOrder)
		{
			// Avoid picking the same order twice in a row if possible and there's more than one order
			if (possibleOrders.Count > 1)
			{
				int newIndex = currentOrderIndex;
				while (newIndex == currentOrderIndex)
				{
					newIndex = Random.Range(0, possibleOrders.Count);
				}
				currentOrderIndex = newIndex;
			}
			else
			{
				currentOrderIndex = 0; // Only one order, so pick that one
			}
		}
		else // Sequential
		{
			currentOrderIndex++;
			if (currentOrderIndex >= possibleOrders.Count)
			{
				Debug.Log("All sequential orders completed!");
				if (currentOrderText != null) currentOrderText.text = "All Orders Done!";
				currentOrder = null;
				allOrdersDone = true;
				// Optional: Play a special "all orders complete" sound here
				return;
			}
		}

		currentOrder = possibleOrders[currentOrderIndex];
		UpdateOrderUI();

		// Play new order sound
		if (audioSource != null && newOrderSound != null && currentOrder != null)
		{
			audioSource.PlayOneShot(newOrderSound);
		}
	}

	void UpdateOrderUI()
	{
		if (currentOrderText != null && currentOrder != null)
		{
			currentOrderText.text = $"Order: {currentOrder.recipeName}";
		}
		else if (currentOrderText != null && allOrdersDone)
		{
			currentOrderText.text = "All Orders Done!";
		}
		else if (currentOrderText != null)
		{
			currentOrderText.text = ""; // Clear if no current order for other reasons
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
			Debug.Log($"Correct dish submitted: {currentOrder.recipeName}");
			return true;
		}
		else
		{
			Debug.Log($"Incorrect dish submitted. Expected tag: '{currentOrder.resultDishTag}', Submitted object tag: '{submittedDish.tag}'");
			return false;
		}
	}

	public void OrderCompleted()
	{
		if (currentOrder == null) return; // Should not happen if CheckOrderCompletion was true

		Debug.Log($"Order '{currentOrder.recipeName}' Completed!");

		// Play order complete sound
		if (audioSource != null && orderCompleteSound != null)
		{
			audioSource.PlayOneShot(orderCompleteSound);
		}

		GetNextOrder();
	}
}