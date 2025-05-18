using UnityEngine;
using TMPro; // Required for TextMeshPro UI elements

// Placeholder for a Book script - you'll need to create this if you want books to be readable
// public class ReadableBook : MonoBehaviour { public void Read() { Debug.Log("Reading book: " + gameObject.name); /* Implement reading UI */ } }
// Placeholder for RadioInteractable script
// public class RadioInteractable : MonoBehaviour { public void Interact() { Debug.Log("Interacting with Radio: " + gameObject.name); /* Implement radio logic */ } }


public class PickupController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private PlayerInputHandler inputHandler;
	[SerializeField] private Camera mainCamera;
	[SerializeField] private Transform holdPoint;

	[Header("Interaction Parameters")]
	[SerializeField] private float interactionDistance = 3f;
	[SerializeField] private LayerMask interactableLayerMask; // Assign ALL interactable layers here (Pickupable, IngredientSources, CatLayer, Radio, Book, Counter1, Counter2)

	[Header("UI Interaction Prompts (Assign in Inspector)")]
	[SerializeField] private TextMeshProUGUI eActionText;
	// Q Action Text is intentionally removed from UI display based on previous request

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

	private CookingStation activeCookingStation = null; // For hold-to-cook stations
	private AudioSource audioSource;

	private GameObject lastLookedAtGameObjectForUI = null;
	private string lastEText = "";

	// Layer integer values
	private int ingredientSourceLayerValue = -1;
	private int pickupableLayerValue = -1;
	private int catLayerValue = -1;
	private int counter1LayerValue = -1; // Still needed for Q-key ACTION
	private int counter2LayerValue = -1; // Still needed for Q-key ACTION
	private int radioLayerValue = -1;
	private int bookLayerValue = -1;

	void Awake()
	{
		Debug.Log("PickupController: Awake()");
		if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
		if (mainCamera == null) mainCamera = Camera.main;
		playerCollider = GetComponent<Collider>();
		audioSource = GetComponent<AudioSource>();

		if (inputHandler == null) Debug.LogError("PickupController: InputHandler not assigned/found!");
		if (mainCamera == null) Debug.LogError("PickupController: MainCamera not assigned/found!");
		if (holdPoint == null) Debug.LogError("PickupController: HoldPoint not assigned!");

		if (inputHandler == null || mainCamera == null || holdPoint == null)
		{
			Debug.LogError("PickupController: Essential references missing. Disabling script.");
			enabled = false;
			return;
		}

		if (eActionText == null) Debug.LogWarning("E_Action_Text not assigned in PickupController. E prompts will not appear.", this);

		if (interactableLayerMask.value == 0) Debug.LogWarning("InteractableLayerMask not set in PickupController. Interactions might not work.", this);

		// Get integer values for specific layers by name
		ingredientSourceLayerValue = LayerMask.NameToLayer("IngredientSources");
		pickupableLayerValue = LayerMask.NameToLayer("Pickupable");
		catLayerValue = LayerMask.NameToLayer("CatLayer");
		counter1LayerValue = LayerMask.NameToLayer("Counter1");
		counter2LayerValue = LayerMask.NameToLayer("Counter2");
		radioLayerValue = LayerMask.NameToLayer("Radio");
		bookLayerValue = LayerMask.NameToLayer("Book");

		// Log errors if layers aren't found
		if (ingredientSourceLayerValue == -1) Debug.LogError("PickupController: Layer 'IngredientSources' not found.");
		if (pickupableLayerValue == -1) Debug.LogError("PickupController: Layer 'Pickupable' not found. Pickup/Drop action might fail.");
		if (catLayerValue == -1) Debug.LogError("PickupController: Layer 'CatLayer' not found.");
		if (counter1LayerValue == -1) Debug.LogError("PickupController: Layer 'Counter1' not found. Q-key cook action might fail.");
		if (counter2LayerValue == -1) Debug.LogError("PickupController: Layer 'Counter2' not found. Q-key cook action might fail.");
		if (radioLayerValue == -1) Debug.LogError("PickupController: Layer 'Radio' not found.");
		if (bookLayerValue == -1) Debug.LogError("PickupController: Layer 'Book' not found.");

		if (audioSource == null) { Debug.LogWarning("PickupController requires an AudioSource component.", this); }

		if (eActionText != null) eActionText.text = "";
	}

	void OnEnable()
	{
		if (inputHandler != null)
		{
			inputHandler.OnInteractActionStarted += HandleInteractionInput;
			inputHandler.OnCookActionStarted += HandleCookActionInput;
			inputHandler.OnCookActionCanceled += HandleCookActionInputReleased;
		}
	}

	void OnDisable()
	{
		if (inputHandler != null)
		{
			inputHandler.OnInteractActionStarted -= HandleInteractionInput;
			inputHandler.OnCookActionStarted -= HandleCookActionInput;
			inputHandler.OnCookActionCanceled -= HandleCookActionInputReleased;
		}
		if (currentlyHeldItem != null) DropItem(false);
		if (activeCookingStation != null) { activeCookingStation.CancelHoldToCook(); activeCookingStation = null; }
		HideAllActionTexts();
	}

	void Update()
	{
		if (mainCamera == null) return;
		CheckForInteractableObjectForUI();
	}

	void FixedUpdate()
	{
		if (currentlyHeldItem != null && heldItemRigidbody != null) MoveHeldItemSmoothly();
	}

	private void CheckForInteractableObjectForUI()
	{
		RaycastHit hit;
		string currentEText = "";

		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, interactableLayerMask))
		{
			lastLookedAtGameObjectForUI = hit.collider.gameObject;
			int hitLayer = hit.collider.gameObject.layer;

			// E Key Interaction Prompts
			if (hitLayer == pickupableLayerValue)
			{
				currentEText = " Pickup/Drop"; // Always show "Pickup/Drop" for items on this layer
			}
			else if (currentlyHeldItem == null) // Only show other prompts if not holding anything
			{
				if (hitLayer == ingredientSourceLayerValue) { currentEText = " Grab item"; }
				else if (hitLayer == catLayerValue) { currentEText = " Pet"; }
				else if (hitLayer == radioLayerValue) { currentEText = " Change Track"; }
				else if (hitLayer == bookLayerValue) { currentEText = " Read"; }
			}
			// If holding an item and not looking at a Pickupable, currentEText remains "" (no E prompt)

			// Q Key Prompts are entirely removed from UI update
		}
		else
		{
			lastLookedAtGameObjectForUI = null;
		}

		if (eActionText != null && currentEText != lastEText)
		{
			eActionText.text = currentEText;
			lastEText = currentEText;
		}
	}

	private void HideAllActionTexts()
	{
		if (eActionText != null) eActionText.text = "";
		lastEText = "";
		lastLookedAtGameObjectForUI = null;
	}

	private void HandleInteractionInput() // E key pressed
	{
		// Perform a fresh raycast for the action itself to ensure we're acting on the correct object.
		RaycastHit hit;
		bool hitSomething = Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, interactableLayerMask);

		if (currentlyHeldItem != null)
		{
			DropItem(true); // If holding, E always drops.
			return;
		}

		// Not holding an item, try to interact with what we are looking at.
		if (hitSomething)
		{
			GameObject hitObject = hit.collider.gameObject;
			int hitLayer = hitObject.layer;

			// Priority 1: Pickupable items (even if on a counter)
			if (hitLayer == pickupableLayerValue)
			{
				Pickupable pickupable = hitObject.GetComponent<Pickupable>();
				if (pickupable != null && pickupable.Rb != null && !pickupable.Rb.isKinematic)
				{
					GrabExistingItem(pickupable, hit.collider);
					return;
				}
			}

			// Priority 2: Other specific interactions if not a direct pickup
			if (hitLayer == ingredientSourceLayerValue)
			{
				IngredientSource source = hitObject.GetComponent<IngredientSource>();
				if (source != null) { TrySpawnFromSource(source); return; }
			}
			else if (hitLayer == catLayerValue)
			{
				CatAI cat = hitObject.GetComponent<CatAI>();
				if (cat != null) { cat.TriggerSitUpAndSound(); return; }
			}
			else if (hitLayer == radioLayerValue)
			{
				RadioInteractable radio = hitObject.GetComponent<RadioInteractable>();
				if (radio != null) { radio.Interact(); return; }
			}
			else if (hitLayer == bookLayerValue)
			{
				Debug.Log($"Interacted with Book: {hitObject.name}. Implement book reading logic.");
				// Example: ReadableBook book = hitObject.GetComponent<ReadableBook>();
				// if (book != null) { book.Read(); }
				return;
			}
		}
	}

	private void HandleCookActionInput() // Q key pressed - FUNCTIONALITY REMAINS
	{
		if (currentlyHeldItem != null) return;

		RaycastHit hit;
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance,
			LayerMask.GetMask("Counter1", "Counter2")))
		{
			activeCookingStation = null;
			GameObject hitObject = hit.collider.gameObject;

			if (hitObject.layer == counter1LayerValue)
			{
				CookingStation station = hitObject.GetComponent<CookingStation>();
				if (station != null && !station.IsProcessing)
				{
					if (station.StartHoldToCook()) activeCookingStation = station;
				}
			}
			else if (hitObject.layer == counter2LayerValue)
			{
				ApplianceStation appliance = hitObject.GetComponent<ApplianceStation>();
				if (appliance != null && !appliance.IsCooking)
				{
					appliance.StartAutoCooking();
				}
			}
		}
	}

	private void HandleCookActionInputReleased() // FUNCTIONALITY REMAINS
	{
		if (activeCookingStation != null)
		{
			activeCookingStation.CancelHoldToCook();
			activeCookingStation = null;
		}
	}

	private bool TrySpawnFromSource(IngredientSource source)
	{
		if (source != null)
		{
			GameObject prefabToSpawn = source.GetIngredientPrefab();
			if (prefabToSpawn != null) { SpawnAndHoldItem(prefabToSpawn); return true; }
		}
		return false;
	}
	private void SpawnAndHoldItem(GameObject itemPrefab)
	{
		if (holdPoint == null) { Debug.LogError("HoldPoint is not assigned!"); return; }
		GameObject newItemGO = Instantiate(itemPrefab, holdPoint.position, holdPoint.rotation);
		currentlyHeldItem = newItemGO.GetComponent<Pickupable>();
		if (currentlyHeldItem == null) { Debug.LogError($"Spawned item {itemPrefab.name} is missing Pickupable script!"); Destroy(newItemGO); return; }

		heldItemRigidbody = newItemGO.GetComponent<Rigidbody>();
		if (heldItemRigidbody == null) { Debug.LogError($"Spawned item {itemPrefab.name} is missing Rigidbody!"); Destroy(newItemGO); currentlyHeldItem = null; return; }

		Collider newItemCollider = newItemGO.GetComponent<Collider>();
		if (newItemCollider == null) { Debug.LogWarning($"Spawned item {itemPrefab.name} is missing a Collider!"); }

		originalGravityState = heldItemRigidbody.useGravity;
		heldItemRigidbody.useGravity = false;
		heldItemRigidbody.isKinematic = true;

		if (playerCollider != null && newItemCollider != null) { Physics.IgnoreCollision(playerCollider, newItemCollider, true); }
		if (audioSource != null && pickupSound != null) { audioSource.PlayOneShot(pickupSound); }
		HideAllActionTexts();
	}
	private void GrabExistingItem(Pickupable item, Collider itemCollider)
	{
		if (item == null) return;
		currentlyHeldItem = item;
		heldItemRigidbody = item.Rb;

		if (heldItemRigidbody != null)
		{
			originalGravityState = heldItemRigidbody.useGravity;
			heldItemRigidbody.useGravity = false;
			heldItemRigidbody.isKinematic = true;
			if (playerCollider != null && itemCollider != null) { Physics.IgnoreCollision(playerCollider, itemCollider, true); }
			if (audioSource != null && pickupSound != null) { audioSource.PlayOneShot(pickupSound); }
			HideAllActionTexts();
		}
		else
		{
			Debug.LogError($"Attempted to grab {item.name} but it has no Rigidbody or Pickupable.Rb is not set!");
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
		if (playSound && audioSource != null && dropSound != null) { audioSource.PlayOneShot(dropSound); }

		currentlyHeldItem = null;
		heldItemRigidbody = null;
		// UI will update in the next CheckForInteractableObjectForUI call
	}
	private void MoveHeldItemSmoothly()
	{
		if (currentlyHeldItem == null || heldItemRigidbody == null || holdPoint == null) return;
		Vector3 targetPosition = holdPoint.position;
		Quaternion targetRotation = holdPoint.rotation;

		Vector3 smoothedPosition = Vector3.Lerp(heldItemRigidbody.position, targetPosition, Time.fixedDeltaTime * positionLerpSpeed);
		heldItemRigidbody.MovePosition(smoothedPosition);

		Quaternion smoothedRotation = Quaternion.Slerp(heldItemRigidbody.rotation, targetRotation, Time.fixedDeltaTime * rotationLerpSpeed);
		heldItemRigidbody.MoveRotation(smoothedRotation);
	}
}