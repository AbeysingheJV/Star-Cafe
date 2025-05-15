using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Collider))]
public class CookingStation : MonoBehaviour
{
	[Header("Setup")]
	[SerializeField] private List<RecipeData> availableRecipes;
	[SerializeField] private Transform resultSpawnPoint; // Also good for VFX spawn point
	[SerializeField] private Slider progressBar;

	[Header("VFX")]
	[SerializeField] private ParticleSystem stationProcessingVFXPrefab; // For continuous effect during timer (optional)
	[SerializeField] private ParticleSystem playerHoldActionVFXPrefab; // Assign your starburst VFX here

	[Header("Audio")]
	[SerializeField] private AudioClip processStartSound;
	[SerializeField] private AudioClip processingLoopSound;
	[SerializeField] private AudioClip processCompleteSound;

	[Header("State (Read Only)")]
	[SerializeField] private List<Pickupable> ingredientsOnStation = new List<Pickupable>();

	private bool isProcessing = false; // Is the station's timer running?
	private float currentProcessTimer = 0f;
	private RecipeData currentRecipe = null;
	private Collider triggerCollider;
	private AudioSource audioSource;

	private ParticleSystem currentStationProcessingVFXInstance = null; // Instance of station's own timed VFX
	private ParticleSystem currentPlayerHoldActionVFXInstance = null; // Instance of the starburst VFX

	void Awake()
	{
		triggerCollider = GetComponent<Collider>();
		audioSource = GetComponent<AudioSource>();

		if (!triggerCollider.isTrigger) { Debug.LogWarning($"Collider on {gameObject.name} not 'Is Trigger'.", this); }
		if (resultSpawnPoint == null) { Debug.LogWarning($"{gameObject.name} missing Result Spawn Point! Using own transform as fallback for results/VFX.", this); resultSpawnPoint = transform; }
		if (availableRecipes == null || availableRecipes.Count == 0) { Debug.LogWarning($"{gameObject.name} has no recipes!", this); }
		if (progressBar != null) { progressBar.gameObject.SetActive(false); progressBar.value = 0; }
		if (audioSource == null) { Debug.LogWarning($"CookingStation on {gameObject.name} is missing an AudioSource component for sounds!", this); }
		if (playerHoldActionVFXPrefab == null) { Debug.LogWarning($"CookingStation on {gameObject.name} is missing its Player Hold Action VFX Prefab assignment! This VFX will not play.", this); }
	}

	void Update()
	{
		if (isProcessing) // Only if station's timer is active
		{
			UpdateHoldToProcess(Time.deltaTime);
		}
		if (progressBar != null && progressBar.gameObject.activeSelf)
		{
			UpdateProgressBarTransform();
		}
	}

	void OnDestroy()
	{
		if (currentStationProcessingVFXInstance != null)
		{
			Destroy(currentStationProcessingVFXInstance.gameObject);
		}
		if (currentPlayerHoldActionVFXInstance != null)
		{
			Destroy(currentPlayerHoldActionVFXInstance.gameObject);
		}

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

	// Called by PickupController when Q is PRESSED
	public bool StartHoldToCook()
	{
		if (isProcessing) // If station's timer is already running, don't restart.
		{
			// If player presses Q again while it's already processing,
			// ensure the hold VFX is playing if it somehow got stopped.
			// This scenario might be rare depending on PickupController logic.
			if (currentPlayerHoldActionVFXInstance == null)
			{
				PlayPlayerHoldActionVFX();
			}
			return false; // Indicate that the station was already processing.
		}

		ingredientsOnStation.RemoveAll(item => item == null || item.gameObject == null || (item.Rb != null && item.Rb.isKinematic));
		List<IngredientData> currentIngredientsData = ingredientsOnStation.Where(p => p != null && p.ingredientData != null).Select(p => p.ingredientData).ToList();

		currentRecipe = FindMatchingRecipe(currentIngredientsData);

		if (currentRecipe != null) // A valid recipe is present, so station timer will start
		{
			isProcessing = true; // Station's internal timer will start
			currentProcessTimer = 0f;

			if (progressBar != null)
			{
				progressBar.minValue = 0;
				progressBar.maxValue = currentRecipe.cookingDuration;
				progressBar.value = 0;
				progressBar.gameObject.SetActive(true);
			}

			// Start station's own continuous processing VFX (optional)
			if (stationProcessingVFXPrefab != null)
			{
				Vector3 effectPosition = (resultSpawnPoint != null) ? resultSpawnPoint.position : transform.position;
				currentStationProcessingVFXInstance = Instantiate(stationProcessingVFXPrefab, effectPosition, Quaternion.identity, transform);
				currentStationProcessingVFXInstance.Play();
			}

			// --- Play Player Hold Action VFX only if a recipe is matched and processing starts ---
			PlayPlayerHoldActionVFX();
			// ------------------------------------------------------------------------------------

			if (audioSource != null && processStartSound != null) audioSource.PlayOneShot(processStartSound);
			if (audioSource != null && processingLoopSound != null)
			{
				audioSource.clip = processingLoopSound;
				audioSource.loop = true;
				audioSource.Play();
			}
			return true; // Indicate that processing has started
		}
		else
		{
			// No valid recipe, so station timer does not start.
			// Player might still be holding Q, but the "playerHoldActionVFX" should not play.
			// Ensure it's stopped if it was somehow active from a previous interrupted state.
			StopPlayerHoldActionVFX(); // Make sure it's off if no recipe.
			return false; // Indicate that no processing started. PickupController might use this.
		}
	}

	// Called by PickupController when Q is RELEASED or if station's timer completes
	public void CancelHoldToCook()
	{
		// This method is called when Q is released OR when CompleteProcessing finishes.
		// It should stop all effects related to this station's active processing.

		bool wasProcessing = isProcessing; // Store if we were actively processing
		isProcessing = false; // Stop the station's internal timer/state immediately

		StopPlayerHoldActionVFX(); // Stop the hold action VFX regardless of previous state, as the hold is over.

		if (progressBar != null) { progressBar.gameObject.SetActive(false); progressBar.value = 0; }

		if (currentStationProcessingVFXInstance != null)
		{
			currentStationProcessingVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			Destroy(currentStationProcessingVFXInstance.gameObject, currentStationProcessingVFXInstance.main.startLifetime.constantMax + 0.5f);
			currentStationProcessingVFXInstance = null;
		}

		if (audioSource != null && audioSource.isPlaying && audioSource.clip == processingLoopSound)
		{
			audioSource.Stop();
			audioSource.loop = false;
		}

		// If it was processing and now cancelled, reset recipe and timer
		// This prevents CompleteProcessing from running again if cancelled early.
		if (wasProcessing)
		{
			currentRecipe = null;
			currentProcessTimer = 0f;
		}
	}

	private void PlayPlayerHoldActionVFX()
	{
		if (playerHoldActionVFXPrefab != null)
		{
			if (currentPlayerHoldActionVFXInstance == null) // Only instantiate if not already playing
			{
				Vector3 effectPosition = (resultSpawnPoint != null) ? resultSpawnPoint.position : transform.position;
				// Ensure VFX is oriented nicely, e.g., always facing up or matching station's up vector
				Quaternion vfxRotation = Quaternion.LookRotation(Vector3.up, transform.up); // Example: VFX "looks" upwards, aligned with station's up
																							// Or simply Quaternion.identity if the prefab is authored to look good without specific rotation
				currentPlayerHoldActionVFXInstance = Instantiate(playerHoldActionVFXPrefab, effectPosition, vfxRotation, transform); // Parent to station
				currentPlayerHoldActionVFXInstance.Play();
				Debug.Log($"CookingStation [{gameObject.name}]: Playing Player Hold Action VFX.");
			}
		}
	}

	private void StopPlayerHoldActionVFX()
	{
		if (currentPlayerHoldActionVFXInstance != null)
		{
			currentPlayerHoldActionVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			// Use the particle system's own main duration + startLifetime for a more accurate destroy delay
			float destroyDelay = currentPlayerHoldActionVFXInstance.main.duration + currentPlayerHoldActionVFXInstance.main.startLifetime.constantMax;
			Destroy(currentPlayerHoldActionVFXInstance.gameObject, destroyDelay + 0.1f); // Add small buffer
			currentPlayerHoldActionVFXInstance = null;
			Debug.Log($"CookingStation [{gameObject.name}]: Stopped Player Hold Action VFX.");
		}
	}

	private void UpdateHoldToProcess(float deltaTime)
	{
		if (!isProcessing || currentRecipe == null) return;
		currentProcessTimer += deltaTime;
		if (progressBar != null) { progressBar.value = currentProcessTimer; }
		if (currentProcessTimer >= currentRecipe.cookingDuration)
		{
			CompleteProcessing();
		}
	}

	private void CompleteProcessing()
	{
		if (!isProcessing || currentRecipe == null) return; // Should not happen if called from UpdateHoldToProcess

		Debug.Log($"Process Complete: {currentRecipe.recipeName} on {gameObject.name}!");

		// Stop this station's looping sound (if any) before playing complete sound
		if (audioSource != null && audioSource.isPlaying && audioSource.clip == processingLoopSound)
		{
			audioSource.Stop();
			audioSource.loop = false;
		}
		if (audioSource != null && processCompleteSound != null) audioSource.PlayOneShot(processCompleteSound);

		List<Pickupable> itemsToDestroy = new List<Pickupable>(ingredientsOnStation);
		foreach (Pickupable ingredient in itemsToDestroy) { if (ingredient != null) Destroy(ingredient.gameObject); }
		ingredientsOnStation.Clear();

		if (currentRecipe.resultPrefab != null)
		{
			Instantiate(currentRecipe.resultPrefab, resultSpawnPoint.position, resultSpawnPoint.rotation);
		}
		else { Debug.LogError($"Recipe '{currentRecipe.recipeName}' has no result prefab!"); }

		// This will also call StopPlayerHoldActionVFX()
		CancelHoldToCook(); // Resets station state, stops its VFX, progress bar, and player hold VFX.

		// Ensure these are reset after CancelHoldToCook has done its job with the current values if needed
		isProcessing = false;
		currentRecipe = null;
		currentProcessTimer = 0f;
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