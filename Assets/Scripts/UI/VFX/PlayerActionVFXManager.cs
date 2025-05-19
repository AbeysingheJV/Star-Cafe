using UnityEngine;

public class PlayerActionVFXManager : MonoBehaviour
{
	public static PlayerActionVFXManager Instance { get; private set; }

	[Header("VFX Settings")]
	[SerializeField] private ParticleSystem defaultHoldActionVFXPrefab; // Prefab for the hold action VFX.
	[SerializeField] private Vector3 vfxOffsetFromCamera = new Vector3(0f, -0.2f, 0.7f); // Offset from camera for VFX position.
	[SerializeField] private float vfxDestroyDelay = 2f; // Delay before destroying VFX after it stops.

	private ParticleSystem currentHoldActionVFXInstance; // Instance of the currently playing hold action VFX.
	private Camera mainCamera; // Reference to the main camera.

	// Called when the script instance is being loaded.
	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		mainCamera = Camera.main;
	}

	// Called when the GameObject is being destroyed.
	void OnDestroy()
	{
		StopHoldActionVFXImmediate();
		if (Instance == this)
		{
			Instance = null;
		}
	}

	// Plays the hold action visual effect.
	public void PlayHoldActionVFX()
	{
		if (defaultHoldActionVFXPrefab == null)
		{
			Debug.LogWarning("PlayerActionVFXManager: DefaultHoldActionVFXPrefab not assigned.");
			return;
		}

		if (currentHoldActionVFXInstance != null)
		{
			StopHoldActionVFXImmediate();
		}

		if (mainCamera == null)
		{
			Debug.LogError("PlayerActionVFXManager: Main Camera not found!");
			return;
		}

		Vector3 spawnPosition = mainCamera.transform.position +
								mainCamera.transform.forward * vfxOffsetFromCamera.z +
								mainCamera.transform.right * vfxOffsetFromCamera.x +
								mainCamera.transform.up * vfxOffsetFromCamera.y;

		currentHoldActionVFXInstance = Instantiate(defaultHoldActionVFXPrefab, spawnPosition, mainCamera.transform.rotation);
		currentHoldActionVFXInstance.transform.SetParent(mainCamera.transform, true);
		currentHoldActionVFXInstance.Play();
		Debug.Log("PlayerActionVFXManager: Playing Hold Action VFX.");
	}

	// Stops the hold action visual effect emission and schedules its destruction.
	public void StopHoldActionVFX()
	{
		if (currentHoldActionVFXInstance != null)
		{
			Debug.Log("PlayerActionVFXManager: Stopping Hold Action VFX emission.");
			currentHoldActionVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			currentHoldActionVFXInstance.transform.SetParent(null);
			Destroy(currentHoldActionVFXInstance.gameObject, vfxDestroyDelay);
			currentHoldActionVFXInstance = null;
		}
	}

	// Stops and immediately destroys the hold action visual effect.
	private void StopHoldActionVFXImmediate()
	{
		if (currentHoldActionVFXInstance != null)
		{
			Destroy(currentHoldActionVFXInstance.gameObject);
			currentHoldActionVFXInstance = null;
		}
	}
}