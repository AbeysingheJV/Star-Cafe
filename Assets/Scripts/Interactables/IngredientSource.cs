using UnityEngine;

public class IngredientSource : MonoBehaviour
{
	[Header("Ingredient Prefab")]
	[Tooltip("Assign the ingredient prefab that this source provides.")]
	[SerializeField] private GameObject ingredientPrefab;

	
	public GameObject GetIngredientPrefab()
	{
		if (ingredientPrefab == null)
		{
			Debug.LogError($"IngredientSource on {gameObject.name} is missing its Ingredient Prefab assignment!", this);
			return null;
		}
		
		if (ingredientPrefab.GetComponent<PickupableItem>() == null)
		{
			Debug.LogError($"Assigned prefab '{ingredientPrefab.name}' on {gameObject.name} is missing the PickupableItem script!", this);
			return null;
		}
		return ingredientPrefab;
	}
}