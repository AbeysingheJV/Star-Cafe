using UnityEngine;

public class CounterSurface : MonoBehaviour
{
	[Tooltip("Assign an empty GameObject positioned where items should snap on this counter.")]
	[SerializeField] private Transform placementPoint;

	
	public Transform GetPlacementPoint()
	{
		if (placementPoint == null)
		{
			Debug.LogWarning($"CounterSurface on {gameObject.name} is missing a Placement Point assignment!", this);
		
			return transform;
		}
		return placementPoint;
	}
}