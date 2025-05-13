using UnityEngine;

public class PickupController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private PlayerInputHandler inputHandler;
	[SerializeField] private Camera mainCamera;
	[SerializeField] private Transform holdPoint;

	[Header("Interaction Parameters")]
	[SerializeField] private float interactionDistance = 3f;
	[SerializeField] private LayerMask pickupLayerMask;
	[SerializeField] private LayerMask raycastIgnoreLayerMask; // Keep if used, though not in current logic
	[SerializeField] private LayerMask cookingStationLayerMask;
	[SerializeField] private LayerMask ingredientSourceLayerMask;

	[Header("Holding Parameters")]
	[SerializeField] private float positionLerpSpeed = 15f;
	[SerializeField] private float rotationLerpSpeed = 15f;

	[Header("Audio")]
	[SerializeField] private AudioClip pickupSound;
	[SerializeField] private AudioClip dropSound;

	private Pickupable currentlyHeldItem = null;
	private Rigidbody heldItemRigidbody = null;
	private bool originalGravityState;
	private Collider playerCollider;
	private int pickupableLayerInt = -1;

	private CookingStation activeCookingStation = null; // For Hold-to-Cook
														// Removed activeApplianceStation as ApplianceStation is press-once

	private AudioSource audioSource;

	void Awake()
	{
		if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
		if (mainCamera == null) mainCamera = Camera.main;
		playerCollider = GetComponent<Collider>();
		audioSource = GetComponent<AudioSource>(); // Get the AudioSource component

		if (inputHandler == null || mainCamera == null || holdPoint == null) { enabled = false; return; }
		if (pickupLayerMask == 0) { Debug.LogWarning("PickupLayerMask not set on PickupController.", this); }
		if (cookingStationLayerMask == 0) { Debug.LogWarning("CookingStationLayerMask not set on PickupController.", this); }
		if (ingredientSourceLayerMask == 0) { Debug.LogWarning("IngredientSourceLayerMask not set on PickupController.", this); }

		pickupableLayerInt = LayerMask.NameToLayer("Pickupable");
		if (pickupableLayerInt == -1) { Debug.LogError("Pickupable layer does not exist! Please create it.", this); }
		if (audioSource == null) { Debug.LogWarning("PickupController requires an AudioSource component on the same GameObject.", this); }
	}

	void OnEnable()
	{
		if (inputHandler != null)
		{
			inputHandler.OnInteractActionStarted += HandleInteraction;
			inputHandler.OnCookActionStarted += HandleCookActionStarted;
			inputHandler.OnCookActionCanceled += HandleCookActionCanceled;
		}
	}

	void OnDisable()
	{
		if (inputHandler != null)
		{
			inputHandler.OnInteractActionStarted -= HandleInteraction;
			inputHandler.OnCookActionStarted -= HandleCookActionStarted;
			inputHandler.OnCookActionCanceled -= HandleCookActionCanceled;
		}
		if (currentlyHeldItem != null) DropItem(false); // Pass false as we don't want sound on disable

		if (activeCookingStation != null)
		{
			activeCookingStation.CancelHoldToCook();
			activeCookingStation = null;
		}
	}

	void FixedUpdate()
	{
		if (currentlyHeldItem != null && heldItemRigidbody != null)
		{
			MoveHeldItemSmoothly();
		}
	}

	private void HandleInteraction()
	{
		if (currentlyHeldItem != null)
		{
			DropItem(true); // Play sound on manual drop
		}
		else
		{
			if (!TrySpawnFromSource())
			{
				TryPickupExistingItem();
			}
		}
	}

	private void HandleCookActionStarted()
	{
		if (currentlyHeldItem != null) { return; } // Can't cook if holding something

		activeCookingStation = null; // Reset just in case

		RaycastHit hit;
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, cookingStationLayerMask))
		{
			ApplianceStation appliance = hit.collider.GetComponent<ApplianceStation>();
			if (appliance != null)
			{
				appliance.StartAutoCooking(); // This is a press-once action
				return;
			}

			CookingStation station = hit.collider.GetComponent<CookingStation>();
			if (station != null)
			{
				if (station.StartHoldToCook()) // Returns true if hold started
				{
					activeCookingStation = station; // Track it for cancellation on Q release
				}
			}
		}
	}

	private void HandleCookActionCanceled()
	{
		if (activeCookingStation != null)
		{
			activeCookingStation.CancelHoldToCook();
			activeCookingStation = null;
		}
	}

	private bool TrySpawnFromSource()
	{
		RaycastHit hit;
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, ingredientSourceLayerMask))
		{
			IngredientSource source = hit.collider.GetComponent<IngredientSource>();
			if (source != null)
			{
				GameObject prefabToSpawn = source.GetIngredientPrefab();
				if (prefabToSpawn != null)
				{
					SpawnAndHoldItem(prefabToSpawn);
					return true;
				}
			}
		}
		return false;
	}

	private void SpawnAndHoldItem(GameObject itemPrefab)
	{
		GameObject newItemGO = Instantiate(itemPrefab, holdPoint.position, holdPoint.rotation);
		currentlyHeldItem = newItemGO.GetComponent<Pickupable>();
		heldItemRigidbody = newItemGO.GetComponent<Rigidbody>();
		Collider newItemCollider = newItemGO.GetComponent<Collider>();

		if (currentlyHeldItem == null || heldItemRigidbody == null || newItemCollider == null)
		{
			Destroy(newItemGO); currentlyHeldItem = null; heldItemRigidbody = null; return;
		}

		originalGravityState = heldItemRigidbody.useGravity;
		heldItemRigidbody.useGravity = false;
		heldItemRigidbody.isKinematic = true;
		if (playerCollider != null) { Physics.IgnoreCollision(playerCollider, newItemCollider, true); }

		// Play pickup sound
		if (audioSource != null && pickupSound != null)
		{
			audioSource.PlayOneShot(pickupSound);
		}
	}

	private void TryPickupExistingItem()
	{
		RaycastHit hit;
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, pickupLayerMask))
		{
			Pickupable itemToPickup = hit.collider.GetComponent<Pickupable>();
			if (itemToPickup != null && itemToPickup.Rb != null && !itemToPickup.Rb.isKinematic) // Ensure it's not already held or static
			{
				GrabExistingItem(itemToPickup, hit.collider);
			}
		}
	}

	private void GrabExistingItem(Pickupable item, Collider itemCollider)
	{
		Rigidbody itemRb = item.Rb;
		currentlyHeldItem = item;
		heldItemRigidbody = itemRb;

		if (heldItemRigidbody != null)
		{
			originalGravityState = heldItemRigidbody.useGravity;
			heldItemRigidbody.useGravity = false;
			heldItemRigidbody.isKinematic = true;
			currentlyHeldItem.gameObject.layer = pickupableLayerInt; // Ensure it's on the correct layer if we change it
			if (playerCollider != null && itemCollider != null) { Physics.IgnoreCollision(playerCollider, itemCollider, true); }

			// Play pickup sound
			if (audioSource != null && pickupSound != null)
			{
				audioSource.PlayOneShot(pickupSound);
			}
		}
		else
		{
			currentlyHeldItem = null;
		}
	}

	private void DropItem(bool playSound)
	{
		if (currentlyHeldItem == null || heldItemRigidbody == null) return;

		Collider itemCollider = currentlyHeldItem.GetComponent<Collider>();
		heldItemRigidbody.isKinematic = false;
		heldItemRigidbody.useGravity = originalGravityState;

		if (playerCollider != null && itemCollider != null) { Physics.IgnoreCollision(playerCollider, itemCollider, false); }
		// We don't change layer back here, assuming it stays Pickupable or gets handled by station

		if (playSound && audioSource != null && dropSound != null)
		{
			audioSource.PlayOneShot(dropSound);
		}

		currentlyHeldItem = null;
		heldItemRigidbody = null;
	}

	private void MoveHeldItemSmoothly()
	{
		Vector3 targetPosition = holdPoint.position;
		Quaternion targetRotation = holdPoint.rotation;

		Vector3 smoothedPosition = Vector3.Lerp(heldItemRigidbody.position, targetPosition, Time.fixedDeltaTime * positionLerpSpeed);
		heldItemRigidbody.MovePosition(smoothedPosition);

		Quaternion smoothedRotation = Quaternion.Slerp(heldItemRigidbody.rotation, targetRotation, Time.fixedDeltaTime * rotationLerpSpeed);
		heldItemRigidbody.MoveRotation(smoothedRotation);
	}
}