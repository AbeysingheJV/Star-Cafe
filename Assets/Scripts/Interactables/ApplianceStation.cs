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
	[SerializeField] private string specificCookActionName = "Fry"; // Default action verb for this appliance type

	[Header("VFX")]
	[SerializeField] private ParticleSystem cookingProcessVFXPrefab;
	[SerializeField] private Transform cookingProcessVFXSpawnPoint;
	[SerializeField] private ParticleSystem underPanFireVFXPrefab;
	[SerializeField] private Transform fireSpawnPoint;

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

	private ParticleSystem currentCookingProcessVFXInstance = null;
	private ParticleSystem currentUnderPanFireVFXInstance = null;

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

	void Update()
	{
		if (isCooking) UpdateAutoCooking(Time.deltaTime);
		if (progressBar != null && progressBar.gameObject.activeSelf) UpdateProgressBarTransform();
	}

	void OnDestroy()
	{
		StopAllVFX();
		if (audioSource != null && audioSource.isPlaying && audioSource.loop) audioSource.Stop();
	}

	private void StopAllVFX()
	{
		if (currentCookingProcessVFXInstance != null) { Destroy(currentCookingProcessVFXInstance.gameObject); currentCookingProcessVFXInstance = null; }
		if (currentUnderPanFireVFXInstance != null) { Destroy(currentUnderPanFireVFXInstance.gameObject); currentUnderPanFireVFXInstance = null; }
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
		if (p != null && ingredientsOnStation.Contains(p)) ingredientsOnStation.Remove(p);
	}

	// Public property for PickupController to check status
	public bool IsCooking => isCooking;

	// Method for PickupController to get interaction info (primarily its boolean return value now)
	public virtual bool GetCookActionInfo(out string actionName)
	{
		if (isCooking)
		{
			actionName = ""; // No prompt text if busy (PickupController handles this via IsCooking check)
			return false;
		}

		List<Pickupable> currentStationItems = new List<Pickupable>(ingredientsOnStation);
		currentStationItems.RemoveAll(item => item == null || item.gameObject == null || (item.Rb != null && item.Rb.isKinematic));
		List<IngredientData> currentIngredientsData = currentStationItems.Where(p => p != null && p.ingredientData != null).Select(p => p.ingredientData).ToList();

		RecipeData recipe = FindMatchingRecipe(currentIngredientsData);

		if (recipe != null)
		{
			actionName = specificCookActionName; // Still set it, though PickupController might override UI text
			return true; // Ready to cook this recipe
		}
		actionName = ""; // No action available
		return false; // No recipe matched or no ingredients
	}

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

	private void UpdateAutoCooking(float deltaTime)
	{
		if (!isCooking || currentRecipe == null) return;
		currentCookTimer += deltaTime;
		if (progressBar != null) progressBar.value = currentCookTimer;
		if (currentCookTimer >= currentRecipe.cookingDuration) CompleteCooking();
	}

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

	protected virtual RecipeData FindMatchingRecipe(List<IngredientData> currentIngredients)
	{
		if (currentIngredients == null || currentIngredients.Count == 0) return null;
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
		foreach (var kvp in requiredCounts) { if (!currentCounts.ContainsKey(kvp.Key) || currentCounts[kvp.Key] != kvp.Value) return false; }
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

	private void StopCookingProcessVFX()
	{
		if (currentCookingProcessVFXInstance != null)
		{
			currentCookingProcessVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			Destroy(currentCookingProcessVFXInstance.gameObject, currentCookingProcessVFXInstance.main.startLifetime.constantMax + 0.5f);
			currentCookingProcessVFXInstance = null;
		}
	}

	private void PlayUnderPanFireVFX()
	{
		if (underPanFireVFXPrefab != null && fireSpawnPoint != null)
		{
			if (currentUnderPanFireVFXInstance != null) Destroy(currentUnderPanFireVFXInstance.gameObject);
			currentUnderPanFireVFXInstance = Instantiate(underPanFireVFXPrefab, fireSpawnPoint.position, fireSpawnPoint.rotation, fireSpawnPoint);
			currentUnderPanFireVFXInstance.Play();
		}
	}

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