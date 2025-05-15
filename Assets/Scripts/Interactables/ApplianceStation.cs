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

	[Header("VFX")]
	[SerializeField] private ParticleSystem cookingProcessVFXPrefab; // e.g., Sizzle/Steam ON the pan
	[SerializeField] private Transform cookingProcessVFXSpawnPoint; // Optional: Specific point for the above VFX
	[SerializeField] private ParticleSystem underPanFireVFXPrefab; // Assign your Fire VFX Prefab here
	[SerializeField] private Transform fireSpawnPoint; // Assign the empty GameObject from under your pan model

	[Header("Audio")]
	[SerializeField] private AudioClip processStartSound;
	[SerializeField] private AudioClip processingLoopSound;
	[SerializeField] private AudioClip processCompleteSound;

	[Header("State (Read Only)")]
	[SerializeField] private List<Pickupable> ingredientsOnStation = new List<Pickupable>();

	private bool isCooking = false;
	private float currentCookTimer = 0f;
	private RecipeData currentRecipe = null;
	private Collider triggerCollider;
	private AudioSource audioSource;

	private ParticleSystem currentCookingProcessVFXInstance = null; // For sizzle, etc.
	private ParticleSystem currentUnderPanFireVFXInstance = null;   // For the fire

	void Awake()
	{
		triggerCollider = GetComponent<Collider>();
		audioSource = GetComponent<AudioSource>();

		if (!triggerCollider.isTrigger) { Debug.LogWarning($"Collider on {gameObject.name} not 'Is Trigger'.", this); }
		if (resultSpawnPoint == null) { Debug.LogWarning($"{gameObject.name} missing Result Spawn Point! Using own transform as fallback.", this); resultSpawnPoint = transform; }
		if (cookingProcessVFXSpawnPoint == null) { cookingProcessVFXSpawnPoint = resultSpawnPoint; } // Default to result spawn if not set
		if (fireSpawnPoint == null) { Debug.LogWarning($"ApplianceStation [{gameObject.name}]: Fire Spawn Point not assigned. Fire VFX may not appear correctly.", this); fireSpawnPoint = transform; } // Default to station's base

		if (availableRecipes == null || availableRecipes.Count == 0) { Debug.LogWarning($"{gameObject.name} has no recipes!", this); }
		if (progressBar != null) { progressBar.gameObject.SetActive(false); progressBar.value = 0; }
		if (audioSource == null) { Debug.LogWarning($"ApplianceStation on {gameObject.name} is missing an AudioSource component for sounds!", this); }
		if (underPanFireVFXPrefab == null) { Debug.LogWarning($"ApplianceStation on {gameObject.name} is missing its Under Pan Fire VFX Prefab assignment!", this); }
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
		StopAllVFX(); // Ensure all VFX are cleaned up
		if (audioSource != null && audioSource.isPlaying && audioSource.loop)
		{
			audioSource.Stop();
		}
	}

	private void StopAllVFX()
	{
		if (currentCookingProcessVFXInstance != null)
		{
			Destroy(currentCookingProcessVFXInstance.gameObject);
			currentCookingProcessVFXInstance = null;
		}
		if (currentUnderPanFireVFXInstance != null)
		{
			Destroy(currentUnderPanFireVFXInstance.gameObject);
			currentUnderPanFireVFXInstance = null;
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
			return false; // No recipe, so no fire either
		}

		currentRecipe = FindMatchingRecipe(currentIngredientsData);

		if (currentRecipe != null)
		{
			isCooking = true;
			currentCookTimer = 0f;

			if (progressBar != null)
			{
				progressBar.minValue = 0;
				progressBar.maxValue = currentRecipe.cookingDuration;
				progressBar.value = 0;
				progressBar.gameObject.SetActive(true);
			}

			// Start cooking process VFX (e.g., sizzle on pan)
			if (cookingProcessVFXPrefab != null)
			{
				if (currentCookingProcessVFXInstance != null) Destroy(currentCookingProcessVFXInstance.gameObject); // Clear old one
				Vector3 spawnPos = (cookingProcessVFXSpawnPoint != null) ? cookingProcessVFXSpawnPoint.position : resultSpawnPoint.position;
				currentCookingProcessVFXInstance = Instantiate(cookingProcessVFXPrefab, spawnPos, cookingProcessVFXPrefab.transform.rotation, cookingProcessVFXSpawnPoint != null ? cookingProcessVFXSpawnPoint : transform);
				currentCookingProcessVFXInstance.Play();
			}

			// --- Start Under Pan Fire VFX ---
			if (underPanFireVFXPrefab != null && fireSpawnPoint != null)
			{
				if (currentUnderPanFireVFXInstance != null) Destroy(currentUnderPanFireVFXInstance.gameObject); // Clear old one
																												// Instantiate at the fireSpawnPoint, parented to it for stability if pan moves
				currentUnderPanFireVFXInstance = Instantiate(underPanFireVFXPrefab, fireSpawnPoint.position, fireSpawnPoint.rotation, fireSpawnPoint);
				currentUnderPanFireVFXInstance.Play();
				Debug.Log($"ApplianceStation [{gameObject.name}]: Started Under Pan Fire VFX.");
			}
			// ------------------------------

			if (audioSource != null && processStartSound != null) audioSource.PlayOneShot(processStartSound);
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
			// No matching recipe, ensure no fire starts
			ResetState(); // This will also stop any lingering fire VFX
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
		Debug.Log($"Auto-Cook Complete: {currentRecipe.recipeName} on {gameObject.name}!");

		if (audioSource != null && audioSource.isPlaying && audioSource.clip == processingLoopSound) audioSource.Stop();
		if (audioSource != null && processCompleteSound != null) audioSource.PlayOneShot(processCompleteSound);

		List<Pickupable> itemsToDestroy = new List<Pickupable>(ingredientsOnStation);
		foreach (Pickupable ingredient in itemsToDestroy) { if (ingredient != null) Destroy(ingredient.gameObject); }
		ingredientsOnStation.Clear();

		if (currentRecipe.resultPrefab != null)
		{
			Instantiate(currentRecipe.resultPrefab, resultSpawnPoint.position, resultSpawnPoint.rotation);
		}
		else { Debug.LogError($"Recipe '{currentRecipe.recipeName}' has no result prefab!"); }

		ResetState(); // This will stop VFX and sounds
	}

	private void ResetState() // Called on completion, or if StartAutoCooking fails to find a recipe
	{
		isCooking = false;
		// currentRecipe = null; // Keep for logs if needed, or nullify
		// currentCookTimer = 0f;

		if (progressBar != null) { progressBar.gameObject.SetActive(false); progressBar.value = 0; }

		// --- Stop Cooking Process VFX (Sizzle) ---
		if (currentCookingProcessVFXInstance != null)
		{
			currentCookingProcessVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			Destroy(currentCookingProcessVFXInstance.gameObject, currentCookingProcessVFXInstance.main.startLifetime.constantMax + 0.5f);
			currentCookingProcessVFXInstance = null;
		}
		// -----------------------------------------

		// --- Stop Under Pan Fire VFX ---
		if (currentUnderPanFireVFXInstance != null)
		{
			currentUnderPanFireVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			// Use particle system's own duration for a more accurate destroy delay
			float fireDestroyDelay = currentUnderPanFireVFXInstance.main.duration + currentUnderPanFireVFXInstance.main.startLifetime.constantMax;
			Destroy(currentUnderPanFireVFXInstance.gameObject, fireDestroyDelay + 0.1f); // Add small buffer
			currentUnderPanFireVFXInstance = null;
			Debug.Log($"ApplianceStation [{gameObject.name}]: Stopped Under Pan Fire VFX.");
		}
		// -----------------------------

		if (audioSource != null && audioSource.isPlaying && audioSource.clip == processingLoopSound)
		{
			audioSource.Stop();
			audioSource.loop = false;
		}

		currentRecipe = null; // Nullify after use
		currentCookTimer = 0f;
	}

	private RecipeData FindMatchingRecipe(List<IngredientData> currentIngredients)
	{
		foreach (RecipeData recipe in availableRecipes)
		{
			if (DoIngredientsMatch(recipe.requiredIngredients, currentIngredients)) return recipe;
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
			if (!currentCounts.ContainsKey(kvp.Key) || currentCounts[kvp.Key] != kvp.Value) return false;
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