using UnityEngine;


public class IngredientSource : MonoBehaviour
{
	[Header("Ingredient Prefab")]
	[Tooltip("Assign the ACTUAL ingredient prefab (the one with Pickupable script) that this source provides.")]
	[SerializeField] private GameObject ingredientPrefab;

	
	public GameObject GetIngredientPrefab()
	{
		if (ingredientPrefab == null)
		{
			Debug.LogError($"IngredientSource on {gameObject.name} is missing its Ingredient Prefab assignment!", this);
			return null;
		}
		
		if (ingredientPrefab.GetComponent<Pickupable>() == null)
		{
			Debug.LogError($"Assigned prefab '{ingredientPrefab.name}' on {gameObject.name} is missing the Pickupable script!", this);
			return null;
		}
		return ingredientPrefab;
	}

	public string GetIngredientName()
	{
		if (ingredientPrefab != null)
		{
			Pickupable p = ingredientPrefab.GetComponent<Pickupable>();
			if (p != null && p.ingredientData != null && !string.IsNullOrEmpty(p.ingredientData.displayName))
			{
				return p.ingredientData.displayName;
			}
			return ingredientPrefab.name; // Fallback
		}
		return "Item";
	}


	void Start()
	{
		if (GetComponent<Collider>() == null)
		{
			Debug.LogError($"IngredientSource on {gameObject.name} requires a Collider component to be detected by the player's raycast!", this);
		}
		
		if (GetComponent<Rigidbody>() != null)
		{
			Debug.LogWarning($"IngredientSource on {gameObject.name} has a Rigidbody. This is usually not needed for static sources.", this);
		}
	}
}