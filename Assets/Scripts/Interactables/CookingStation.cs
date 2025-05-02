using UnityEngine;
using System.Collections.Generic; 
using System.Linq; 

[RequireComponent(typeof(Collider))] 
public class CookingStation : MonoBehaviour
{
	[Header("Setup")]
	[Tooltip("Assign all possible RecipeData Scriptable Objects here.")]
	[SerializeField] private List<RecipeData> availableRecipes;

	[Tooltip("Assign an empty GameObject where the resulting cooked item should spawn.")]
	[SerializeField] private Transform resultSpawnPoint;

	[Header("State (Read Only)")]
	[SerializeField] 
	private List<Pickupable> itemsOnStation = new List<Pickupable>(); 

	private Collider triggerCollider;

	void Awake()
	{
		triggerCollider = GetComponent<Collider>();
		if (!triggerCollider.isTrigger)
		{
			Debug.LogWarning($"Collider on CookingStation '{gameObject.name}' is not set to 'Is Trigger'. Ingredient detection might not work correctly.", this);
		}
		if (resultSpawnPoint == null)
		{
			Debug.LogWarning($"CookingStation '{gameObject.name}' is missing its Result Spawn Point assignment! Using own transform as fallback.", this);
			
			resultSpawnPoint = transform;
		}
		if (availableRecipes == null || availableRecipes.Count == 0)
		{
			Debug.LogWarning($"CookingStation '{gameObject.name}' has no available recipes assigned!", this);
		}
	}

	void OnTriggerEnter(Collider other)
	{
		Pickupable pickupable = other.GetComponent<Pickupable>();
		
		if (pickupable != null && pickupable.ingredientData != null && !itemsOnStation.Contains(pickupable))
		{
			Debug.Log($"{pickupable.ingredientData.displayName} entered {gameObject.name}");
			itemsOnStation.Add(pickupable);
		}
		
	}

	void OnTriggerExit(Collider other)
	{
		Pickupable pickupable = other.GetComponent<Pickupable>();
		
		if (pickupable != null && itemsOnStation.Contains(pickupable))
		{
			
			string itemName = pickupable.ingredientData != null ? pickupable.ingredientData.displayName : pickupable.gameObject.name;
			Debug.Log($"{itemName} exited {gameObject.name}");
			itemsOnStation.Remove(pickupable);
		}
	}

	
	public bool TryCook()
	{
		Debug.Log($"Trying to cook with {itemsOnStation.Count} potential ingredient items on station.");
		
		itemsOnStation.RemoveAll(item => item == null || item.gameObject == null);

		
		List<IngredientData> currentIngredientsData = itemsOnStation
			.Where(p => p.ingredientData != null) 
			.Select(p => p.ingredientData)
			.ToList();

		if (currentIngredientsData.Count == 0) 
		{
			Debug.Log("No valid ingredients on the station to cook with.");
			return false;
		}

		
		RecipeData matchedRecipe = FindMatchingRecipe(currentIngredientsData);

		if (matchedRecipe != null)
		{
			Debug.Log($"Recipe Matched: {matchedRecipe.recipeName}! Cooking...");

			
			List<Pickupable> itemsToDestroy = itemsOnStation
											 .Where(p => p.ingredientData != null) 
											 .ToList();

			Debug.Log($"Destroying {itemsToDestroy.Count} ingredients.");
			foreach (Pickupable ingredient in itemsToDestroy) 
			{
				if (ingredient != null) Destroy(ingredient.gameObject);
			}
			
			itemsOnStation.Clear(); 

			
			if (matchedRecipe.resultPrefab != null)
			{
				Instantiate(matchedRecipe.resultPrefab, resultSpawnPoint.position, resultSpawnPoint.rotation);
				Debug.Log($"Spawned {matchedRecipe.resultPrefab.name}");
			}
			else
			{
				Debug.LogError($"Recipe '{matchedRecipe.recipeName}' has no result prefab assigned!");
			}

			return true; 
		}
		else
		{
			Debug.Log("No matching recipe found for the valid ingredients on the station.");
			
			return false; 
		}
	}

	
	private RecipeData FindMatchingRecipe(List<IngredientData> currentIngredients)
	{
		foreach (RecipeData recipe in availableRecipes)
		{
			if (DoIngredientsMatch(recipe.requiredIngredients, currentIngredients))
			{
				return recipe;
			}
		}
		return null; 
	}

	
	private bool DoIngredientsMatch(List<IngredientData> required, List<IngredientData> current)
	{
		if (required == null || current == null) return false; 
		if (required.Count != current.Count) return false; 
		if (required.Count == 0) return false; 

		
		var requiredCounts = required.Where(data => data != null)
									 .GroupBy(data => data.ingredientID)
									 .ToDictionary(group => group.Key, group => group.Count());
		var currentCounts = current.Where(data => data != null)
								   .GroupBy(data => data.ingredientID)
								   .ToDictionary(group => group.Key, group => group.Count());


		
		if (requiredCounts.Count != currentCounts.Count) return false;

		
		foreach (var kvp in requiredCounts)
		{
			
			if (!currentCounts.ContainsKey(kvp.Key) || currentCounts[kvp.Key] != kvp.Value)
			{
				return false; 
			}
		}

		return true; 
	}
}