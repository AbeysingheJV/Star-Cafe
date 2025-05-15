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
	[SerializeField] private LayerMask cookingStationLayerMask;
	[SerializeField] private LayerMask ingredientSourceLayerMask;
	[SerializeField] private LayerMask generalInteractableLayerMask;
	[SerializeField] private LayerMask catInteractableLayerMask;

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

	private CookingStation activeCookingStation = null; // Tracks the station we are "hold-cooking" on

	private AudioSource audioSource;

	void Awake()
	{
		if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
		if (mainCamera == null) mainCamera = Camera.main;
		playerCollider = GetComponent<Collider>();
		audioSource = GetComponent<AudioSource>();

		if (inputHandler == null || mainCamera == null || holdPoint == null) { enabled = false; return; }
		if (pickupLayerMask.value == 0) Debug.LogWarning("PickupLayerMask not set.", this);
		if (cookingStationLayerMask.value == 0) Debug.LogWarning("CookingStationLayerMask not set.", this);
		if (ingredientSourceLayerMask.value == 0) Debug.LogWarning("IngredientSourceLayerMask not set.", this);
		if (generalInteractableLayerMask.value == 0) Debug.LogWarning("GeneralInteractableLayerMask not set.", this);
		if (catInteractableLayerMask.value == 0) Debug.LogWarning("CatInteractableLayerMask not set.", this);

		pickupableLayerInt = LayerMask.NameToLayer("Pickupable");
		if (pickupableLayerInt == -1) { Debug.LogError("Pickupable layer does not exist!", this); }
		if (audioSource == null) { Debug.LogWarning("PickupController requires an AudioSource.", this); }
	}

	void OnEnable()
	{
		if (inputHandler != null)
		{
			inputHandler.OnInteractActionStarted += HandleInteraction;
			inputHandler.OnCookActionStarted += HandleCookActionStarted;     // Q Press
			inputHandler.OnCookActionCanceled += HandleCookActionCanceled;   // Q Release
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
		if (currentlyHeldItem != null) DropItem(false);

		if (activeCookingStation != null) // If player is disabled while holding Q on a station
		{
			activeCookingStation.CancelHoldToCook(); // This will stop the station's timer & its VFX
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

	private void HandleInteraction() // 'E' key
	{
		RaycastHit hit;
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, catInteractableLayerMask))
		{
			CatAI cat = hit.collider.GetComponent<CatAI>();
			if (cat != null) { cat.TriggerSitUpAndSound(); return; }
		}
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, generalInteractableLayerMask))
		{
			RadioInteractable radio = hit.collider.GetComponent<RadioInteractable>();
			if (radio != null) { radio.Interact(); return; }
		}
		if (currentlyHeldItem != null) DropItem(true);
		else { if (TrySpawnFromSource()) return; TryPickupExistingItem(); }
	}

	private void HandleCookActionStarted() // 'Q' key press
	{
		if (currentlyHeldItem != null) return; // Can't cook if holding something
		activeCookingStation = null; // Reset

		RaycastHit hit;
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, cookingStationLayerMask))
		{
			ApplianceStation appliance = hit.collider.GetComponent<ApplianceStation>();
			if (appliance != null)
			{
				appliance.StartAutoCooking(); // Appliance handles its own VFX
				return;
			}

			CookingStation station = hit.collider.GetComponent<CookingStation>();
			if (station != null)
			{
				// StartHoldToCook on the station will now handle its own "hold action" VFX
				if (station.StartHoldToCook())
				{
					activeCookingStation = station; // Track that we are "holding Q" on this station
				}
			}
		}
	}

	private void HandleCookActionCanceled() // 'Q' key release
	{
		if (activeCookingStation != null) // If we were "holding Q" on a CookingStation
		{
			activeCookingStation.CancelHoldToCook(); // This tells the station to stop its processes and VFX
			activeCookingStation = null;
		}
		// No action needed for ApplianceStation on Q release
	}

	// ... (rest of TrySpawnFromSource, SpawnAndHoldItem, TryPickupExistingItem, GrabExistingItem, DropItem, MoveHeldItemSmoothly methods remain the same)
	// Make sure to copy them from your previous working version if they are not included below.
	private bool TrySpawnFromSource()
	{
		RaycastHit hit;
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, ingredientSourceLayerMask))
		{
			IngredientSource source = hit.collider.GetComponent<IngredientSource>();
			if (source != null)
			{
				GameObject prefabToSpawn = source.GetIngredientPrefab();
				if (prefabToSpawn != null) { SpawnAndHoldItem(prefabToSpawn); return true; }
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
		if (currentlyHeldItem == null || heldItemRigidbody == null || newItemCollider == null) { Destroy(newItemGO); currentlyHeldItem = null; heldItemRigidbody = null; return; }
		originalGravityState = heldItemRigidbody.useGravity;
		heldItemRigidbody.useGravity = false; heldItemRigidbody.isKinematic = true;
		if (playerCollider != null) { Physics.IgnoreCollision(playerCollider, newItemCollider, true); }
		if (audioSource != null && pickupSound != null) { audioSource.PlayOneShot(pickupSound); }
	}
	private void TryPickupExistingItem()
	{
		RaycastHit hit;
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, pickupLayerMask))
		{
			Pickupable itemToPickup = hit.collider.GetComponent<Pickupable>();
			if (itemToPickup != null && itemToPickup.Rb != null && !itemToPickup.Rb.isKinematic) { GrabExistingItem(itemToPickup, hit.collider); }
		}
	}
	private void GrabExistingItem(Pickupable item, Collider itemCollider)
	{
		Rigidbody itemRb = item.Rb; currentlyHeldItem = item; heldItemRigidbody = itemRb;
		if (heldItemRigidbody != null)
		{
			originalGravityState = heldItemRigidbody.useGravity; heldItemRigidbody.useGravity = false; heldItemRigidbody.isKinematic = true;
			if (playerCollider != null && itemCollider != null) { Physics.IgnoreCollision(playerCollider, itemCollider, true); }
			if (audioSource != null && pickupSound != null) { audioSource.PlayOneShot(pickupSound); }
		}
		else { currentlyHeldItem = null; }
	}
	private void DropItem(bool playSound)
	{
		if (currentlyHeldItem == null || heldItemRigidbody == null) return;
		Collider itemCollider = currentlyHeldItem.GetComponent<Collider>();
		heldItemRigidbody.isKinematic = false; heldItemRigidbody.useGravity = originalGravityState;
		if (playerCollider != null && itemCollider != null) { Physics.IgnoreCollision(playerCollider, itemCollider, false); }
		if (playSound && audioSource != null && dropSound != null) { audioSource.PlayOneShot(dropSound); }
		currentlyHeldItem = null; heldItemRigidbody = null;
	}
	private void MoveHeldItemSmoothly()
	{
		if (currentlyHeldItem == null || heldItemRigidbody == null) return;
		Vector3 targetPosition = holdPoint.position; Quaternion targetRotation = holdPoint.rotation;
		Vector3 smoothedPosition = Vector3.Lerp(heldItemRigidbody.position, targetPosition, Time.fixedDeltaTime * positionLerpSpeed);
		heldItemRigidbody.MovePosition(smoothedPosition);
		Quaternion smoothedRotation = Quaternion.Slerp(heldItemRigidbody.rotation, targetRotation, Time.fixedDeltaTime * rotationLerpSpeed);
		heldItemRigidbody.MoveRotation(smoothedRotation);
	}
}