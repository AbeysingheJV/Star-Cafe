using UnityEngine;

public class PlayerActionVFXManager : MonoBehaviour
{
	[Header("VFX Settings")]
	[SerializeField] private ParticleSystem defaultHoldActionVFXPrefab; // Assign your "HoldingActionVFX" prefab here
	[SerializeField] private Vector3 vfxOffsetFromCamera = new Vector3(0f, -0.2f, 0.7f); // Adjust as needed: X, Y, Z
	[SerializeField] private float vfxDestroyDelay = 2f; // Time after stopping emission to destroy the VFX object

	private ParticleSystem currentHoldActionVFXInstance;
	private Camera mainCamera;

	public static PlayerActionVFXManager Instance { get; private set; }

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		mainCamera = Camera.main; // Assuming your player's view camera is tagged "MainCamera"
	}

	public void PlayHoldActionVFX()
	{
		if (defaultHoldActionVFXPrefab == null)
		{
			Debug.LogWarning("PlayerActionVFXManager: DefaultHoldActionVFXPrefab not assigned.");
			return;
		}

		if (currentHoldActionVFXInstance != null) // If one is already playing, stop it first
		{
			StopHoldActionVFXImmediate();
		}

		if (mainCamera == null)
		{
			Debug.LogError("PlayerActionVFXManager: Main Camera not found!");
			return;
		}

		// Instantiate the VFX
		// Position it relative to the camera: camera's position + camera's forward vector * Z offset + camera's right * X offset + camera's up * Y offset
		Vector3 spawnPosition = mainCamera.transform.position +
								mainCamera.transform.forward * vfxOffsetFromCamera.z +
								mainCamera.transform.right * vfxOffsetFromCamera.x +
								mainCamera.transform.up * vfxOffsetFromCamera.y;

		currentHoldActionVFXInstance = Instantiate(defaultHoldActionVFXPrefab, spawnPosition, mainCamera.transform.rotation);

		// Parent to camera so it moves with camera view (optional, but good for first-person)
		// If your camera has a specific "VFX anchor point" child object, parent to that instead.
		currentHoldActionVFXInstance.transform.SetParent(mainCamera.transform, true); // Set to worldPositionStays = true

		currentHoldActionVFXInstance.Play();
		Debug.Log("PlayerActionVFXManager: Playing Hold Action VFX.");
	}

	public void StopHoldActionVFX()
	{
		if (currentHoldActionVFXInstance != null)
		{
			Debug.Log("PlayerActionVFXManager: Stopping Hold Action VFX emission.");
			currentHoldActionVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting); // Stop emitting, let existing particles fade

			// Detach from parent so it doesn't disappear if camera moves/is disabled quickly
			currentHoldActionVFXInstance.transform.SetParent(null);

			Destroy(currentHoldActionVFXInstance.gameObject, vfxDestroyDelay);
			currentHoldActionVFXInstance = null;
		}
	}

	// Used if we need to immediately clear an old one before starting a new one
	private void StopHoldActionVFXImmediate()
	{
		if (currentHoldActionVFXInstance != null)
		{
			Destroy(currentHoldActionVFXInstance.gameObject);
			currentHoldActionVFXInstance = null;
		}
	}

	// Ensure VFX is stopped if this manager is destroyed
	void OnDestroy()
	{
		StopHoldActionVFXImmediate();
		if (Instance == this)
		{
			Instance = null;
		}
	}
}