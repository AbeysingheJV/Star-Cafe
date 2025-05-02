using UnityEngine;
using System.Collections.Generic; 


[CreateAssetMenu(fileName = "New Recipe", menuName = "Cooking/Recipe Data")]
public class RecipeData : ScriptableObject
{
	public string recipeName; 

	[Tooltip("List of ingredients required for this recipe.")]
	public List<IngredientData> requiredIngredients;

	[Tooltip("The prefab of the item created when this recipe is cooked.")]
	public GameObject resultPrefab;

	
}