using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Collider))]
public class ApplianceStation : MonoBehaviour 
{
	

	[Header("Setup")]
	[SerializeField] private List<RecipeData> availableRecipes;
	[SerializeField] private Transform resultSpawnPoint;
	[SerializeField] private Slider progressBar;

	[Header("State (Read Only)")]
	[SerializeField] private List<Pickupable> ingredientsOnStation = new List<Pickupable>();

	private bool isCooking = false; 
	private float currentCookTimer = 0f;
	private RecipeData currentRecipe = null;

	private Collider triggerCollider;

	void Awake()
	{
		triggerCollider = GetComponent<Collider>();
		if (!triggerCollider.isTrigger) { Debug.LogWarning($"Collider on {gameObject.name} not 'Is Trigger'.", this); }
		if (resultSpawnPoint == null) { Debug.LogWarning($"{gameObject.name} missing Result Spawn Point!", this); resultSpawnPoint = transform; }
		if (availableRecipes == null || availableRecipes.Count == 0) { Debug.LogWarning($"{gameObject.name} has no recipes!", this); }
		if (progressBar != null) { progressBar.gameObject.SetActive(false); progressBar.value = 0; }
	}

	void Update()
	{
		if (isCooking) 
		{
			UpdateAutoCooking(Time.deltaTime);
		}
		if (progressBar != null && progressBar.gameObject.activeSelf) { UpdateProgressBarTransform(); }
	}

	void OnTriggerEnter(Collider other) { Pickupable p = other.GetComponent<Pickupable>(); if (p != null && p.ingredientData != null && !ingredientsOnStation.Contains(p) && p.Rb != null && !p.Rb.isKinematic) { ingredientsOnStation.Add(p); } }
	void OnTriggerExit(Collider other) { Pickupable p = other.GetComponent<Pickupable>(); if (p != null && ingredientsOnStation.Contains(p)) { ingredientsOnStation.Remove(p); } }


	
	public bool StartAutoCooking()
	{
		if (isCooking) { Debug.Log($"{gameObject.name} is already cooking!"); return false; } 

		ingredientsOnStation.RemoveAll(item => item == null || item.gameObject == null || (item.Rb != null && item.Rb.isKinematic));
		List<IngredientData> currentIngredientsData = ingredientsOnStation.Where(p => p.ingredientData != null).Select(p => p.ingredientData).ToList();
		if (currentIngredientsData.Count == 0) { Debug.Log("No valid ingredients on appliance."); return false; }

		currentRecipe = FindMatchingRecipe(currentIngredientsData);

		if (currentRecipe != null)
		{
			Debug.Log($"Recipe Matched: {currentRecipe.recipeName}. Starting {currentRecipe.cookingDuration}s automatic cook timer.");
			isCooking = true;
			currentCookTimer = 0f;
			if (progressBar != null)
			{
				progressBar.minValue = 0;
				progressBar.maxValue = currentRecipe.cookingDuration;
				progressBar.value = 0;
				progressBar.gameObject.SetActive(true);
			}
			return true; 
		}
		else
		{
			Debug.Log("No matching recipe found for ingredients on appliance.");
			ResetState(); 
			return false;
		}
	}

	
	private void UpdateAutoCooking(float deltaTime)
	{
		if (!isCooking || currentRecipe == null) return;

		currentCookTimer += deltaTime;
		if (progressBar != null) { progressBar.value = currentCookTimer; }

		if (currentCookTimer >= currentRecipe.cookingDuration)
		{
			CompleteCooking();
		}
	}

	
	private void CompleteCooking()
	{
		if (!isCooking || currentRecipe == null) return; 

		Debug.Log($"Auto-Cook Complete: {currentRecipe.recipeName}!");
		List<Pickupable> itemsToDestroy = new List<Pickupable>(ingredientsOnStation);
		foreach (Pickupable ingredient in itemsToDestroy) { if (ingredient != null) Destroy(ingredient.gameObject); }
		ingredientsOnStation.Clear();

		if (currentRecipe.resultPrefab != null) { Instantiate(currentRecipe.resultPrefab, resultSpawnPoint.position, resultSpawnPoint.rotation); }
		else { Debug.LogError($"Recipe '{currentRecipe.recipeName}' has no result prefab!"); }

		ResetState(); 
	}

	
	private void ResetState()
	{
		isCooking = false;
		currentRecipe = null;
		currentCookTimer = 0f;
		if (progressBar != null) { progressBar.gameObject.SetActive(false); progressBar.value = 0; }
	}

	
	private RecipeData FindMatchingRecipe(List<IngredientData> currentIngredients) { foreach (RecipeData recipe in availableRecipes) { if (DoIngredientsMatch(recipe.requiredIngredients, currentIngredients)) { return recipe; } } return null; }
	private bool DoIngredientsMatch(List<IngredientData> required, List<IngredientData> current) { if (required == null || current == null || required.Count != current.Count || required.Count == 0) return false; var requiredCounts = required.Where(d => d != null).GroupBy(d => d.ingredientID).ToDictionary(g => g.Key, g => g.Count()); var currentCounts = current.Where(d => d != null).GroupBy(d => d.ingredientID).ToDictionary(g => g.Key, g => g.Count()); if (requiredCounts.Count != currentCounts.Count) return false; foreach (var kvp in requiredCounts) { if (!currentCounts.ContainsKey(kvp.Key) || currentCounts[kvp.Key] != kvp.Value) { return false; } } return true; }
	void UpdateProgressBarTransform() { if (progressBar == null) return; Canvas canvas = progressBar.GetComponentInParent<Canvas>(); if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && Camera.main != null) { progressBar.transform.position = transform.position + Vector3.up * 0.5f; progressBar.transform.LookAt(Camera.main.transform); progressBar.transform.Rotate(0, 180, 0); } }
}