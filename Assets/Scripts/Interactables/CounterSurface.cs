using UnityEngine;


public class CounterSurface : MonoBehaviour
{
	[Tooltip("Assign an empty GameObject positioned exactly where items should snap on this surface (e.g., center of cutting board).")]
	[SerializeField] private Transform placementPoint;

	
	private GameObject currentlyPlacedItem = null;

	public Transform GetPlacementPoint()
	{
		if (placementPoint == null)
		{
			Debug.LogWarning($"CounterSurface on {gameObject.name} is missing a Placement Point assignment! Using own transform as fallback.", this);
			return transform;
		}
		return placementPoint;
	}

	
	public bool IsOccupied()
	{
		if (currentlyPlacedItem != null && currentlyPlacedItem.gameObject == null)
		{
			Debug.Log($"{gameObject.name} detected its placed item was destroyed. Clearing slot.");
			currentlyPlacedItem = null;
		}
		return currentlyPlacedItem != null;
	}

	
	public void SetOccupied(GameObject item)
	{
		if (item != null)
		{
			Debug.Log($"{gameObject.name} is now occupied by {item.name}");
			currentlyPlacedItem = item;
		}
	}

	
	public void SetUnoccupied()
	{
		if (currentlyPlacedItem != null)
		{
			Debug.Log($"{gameObject.name} is now unoccupied (item: {currentlyPlacedItem.name}).");
			currentlyPlacedItem = null;
		}
	}

	void Start()
	{
		if (GetComponent<Collider>() == null)
		{
			Debug.LogError($"GameObject '{gameObject.name}' with CounterSurface needs a Collider component.", this);
		}
	}
}