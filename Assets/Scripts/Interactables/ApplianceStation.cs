using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Collider))]
public class ApplianceStation : MonoBehaviour // For Pan/Pot (Press Q once)
{
	[Header("Setup")]
	[SerializeField] private List<RecipeData> availableRecipes;
	[SerializeField] private Transform resultSpawnPoint;
	[SerializeField] private Slider progressBar;
	// [SerializeField] private ParticleSystem cookingEffectPrefab; // If you want particles too

	[Header("Audio")]
	[SerializeField] private AudioClip processStartSound;    // e.g., item hits hot pan
	[SerializeField] private AudioClip processingLoopSound;  // e.g., continuous sizzling
	[SerializeField] private AudioClip processCompleteSound; // e.g., sizzling fades, item ready

	[Header("State (Read Only)")]
	[SerializeField] private List<Pickupable> ingredientsOnStation = new List<Pickupable>();

	private bool isCooking = false;
	private float currentCookTimer = 0f;
	private RecipeData currentRecipe = null;
	private Collider triggerCollider;
	// private ParticleSystem currentCookingEffectInstance = null;
	private AudioSource audioSource;

	void Awake()
	{
		triggerCollider = GetComponent<Collider>();
		audioSource = GetComponent<AudioSource>(); // Get the AudioSource

		if (!triggerCollider.isTrigger) { Debug.LogWarning($"Collider on {gameObject.name} not 'Is Trigger'.", this); }
		if (resultSpawnPoint == null) { Debug.LogWarning($"{gameObject.name} missing Result Spawn Point!", this); resultSpawnPoint = transform; }
		if (availableRecipes == null || availableRecipes.Count == 0) { Debug.LogWarning($"{gameObject.name} has no recipes!", this); }
		if (progressBar != null) { progressBar.gameObject.SetActive(false); progressBar.value = 0; }
		// if (cookingEffectPrefab == null) { Debug.LogWarning($"ApplianceStation on {gameObject.name} is missing its Cooking Effect Prefab assignment!", this); }
		if (audioSource == null) { Debug.LogWarning($"ApplianceStation on {gameObject.name} is missing an AudioSource component for sounds!", this); }
	}

	void Update()
	{
		if (isCooking)
		{
			UpdateAutoCooking(Time.deltaTime);
		}
		if (progressBar != null && progressBar.gameObject.activeSelf)
		{
			UpdateProgressBarTransform();
		}
	}

	void OnDestroy()
	{
		// if (currentCookingEffectInstance != null)
		// {
		//     Destroy(currentCookingEffectInstance.gameObject);
		// }
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

	public bool StartAutoCooking()
	{
		if (isCooking) { Debug.Log($"{gameObject.name} is already cooking!"); return false; }

		ingredientsOnStation.RemoveAll(item => item == null || item.gameObject == null || (item.Rb != null && item.Rb.isKinematic));
		List<IngredientData> currentIngredientsData = ingredientsOnStation.Where(p => p != null && p.ingredientData != null).Select(p => p.ingredientData).ToList();

		if (currentIngredientsData.Count == 0)
		{
			Debug.Log("No valid ingredients on appliance to start auto-cooking.");
			return false;
		}

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

			// if (cookingEffectPrefab != null)
			// {
			//     currentCookingEffectInstance = Instantiate(cookingEffectPrefab, resultSpawnPoint.position, resultSpawnPoint.rotation);
			//     currentCookingEffectInstance.Play();
			// }

			// Play Start Sound
			if (audioSource != null && processStartSound != null)
			{
				audioSource.PlayOneShot(processStartSound);
			}
			// Play Looping Sound (e.g., Sizzling)
			if (audioSource != null && processingLoopSound != null)
			{
				audioSource.clip = processingLoopSound;
				audioSource.loop = true;
				audioSource.Play();
			}

			return true;
		}
		else
		{
			Debug.Log("No matching recipe found for ingredients on appliance.");
			ResetState(); // Ensure sounds are stopped if no recipe found
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
		if (!isCooking || currentRecipe == null) return; // Safety check

		Debug.Log($"Auto-Cook Complete: {currentRecipe.recipeName}!");

		// Stop Looping Sound before playing complete sound or resetting
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

		List<Pickupable> itemsToDestroy = new List<Pickupable>(ingredientsOnStation);
		foreach (Pickupable ingredient in itemsToDestroy) { if (ingredient != null) Destroy(ingredient.gameObject); }
		ingredientsOnStation.Clear();

		if (currentRecipe.resultPrefab != null)
		{
			Instantiate(currentRecipe.resultPrefab, resultSpawnPoint.position, resultSpawnPoint.rotation);
		}
		else { Debug.LogError($"Recipe '{currentRecipe.recipeName}' has no result prefab!"); }

		ResetState(); // Reset state after completion (this will also handle stopping particles)
	}

	private void ResetState()
	{
		isCooking = false;
		currentRecipe = null;
		currentCookTimer = 0f;

		if (progressBar != null) { progressBar.gameObject.SetActive(false); progressBar.value = 0; }

		// if (currentCookingEffectInstance != null)
		// {
		//     currentCookingEffectInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
		//     Destroy(currentCookingEffectInstance.gameObject, 2f); // Allow particles to fade
		//     currentCookingEffectInstance = null;
		// }

		// Ensure Looping Sound is stopped if it was playing
		if (audioSource != null && audioSource.isPlaying && audioSource.clip == processingLoopSound)
		{
			audioSource.Stop();
			audioSource.loop = false; // Important to reset loop state
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