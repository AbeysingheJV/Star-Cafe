using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

// Requires a Collider component to be attached.
[RequireComponent(typeof(Collider))]
public class CookingStation : MonoBehaviour
{
	[Header("Setup")]
	[SerializeField] private List<RecipeData> availableRecipes; // Recipes this station can prepare.
	[SerializeField] private Transform resultSpawnPoint; // Where the prepared dish appears.
	[SerializeField] private Slider progressBar; // UI slider for processing progress.
	[SerializeField] private string specificProcessActionName = "Prepare Food"; // Action name for UI (e.g., "Chop").

	[Header("VFX")]
	[SerializeField] private ParticleSystem stationProcessingVFXPrefab; // VFX when station is processing.
	[SerializeField] private ParticleSystem playerHoldActionVFXPrefab; // VFX when player holds action key.

	[Header("Audio")]
	[SerializeField] private AudioClip processStartSound; // Sound when processing starts.
	[SerializeField] private AudioClip processingLoopSound; // Looping sound during processing.
	[SerializeField] private AudioClip processCompleteSound; // Sound when processing finishes.

	[Header("State (Read Only)")]
	[SerializeField] private List<Pickupable> ingredientsOnStation = new List<Pickupable>(); // Ingredients currently on station.

	private bool isProcessing = false; // Is the station currently processing ingredients?
	private float currentProcessTimer = 0f; // Timer for the current processing.
	private RecipeData currentRecipe = null; // The recipe currently being processed.
	private Collider triggerCollider; // The station's trigger collider.
	private AudioSource audioSource; // Component for playing sounds.

	private ParticleSystem currentStationProcessingVFXInstance = null; // Instance of station's processing VFX.
	private ParticleSystem currentPlayerHoldActionVFXInstance = null; // Instance of player's hold action VFX.

	// Public property to check if the station is currently processing.
	public bool IsProcessing => isProcessing;

	// Called when the script instance is being loaded.
	void Awake()
	{
		triggerCollider = GetComponent<Collider>();
		audioSource = GetComponent<AudioSource>();

		if (!triggerCollider.isTrigger) { Debug.LogWarning($"Collider on {gameObject.name} not 'Is Trigger'.", this); }
		if (resultSpawnPoint == null) { Debug.LogWarning($"{gameObject.name} missing Result Spawn Point!", this); resultSpawnPoint = transform; }
		if (availableRecipes == null || availableRecipes.Count == 0) { Debug.LogWarning($"{gameObject.name} has no recipes!", this); }
		if (progressBar != null) { progressBar.gameObject.SetActive(false); progressBar.value = 0; }
		if (audioSource == null) { Debug.LogWarning($"CookingStation on {gameObject.name} is missing an AudioSource component for sounds!", this); }
		if (playerHoldActionVFXPrefab == null) { Debug.LogWarning($"CookingStation on {gameObject.name} is missing its Player Hold Action VFX Prefab assignment (starburst)!", this); }
	}

	// Called every frame.
	void Update()
	{
		if (isProcessing) UpdateHoldToProcess(Time.deltaTime);
		if (progressBar != null && progressBar.gameObject.activeSelf) UpdateProgressBarTransform();
	}

	// Called when the GameObject is being destroyed.
	void OnDestroy()
	{
		if (currentStationProcessingVFXInstance != null) Destroy(currentStationProcessingVFXInstance.gameObject);
		if (currentPlayerHoldActionVFXInstance != null) Destroy(currentPlayerHoldActionVFXInstance.gameObject);
		if (audioSource != null && audioSource.isPlaying && audioSource.loop) audioSource.Stop();
	}

	// Called when another Collider enters this GameObject's trigger.
	void OnTriggerEnter(Collider other)
	{
		Pickupable p = other.GetComponent<Pickupable>();
		if (p != null && p.ingredientData != null && !ingredientsOnStation.Contains(p) && p.Rb != null && !p.Rb.isKinematic)
		{
			ingredientsOnStation.Add(p);
		}
	}

	// Called when another Collider exits this GameObject's trigger.
	void OnTriggerExit(Collider other)
	{
		Pickupable p = other.GetComponent<Pickupable>();
		if (p != null && ingredientsOnStation.Contains(p)) ingredientsOnStation.Remove(p);
	}

	// Determines if the player can interact and provides an action name.
	public virtual bool GetCookActionInfo(out string actionName)
	{
		if (isProcessing)
		{
			actionName = "";
			return false;
		}

		List<Pickupable> currentStationItems = new List<Pickupable>(ingredientsOnStation);
		currentStationItems.RemoveAll(item => item == null || item.gameObject == null || (item.Rb != null && item.Rb.isKinematic));
		List<IngredientData> currentIngredientsData = currentStationItems.Where(p => p != null && p.ingredientData != null).Select(p => p.ingredientData).ToList();

		RecipeData recipe = FindMatchingRecipe(currentIngredientsData);

		if (recipe != null)
		{
			actionName = specificProcessActionName;
			return true;
		}
		actionName = "";
		return false;
	}

	// Starts the hold-to-cook process if a valid recipe is matched.
	public bool StartHoldToCook()
	{
		if (isProcessing)
		{
			if (currentPlayerHoldActionVFXInstance == null && playerHoldActionVFXPrefab != null) PlayPlayerHoldActionVFX();
			return false;
		}

		ingredientsOnStation.RemoveAll(item => item == null || item.gameObject == null || (item.Rb != null && item.Rb.isKinematic));
		List<IngredientData> currentIngredientsData = ingredientsOnStation.Where(p => p != null && p.ingredientData != null).Select(p => p.ingredientData).ToList();
		currentRecipe = FindMatchingRecipe(currentIngredientsData);

		if (currentRecipe != null)
		{
			isProcessing = true;
			currentProcessTimer = 0f;

			if (progressBar != null) { progressBar.minValue = 0; progressBar.maxValue = currentRecipe.cookingDuration; progressBar.value = 0; progressBar.gameObject.SetActive(true); }

			PlayStationProcessingVFX();
			PlayPlayerHoldActionVFX();

			if (audioSource != null && processStartSound != null) audioSource.PlayOneShot(processStartSound);
			if (audioSource != null && processingLoopSound != null) { audioSource.clip = processingLoopSound; audioSource.loop = true; audioSource.Play(); }
			return true;
		}
		else
		{
			StopPlayerHoldActionVFX();
			return false;
		}
	}

	// Cancels the hold-to-cook process (e.g., if player releases key).
	public void CancelHoldToCook()
	{
		bool wasProcessing = isProcessing;
		isProcessing = false;

		StopPlayerHoldActionVFX();
		StopStationProcessingVFX();

		if (progressBar != null) { progressBar.gameObject.SetActive(false); progressBar.value = 0; }

		if (audioSource != null && audioSource.isPlaying && audioSource.clip == processingLoopSound)
		{
			audioSource.Stop();
			audioSource.loop = false;
		}

		if (wasProcessing)
		{
			currentRecipe = null;
			currentProcessTimer = 0f;
		}
	}

	// Plays the VFX associated with the player holding an action at this station.
	private void PlayPlayerHoldActionVFX()
	{
		if (playerHoldActionVFXPrefab != null)
		{
			if (currentPlayerHoldActionVFXInstance == null)
			{
				Vector3 effectPosition = (resultSpawnPoint != null) ? resultSpawnPoint.position : transform.position;
				Quaternion vfxRotation = Quaternion.LookRotation(Vector3.up, transform.up);
				currentPlayerHoldActionVFXInstance = Instantiate(playerHoldActionVFXPrefab, effectPosition, vfxRotation, transform);
				currentPlayerHoldActionVFXInstance.Play();
			}
		}
	}

	// Stops the player's hold action VFX.
	private void StopPlayerHoldActionVFX()
	{
		if (currentPlayerHoldActionVFXInstance != null)
		{
			currentPlayerHoldActionVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			float destroyDelay = currentPlayerHoldActionVFXInstance.main.duration + currentPlayerHoldActionVFXInstance.main.startLifetime.constantMax;
			Destroy(currentPlayerHoldActionVFXInstance.gameObject, destroyDelay + 0.1f);
			currentPlayerHoldActionVFXInstance = null;
		}
	}

	// Plays the VFX on the station itself during processing.
	private void PlayStationProcessingVFX()
	{
		if (stationProcessingVFXPrefab != null)
		{
			if (currentStationProcessingVFXInstance != null) Destroy(currentStationProcessingVFXInstance.gameObject);
			Vector3 effectPosition = (resultSpawnPoint != null) ? resultSpawnPoint.position : transform.position;
			currentStationProcessingVFXInstance = Instantiate(stationProcessingVFXPrefab, effectPosition, Quaternion.identity, transform);
			currentStationProcessingVFXInstance.Play();
		}
	}

	// Stops the station's processing VFX.
	private void StopStationProcessingVFX()
	{
		if (currentStationProcessingVFXInstance != null)
		{
			currentStationProcessingVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			Destroy(currentStationProcessingVFXInstance.gameObject, currentStationProcessingVFXInstance.main.startLifetime.constantMax + 0.5f);
			currentStationProcessingVFXInstance = null;
		}
	}

	// Updates the processing timer and progress bar while player holds action key.
	private void UpdateHoldToProcess(float deltaTime)
	{
		if (!isProcessing || currentRecipe == null) return;
		currentProcessTimer += deltaTime;
		if (progressBar != null) progressBar.value = currentProcessTimer;
		if (currentProcessTimer >= currentRecipe.cookingDuration) CompleteProcessing();
	}

	// Finalizes the processing: spawns result, cleans up ingredients and state.
	private void CompleteProcessing()
	{
		if (!isProcessing || currentRecipe == null) return;

		if (audioSource != null && audioSource.isPlaying && audioSource.clip == processingLoopSound) { audioSource.Stop(); audioSource.loop = false; }
		if (audioSource != null && processCompleteSound != null) audioSource.PlayOneShot(processCompleteSound);

		List<Pickupable> itemsToDestroy = new List<Pickupable>(ingredientsOnStation);
		foreach (Pickupable ingredient in itemsToDestroy) { if (ingredient != null) Destroy(ingredient.gameObject); }
		ingredientsOnStation.Clear();

		if (currentRecipe.resultPrefab != null) Instantiate(currentRecipe.resultPrefab, resultSpawnPoint.position, resultSpawnPoint.rotation);
		else Debug.LogError($"Recipe '{currentRecipe.recipeName}' has no result prefab!");

		CancelHoldToCook();

		isProcessing = false;
		currentRecipe = null;
		currentProcessTimer = 0f;
	}

	// Finds a matching recipe from available recipes based on current ingredients.
	protected virtual RecipeData FindMatchingRecipe(List<IngredientData> currentIngredients)
	{
		if (currentIngredients == null || currentIngredients.Count == 0) return null;
		foreach (RecipeData recipe in availableRecipes)
		{
			if (DoIngredientsMatch(recipe.requiredIngredients, currentIngredients)) return recipe;
		}
		return null;
	}

	// Checks if the current ingredients exactly match the required ingredients for a recipe.
	private bool DoIngredientsMatch(List<IngredientData> required, List<IngredientData> current)
	{
		if (required == null || current == null || required.Count != current.Count || required.Count == 0) return false;
		var requiredCounts = required.Where(d => d != null).GroupBy(d => d.ingredientID).ToDictionary(g => g.Key, g => g.Count());
		var currentCounts = current.Where(d => d != null).GroupBy(d => d.ingredientID).ToDictionary(g => g.Key, g => g.Count());
		if (requiredCounts.Count != currentCounts.Count) return false;
		foreach (var kvp in requiredCounts) { if (!currentCounts.ContainsKey(kvp.Key) || currentCounts[kvp.Key] != kvp.Value) return false; }
		return true;
	}

	// Updates the progress bar's transform to always face the camera (for world-space UI).
	private void UpdateProgressBarTransform()
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