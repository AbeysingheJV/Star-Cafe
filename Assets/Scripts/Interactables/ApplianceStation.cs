using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

// Requires a Collider component to be attached to the same GameObject.
[RequireComponent(typeof(Collider))]
public class ApplianceStation : MonoBehaviour
{
	[Header("Setup")]
	[SerializeField] private List<RecipeData> availableRecipes; // Recipes this appliance can make.
	[SerializeField] private Transform resultSpawnPoint; // Where the cooked dish appears.
	[SerializeField] private Slider progressBar; // UI slider to show cooking progress.
	[SerializeField] private string specificCookActionName = "Fry"; // Action name for UI prompts (e.g., "Fry").

	[Header("VFX")]
	[SerializeField] private ParticleSystem cookingProcessVFXPrefab; // Particle effect for when cooking.
	[SerializeField] private Transform cookingProcessVFXSpawnPoint; // Where the cooking VFX appears.
	[SerializeField] private ParticleSystem underPanFireVFXPrefab; // Particle effect for fire under the appliance.
	[SerializeField] private Transform fireSpawnPoint; // Where the fire VFX appears.

	[Header("Audio")]
	[SerializeField] private AudioClip processStartSound; // Sound when cooking starts.
	[SerializeField] private AudioClip processingLoopSound; // Looping sound while cooking.
	[SerializeField] private AudioClip processCompleteSound; // Sound when cooking finishes.

	[Header("State (Read Only)")]
	[SerializeField] private List<Pickupable> ingredientsOnStation = new List<Pickupable>(); // Ingredients currently on the station.

	private bool isCooking = false; // Is the appliance currently cooking?
	private float currentCookTimer = 0f; // Timer for the current cooking process.
	private RecipeData currentRecipe = null; // The recipe currently being cooked.
	private Collider triggerCollider; // The station's trigger collider.
	private AudioSource audioSource; // Component to play audio.

	private ParticleSystem currentCookingProcessVFXInstance = null; // Instance of the cooking VFX.
	private ParticleSystem currentUnderPanFireVFXInstance = null; // Instance of the fire VFX.

	// Public property to check if the appliance is currently cooking.
	public bool IsCooking => isCooking;

	// Called when the script instance is being loaded.
	void Awake()
	{
		triggerCollider = GetComponent<Collider>();
		audioSource = GetComponent<AudioSource>();

		if (!triggerCollider.isTrigger) { Debug.LogWarning($"Collider on {gameObject.name} not 'Is Trigger'.", this); }
		if (resultSpawnPoint == null) { Debug.LogWarning($"{gameObject.name} missing Result Spawn Point! Using own transform as fallback.", this); resultSpawnPoint = transform; }
		if (cookingProcessVFXSpawnPoint == null) { cookingProcessVFXSpawnPoint = resultSpawnPoint; }
		if (fireSpawnPoint == null) { Debug.LogWarning($"ApplianceStation [{gameObject.name}]: Fire Spawn Point not assigned. Fire VFX may not appear correctly.", this); fireSpawnPoint = transform; }

		if (availableRecipes == null || availableRecipes.Count == 0) { Debug.LogWarning($"{gameObject.name} has no recipes!", this); }
		if (progressBar != null) { progressBar.gameObject.SetActive(false); progressBar.value = 0; }
		if (audioSource == null) { Debug.LogWarning($"ApplianceStation on {gameObject.name} is missing an AudioSource component for sounds!", this); }
		if (underPanFireVFXPrefab == null) { Debug.LogWarning($"ApplianceStation on {gameObject.name} is missing its Under Pan Fire VFX Prefab assignment!", this); }
	}

	// Called every frame.
	void Update()
	{
		if (isCooking) UpdateAutoCooking(Time.deltaTime);
		if (progressBar != null && progressBar.gameObject.activeSelf) UpdateProgressBarTransform();
	}

	// Called when the GameObject is being destroyed.
	void OnDestroy()
	{
		StopAllVFX();
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

	// Determines if the player can interact with this station and provides an action name.
	public virtual bool GetCookActionInfo(out string actionName)
	{
		if (isCooking)
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
			actionName = specificCookActionName;
			return true;
		}
		actionName = "";
		return false;
	}

	// Starts the automatic cooking process if a valid recipe is matched.
	public bool StartAutoCooking()
	{
		if (isCooking) return false;
		ingredientsOnStation.RemoveAll(item => item == null || item.gameObject == null || (item.Rb != null && item.Rb.isKinematic));
		List<IngredientData> currentIngredientsData = ingredientsOnStation.Where(p => p != null && p.ingredientData != null).Select(p => p.ingredientData).ToList();
		if (currentIngredientsData.Count == 0) return false;
		currentRecipe = FindMatchingRecipe(currentIngredientsData);

		if (currentRecipe != null)
		{
			isCooking = true;
			currentCookTimer = 0f;
			if (progressBar != null) { progressBar.minValue = 0; progressBar.maxValue = currentRecipe.cookingDuration; progressBar.value = 0; progressBar.gameObject.SetActive(true); }

			PlayCookingProcessVFX();
			PlayUnderPanFireVFX();

			if (audioSource != null && processStartSound != null) audioSource.PlayOneShot(processStartSound);
			if (audioSource != null && processingLoopSound != null) { audioSource.clip = processingLoopSound; audioSource.loop = true; audioSource.Play(); }
			return true;
		}
		ResetState();
		return false;
	}

	// Updates the cooking timer and progress bar each frame during auto-cooking.
	private void UpdateAutoCooking(float deltaTime)
	{
		if (!isCooking || currentRecipe == null) return;
		currentCookTimer += deltaTime;
		if (progressBar != null) progressBar.value = currentCookTimer;
		if (currentCookTimer >= currentRecipe.cookingDuration) CompleteCooking();
	}

	// Finalizes the cooking process: spawns result, cleans up ingredients and state.
	private void CompleteCooking()
	{
		if (!isCooking || currentRecipe == null) return;
		if (audioSource != null && audioSource.isPlaying && audioSource.clip == processingLoopSound) audioSource.Stop();
		if (audioSource != null && processCompleteSound != null) audioSource.PlayOneShot(processCompleteSound);
		List<Pickupable> itemsToDestroy = new List<Pickupable>(ingredientsOnStation);
		foreach (Pickupable ingredient in itemsToDestroy) { if (ingredient != null) Destroy(ingredient.gameObject); }
		ingredientsOnStation.Clear();
		if (currentRecipe.resultPrefab != null) Instantiate(currentRecipe.resultPrefab, resultSpawnPoint.position, resultSpawnPoint.rotation);
		else Debug.LogError($"Recipe '{currentRecipe.recipeName}' has no result prefab!");
		ResetState();
	}

	// Resets the station's cooking state and UI.
	private void ResetState()
	{
		isCooking = false;
		if (progressBar != null) { progressBar.gameObject.SetActive(false); progressBar.value = 0; }

		StopCookingProcessVFX();
		StopUnderPanFireVFX();

		if (audioSource != null && audioSource.isPlaying && audioSource.clip == processingLoopSound) { audioSource.Stop(); audioSource.loop = false; }

		currentRecipe = null;
		currentCookTimer = 0f;
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

	// Stops and destroys all active VFX instances for this station.
	private void StopAllVFX()
	{
		if (currentCookingProcessVFXInstance != null) { Destroy(currentCookingProcessVFXInstance.gameObject); currentCookingProcessVFXInstance = null; }
		if (currentUnderPanFireVFXInstance != null) { Destroy(currentUnderPanFireVFXInstance.gameObject); currentUnderPanFireVFXInstance = null; }
	}

	// Plays the particle effect for the cooking process.
	private void PlayCookingProcessVFX()
	{
		if (cookingProcessVFXPrefab != null)
		{
			if (currentCookingProcessVFXInstance != null) Destroy(currentCookingProcessVFXInstance.gameObject);
			Vector3 spawnPos = (cookingProcessVFXSpawnPoint != null) ? cookingProcessVFXSpawnPoint.position : resultSpawnPoint.position;
			Quaternion spawnRot = (cookingProcessVFXSpawnPoint != null) ? cookingProcessVFXSpawnPoint.rotation : cookingProcessVFXPrefab.transform.rotation;
			currentCookingProcessVFXInstance = Instantiate(cookingProcessVFXPrefab, spawnPos, spawnRot, cookingProcessVFXSpawnPoint != null ? cookingProcessVFXSpawnPoint : transform);
			currentCookingProcessVFXInstance.Play();
		}
	}

	// Stops the particle effect for the cooking process.
	private void StopCookingProcessVFX()
	{
		if (currentCookingProcessVFXInstance != null)
		{
			currentCookingProcessVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			Destroy(currentCookingProcessVFXInstance.gameObject, currentCookingProcessVFXInstance.main.startLifetime.constantMax + 0.5f);
			currentCookingProcessVFXInstance = null;
		}
	}

	// Plays the particle effect for the fire under the appliance.
	private void PlayUnderPanFireVFX()
	{
		if (underPanFireVFXPrefab != null && fireSpawnPoint != null)
		{
			if (currentUnderPanFireVFXInstance != null) Destroy(currentUnderPanFireVFXInstance.gameObject);
			currentUnderPanFireVFXInstance = Instantiate(underPanFireVFXPrefab, fireSpawnPoint.position, fireSpawnPoint.rotation, fireSpawnPoint);
			currentUnderPanFireVFXInstance.Play();
		}
	}

	// Stops the particle effect for the fire under the appliance.
	private void StopUnderPanFireVFX()
	{
		if (currentUnderPanFireVFXInstance != null)
		{
			currentUnderPanFireVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			float fireDestroyDelay = currentUnderPanFireVFXInstance.main.duration + currentUnderPanFireVFXInstance.main.startLifetime.constantMax;
			Destroy(currentUnderPanFireVFXInstance.gameObject, fireDestroyDelay + 0.1f);
			currentUnderPanFireVFXInstance = null;
		}
	}
}