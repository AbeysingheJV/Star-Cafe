using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Collider))]
public class CookingStation : MonoBehaviour
{
	[Header("Setup")]
	[SerializeField] private List<RecipeData> availableRecipes;
	[SerializeField] private Transform resultSpawnPoint;
	[SerializeField] private Slider progressBar;
	[SerializeField] private ParticleSystem cookingEffectPrefab;

	[Header("Audio")]
	[SerializeField] private AudioClip processStartSound; // e.g., initial knife on board
	[SerializeField] private AudioClip processingLoopSound; // e.g., continuous chopping
	[SerializeField] private AudioClip processCompleteSound; // e.g., final chop, item ready

	[Header("State (Read Only)")]
	[SerializeField] private List<Pickupable> ingredientsOnStation = new List<Pickupable>();

	private bool isProcessing = false; // Renamed from isCooking for clarity
	private float currentProcessTimer = 0f; // Renamed
	private RecipeData currentRecipe = null;
	private Collider triggerCollider;
	private ParticleSystem currentProcessingEffectInstance = null; // Renamed
	private AudioSource audioSource;

	void Awake()
	{
		triggerCollider = GetComponent<Collider>();
		audioSource = GetComponent<AudioSource>(); // Get the AudioSource

		if (!triggerCollider.isTrigger) { Debug.LogWarning($"Collider on {gameObject.name} not 'Is Trigger'.", this); }
		if (resultSpawnPoint == null) { Debug.LogWarning($"{gameObject.name} missing Result Spawn Point!", this); resultSpawnPoint = transform; }
		if (availableRecipes == null || availableRecipes.Count == 0) { Debug.LogWarning($"{gameObject.name} has no recipes!", this); }
		if (progressBar != null) { progressBar.gameObject.SetActive(false); progressBar.value = 0; }
		if (cookingEffectPrefab == null) { Debug.LogWarning($"CookingStation on {gameObject.name} is missing its Cooking Effect Prefab assignment!", this); }
		if (audioSource == null) { Debug.LogWarning($"CookingStation on {gameObject.name} is missing an AudioSource component for sounds!", this); }
	}

	void Update()
	{
		if (isProcessing)
		{
			UpdateHoldToProcess(Time.deltaTime); // Renamed
		}
		if (progressBar != null && progressBar.gameObject.activeSelf)
		{
			UpdateProgressBarTransform();
		}
	}

	void OnDestroy()
	{
		if (currentProcessingEffectInstance != null)
		{
			Destroy(currentProcessingEffectInstance.gameObject);
		}
		// Ensure looping sound is stopped if station is destroyed
		if (audioSource != null && audioSource.isPlaying && audioSource.loop)
		{
			audioSource.Stop();
		}
	}

	void OnTriggerEnter(Collider other)
	{
		Pickupable p = other.GetComponent<Pickupable>();
		if (p != null && p.ingredientData != null && !ingredientsOnStation.Contains(p) && p.Rb != null && !p.Rb.isKinematic)
		{
			ingredientsOnStation.Add(p);
		}
	}

	void OnTriggerExit(Collider other)
	{
		Pickupable p = other.GetComponent<Pickupable>();
		if (p != null && ingredientsOnStation.Contains(p))
		{
			ingredientsOnStation.Remove(p);
		}
	}

	public bool StartHoldToCook() // Method name in PickupController is StartHoldToCook
	{
		if (isProcessing) return false;

		ingredientsOnStation.RemoveAll(item => item == null || item.gameObject == null || (item.Rb != null && item.Rb.isKinematic));
		List<IngredientData> currentIngredientsData = ingredientsOnStation.Where(p => p != null && p.ingredientData != null).Select(p => p.ingredientData).ToList();

		if (currentIngredientsData.Count == 0) { return false; }

		currentRecipe = FindMatchingRecipe(currentIngredientsData);

		if (currentRecipe != null)
		{
			isProcessing = true;
			currentProcessTimer = 0f;

			if (progressBar != null)
			{
				progressBar.minValue = 0;
				progressBar.maxValue = currentRecipe.cookingDuration;
				progressBar.value = 0;
				progressBar.gameObject.SetActive(true);
			}

			if (cookingEffectPrefab != null)
			{
				currentProcessingEffectInstance = Instantiate(cookingEffectPrefab, resultSpawnPoint.position, resultSpawnPoint.rotation);
				currentProcessingEffectInstance.Play();
			}

			// Play Start Sound
			if (audioSource != null && processStartSound != null)
			{
				audioSource.PlayOneShot(processStartSound);
			}
			// Play Looping Sound
			if (audioSource != null && processingLoopSound != null)
			{
				audioSource.clip = processingLoopSound;
				audioSource.loop = true;
				audioSource.Play();
			}

			return true;
		}
		return false;
	}

	private void UpdateHoldToProcess(float deltaTime) // Renamed
	{
		if (!isProcessing || currentRecipe == null) return;

		currentProcessTimer += deltaTime;
		if (progressBar != null) { progressBar.value = currentProcessTimer; }

		if (currentProcessTimer >= currentRecipe.cookingDuration)
		{
			CompleteProcessing(); // Renamed
		}
	}

	public void CancelHoldToCook() // Method name in PickupController is CancelHoldToCook
	{
		if (!isProcessing) return;

		isProcessing = false;
		// currentRecipe = null; // Keep currentRecipe for CompleteSound if needed, will be nulled in CompleteProcessing
		// currentProcessTimer = 0f; // Timer gets reset too

		if (progressBar != null) { progressBar.gameObject.SetActive(false); progressBar.value = 0; }

		if (currentProcessingEffectInstance != null)
		{
			currentProcessingEffectInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			Destroy(currentProcessingEffectInstance.gameObject, 2f);
			currentProcessingEffectInstance = null;
		}

		// Stop Looping Sound
		if (audioSource != null && audioSource.isPlaying && audioSource.clip == processingLoopSound)
		{
			audioSource.Stop();
			audioSource.loop = false; // Important to reset loop state
		}
	}

	private void CompleteProcessing() // Renamed
	{
		if (!isProcessing || currentRecipe == null) return; // Should already be true from UpdateHoldToProcess

		// Stop Looping Sound first if it's still playing (though CancelHoldToCook might be called after this by some logic)
		if (audioSource != null && audioSource.isPlaying && audioSource.clip == processingLoopSound)
		{
			audioSource.Stop();
			audioSource.loop = false;
		}
		// Play Complete Sound
		if (audioSource != null && processCompleteSound != null)
		{
			audioSource.PlayOneShot(processCompleteSound);
		}

		Debug.Log($"Process Complete: {currentRecipe.recipeName}!");
		List<Pickupable> itemsToDestroy = new List<Pickupable>(ingredientsOnStation);
		foreach (Pickupable ingredient in itemsToDestroy) { if (ingredient != null) Destroy(ingredient.gameObject); }
		ingredientsOnStation.Clear();

		if (currentRecipe.resultPrefab != null)
		{
			Instantiate(currentRecipe.resultPrefab, resultSpawnPoint.position, resultSpawnPoint.rotation);
		}
		else { Debug.LogError($"Recipe '{currentRecipe.recipeName}' has no result prefab!"); }

		// Cache recipe name for log before nullifying
		// string completedRecipeName = currentRecipe.recipeName;

		// Reset all processing states
		bool wasProcessing = isProcessing;
		isProcessing = false; // Set before calling Cancel, so it doesn't try to stop sounds again if Cancel is called
		currentRecipe = null;
		currentProcessTimer = 0f;


		if (wasProcessing) // Ensure CancelHoldToCook's effects like stopping particles only happen if it was truly active
		{
			CancelHoldToCook(); // This will hide progress bar, stop particles.
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
		if (required == null || current == null || required.Count != current.Count || required.Count == 0) return false;
		var requiredCounts = required.Where(d => d != null).GroupBy(d => d.ingredientID).ToDictionary(g => g.Key, g => g.Count());
		var currentCounts = current.Where(d => d != null).GroupBy(d => d.ingredientID).ToDictionary(g => g.Key, g => g.Count());
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

	void UpdateProgressBarTransform()
	{
		if (progressBar == null) return;
		Canvas canvas = progressBar.GetComponentInParent<Canvas>();
		if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && Camera.main != null)
		{
			progressBar.transform.position = transform.position + Vector3.up * 0.5f;
			progressBar.transform.LookAt(Camera.main.transform);
			progressBar.transform.Rotate(0, 180, 0);
		}
	}
}