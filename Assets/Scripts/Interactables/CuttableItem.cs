// CuttableItem.cs
using UnityEngine;
using UnityEngine.UI; // Needed for Slider

// Ensure PickupableItem exists if inheriting, or just RequireComponent if separate
[RequireComponent(typeof(PickupableItem))]
public class CuttableItem : MonoBehaviour
{
	[Header("Cutting Settings")]
	[Tooltip("The prefab to spawn when cutting is complete.")]
	[SerializeField] private GameObject cutPrefab;

	[Tooltip("How long the player needs to hold interact to cut.")]
	[SerializeField] private float cutTime = 2.0f;

	[Header("UI Feedback (Optional)")]
	[Tooltip("Assign a UI Slider to show cutting progress.")]
	[SerializeField] private Slider progressBar; // Assign in Inspector

	private float currentCutProgress = 0f;
	private bool isBeingCut = false;

	void Start()
	{
		// Initialize progress bar (hide it initially)
		if (progressBar != null)
		{
			progressBar.gameObject.SetActive(false);
			progressBar.minValue = 0;
			progressBar.maxValue = cutTime;
			progressBar.value = 0;
		}
	}

	// Called by the Interaction Controller when cutting starts
	public void StartCutting()
	{
		if (isBeingCut || cutPrefab == null) return; // Don't restart if already cutting or no prefab

		Debug.Log($"Started cutting {gameObject.name}");
		isBeingCut = true;
		currentCutProgress = 0f;

		if (progressBar != null)
		{
			progressBar.gameObject.SetActive(true);
			progressBar.value = 0;
		}
	}

	// Called by the Interaction Controller every frame while interact is held
	public bool UpdateCutting(float deltaTime)
	{
		if (!isBeingCut) return false;

		currentCutProgress += deltaTime;

		if (progressBar != null)
		{
			progressBar.value = currentCutProgress;
		}

		if (currentCutProgress >= cutTime)
		{
			CompleteCutting();
			return true; // Cutting completed
		}
		return false; // Cutting in progress
	}

	// Called by the Interaction Controller if interact is released early
	public void CancelCutting()
	{
		if (!isBeingCut) return;

		Debug.Log($"Canceled cutting {gameObject.name}");
		isBeingCut = false;
		currentCutProgress = 0f;

		if (progressBar != null)
		{
			progressBar.value = 0;
			progressBar.gameObject.SetActive(false);
		}
	}

	// Called internally or by UpdateCutting when time is reached
	private void CompleteCutting()
	{
		if (!isBeingCut || cutPrefab == null) return; // Prevent double execution

		Debug.Log($"Finished cutting {gameObject.name}");
		isBeingCut = false; // Stop further updates

		// Instantiate the cut version at the same position and rotation
		Instantiate(cutPrefab, transform.position, transform.rotation);

		if (progressBar != null)
		{
			progressBar.gameObject.SetActive(false); // Hide progress bar
		}


		// Destroy the original whole item
		Destroy(gameObject);
	}

	// Optional: Ensure progress bar is linked to this item's position if it's world-space
	void Update()
	{
		if (progressBar != null && progressBar.gameObject.activeSelf && progressBar.GetComponentInParent<Canvas>().renderMode == RenderMode.WorldSpace)
		{
			// Keep the world-space progress bar positioned near the item
			// This might need adjustment based on your specific UI setup
			// Example: Position it slightly above the item
			progressBar.transform.position = transform.position + Vector3.up * 0.5f; // Adjust offset as needed
																					 // Make it face the camera (optional)
			if (Camera.main != null)
			{
				progressBar.transform.LookAt(Camera.main.transform);
				progressBar.transform.Rotate(0, 180, 0); // Adjust if it faces away
			}
		}
	}


}