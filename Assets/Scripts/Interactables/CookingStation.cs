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
	[SerializeField] private string specificProcessActionName = "Prepare Food"; // Customizable action name

	[Header("VFX")]
	[SerializeField] private ParticleSystem stationProcessingVFXPrefab;
	[SerializeField] private ParticleSystem playerHoldActionVFXPrefab;

	[Header("Audio")]
	[SerializeField] private AudioClip processStartSound;
	[SerializeField] private AudioClip processingLoopSound;
	[SerializeField] private AudioClip processCompleteSound;

	[Header("State (Read Only)")]
	[SerializeField] private List<Pickupable> ingredientsOnStation = new List<Pickupable>();

	private bool isProcessing = false;
	private float currentProcessTimer = 0f;
	private RecipeData currentRecipe = null;
	private Collider triggerCollider;
	private AudioSource audioSource;

	private ParticleSystem currentStationProcessingVFXInstance = null;
	private ParticleSystem currentPlayerHoldActionVFXInstance = null;

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

	void Update()
	{
		if (isProcessing) UpdateHoldToProcess(Time.deltaTime);
		if (progressBar != null && progressBar.gameObject.activeSelf) UpdateProgressBarTransform();
	}

	void OnDestroy()
	{
		if (currentStationProcessingVFXInstance != null) Destroy(currentStationProcessingVFXInstance.gameObject);
		if (currentPlayerHoldActionVFXInstance != null) Destroy(currentPlayerHoldActionVFXInstance.gameObject);
		if (audioSource != null && audioSource.isPlaying && audioSource.loop) audioSource.Stop();
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
	public bool IsProcessing => isProcessing;

	// Method for PickupController to get interaction info (primarily its boolean return value now)
	public virtual bool GetCookActionInfo(out string actionName)
	{
		if (isProcessing)
		{
			actionName = ""; // No prompt text if busy (PickupController handles this via IsProcessing check)
			return false;
		}

		List<Pickupable> currentStationItems = new List<Pickupable>(ingredientsOnStation);
		currentStationItems.RemoveAll(item => item == null || item.gameObject == null || (item.Rb != null && item.Rb.isKinematic));
		List<IngredientData> currentIngredientsData = currentStationItems.Where(p => p != null && p.ingredientData != null).Select(p => p.ingredientData).ToList();

		RecipeData recipe = FindMatchingRecipe(currentIngredientsData);

		if (recipe != null)
		{
			actionName = specificProcessActionName; // Still set it, though PickupController might override UI text
			return true;
		}
		actionName = "";
		return false;
	}

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

	private void StopStationProcessingVFX()
	{
		if (currentStationProcessingVFXInstance != null)
		{
			currentStationProcessingVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			Destroy(currentStationProcessingVFXInstance.gameObject, currentStationProcessingVFXInstance.main.startLifetime.constantMax + 0.5f);
			currentStationProcessingVFXInstance = null;
		}
	}

	private void UpdateHoldToProcess(float deltaTime)
	{
		if (!isProcessing || currentRecipe == null) return;
		currentProcessTimer += deltaTime;
		if (progressBar != null) progressBar.value = currentProcessTimer;
		if (currentProcessTimer >= currentRecipe.cookingDuration) CompleteProcessing();
	}

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
}