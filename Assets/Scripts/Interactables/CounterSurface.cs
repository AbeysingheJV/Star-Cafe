using UnityEngine;

public class CounterSurface : MonoBehaviour
{
	[Tooltip("Assign an empty GameObject positioned exactly where items should snap on this surface (e.g., center of cutting board).")]
	[SerializeField] private Transform placementPoint;

	public Transform GetPlacementPoint()
	{
		
		if (placementPoint == null)
		{
			
			return transform; 
        }
		return placementPoint; 
    }

}