using UnityEngine;
using TMPro; // Required for TextMeshPro UI elements

// Placeholder for a Book script - you'll need to create this if you want books to be readable
// public class ReadableBook : MonoBehaviour { public void InteractWithBook() { Debug.Log("Reading book: " + gameObject.name); /* Implement reading UI */ } }
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
	[SerializeField] private LayerMask interactableLayerMask; // Assign ALL interactable layers here

	[Header("UI Interaction Prompts (Assign in Inspector)")]
	[SerializeField] private TextMeshProUGUI eActionText;
	[SerializeField] private TextMeshProUGUI qActionText;

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
	private string lastQText = "";

	// Layer integer values
	private int ingredientSourceLayerValue = -1;
	private int pickupableLayerValue = -1;
	private int catLayerValue = -1;
	private int counter1LayerValue = -1; // For Cooking Stations
	private int counter2LayerValue = -1; // For Appliance Stations
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
		if (qActionText == null) Debug.LogWarning("Q_Action_Text not assigned in PickupController. Q prompts will not appear.", this);


		if (interactableLayerMask.value == 0) Debug.LogWarning("InteractableLayerMask not set in PickupController. Interactions might not work.", this);
		else
		{
			string layersInMask = "";
			for (int i = 0; i < 32; i++) { if ((interactableLayerMask.value & (1 << i)) > 0) { layersInMask += LayerMask.LayerToName(i) + " | "; } }
			Debug.Log($"PickupController: InteractableLayerMask includes: {layersInMask}");
		}

		// Get integer values for specific layers by name
		ingredientSourceLayerValue = LayerMask.NameToLayer("IngredientSources");
		pickupableLayerValue = LayerMask.NameToLayer("Pickupable");
		catLayerValue = LayerMask.NameToLayer("CatLayer");
		counter1LayerValue = LayerMask.NameToLayer("Counter1");
		counter2LayerValue = LayerMask.NameToLayer("Counter2");
		radioLayerValue = LayerMask.NameToLayer("Radio");
		bookLayerValue = LayerMask.NameToLayer("Book");

		if (ingredientSourceLayerValue == -1) Debug.LogError("PickupController: Layer 'IngredientSources' not found in Tags and Layers settings.");
		if (pickupableLayerValue == -1) Debug.LogError("PickupController: Layer 'Pickupable' not found. Pickup/Drop action might fail.");
		if (catLayerValue == -1) Debug.LogError("PickupController: Layer 'CatLayer' not found. Create it and assign the cat to it.");
		if (counter1LayerValue == -1) Debug.LogError("PickupController: Layer 'Counter1' not found. 'Cooking Station' prompt might not work.");
		if (counter2LayerValue == -1) Debug.LogError("PickupController: Layer 'Counter2' not found. 'Frying Station' prompt might not work.");
		if (radioLayerValue == -1) Debug.LogError("PickupController: Layer 'Radio' not found. Create it and assign radio objects to it.");
		if (bookLayerValue == -1) Debug.LogError("PickupController: Layer 'Book' not found. Create it and assign book objects to it.");

		if (audioSource == null) { Debug.LogWarning("PickupController requires an AudioSource component.", this); }

		if (eActionText != null) eActionText.text = "";
		if (qActionText != null) qActionText.text = "";
	}

	void OnEnable()
	{
		if (inputHandler != null)
		{
			inputHandler.OnInteractActionStarted += HandleInteractionInput;
			inputHandler.OnCookActionStarted += HandleCookActionInput;
			inputHandler.OnCookActionCanceled += HandleCookActionInputReleased;
		}
		else
		{
			Debug.LogError("PickupController: InputHandler is NULL in OnEnable, cannot subscribe to events!");
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
		string currentQText = "";

		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, interactableLayerMask))
		{
			lastLookedAtGameObjectForUI = hit.collider.gameObject;
			int hitLayer = hit.collider.gameObject.layer;

			// E Key Interaction Prompts
			if (hitLayer == pickupableLayerValue)
			{
				currentEText = " Pickup/Drop";
			}
			else if (currentlyHeldItem == null) // Only show other E prompts if not holding anything
			{
				if (hitLayer == ingredientSourceLayerValue) { currentEText = " Grab item"; }
				else if (hitLayer == catLayerValue) { currentEText = " Pet"; }
				else if (hitLayer == radioLayerValue) { currentEText = " Change Track"; }
				else if (hitLayer == bookLayerValue) { currentEText = " Read"; }
			}

			// Q Key Prompts for Counters
			if (hitLayer == counter1LayerValue)
			{
				CookingStation cs = hit.collider.GetComponent<CookingStation>();
				if (cs != null && !cs.IsProcessing) { currentQText = " Cooking Station"; }
			}
			else if (hitLayer == counter2LayerValue)
			{
				ApplianceStation asStation = hit.collider.GetComponent<ApplianceStation>();
				if (asStation != null && !asStation.IsCooking) { currentQText = " Frying Station"; }
			}
		}
		else
		{
			lastLookedAtGameObjectForUI = null;
		}

		if (eActionText != null && currentEText != lastEText) { eActionText.text = currentEText; lastEText = currentEText; }
		if (qActionText != null && currentQText != lastQText) { qActionText.text = currentQText; lastQText = currentQText; }
	}

	private void HideAllActionTexts()
	{
		if (eActionText != null) eActionText.text = "";
		if (qActionText != null) qActionText.text = "";
		lastEText = "";
		lastQText = "";
		lastLookedAtGameObjectForUI = null;
	}

	private void HandleInteractionInput() // E key pressed
	{
		RaycastHit hit; // Perform a fresh raycast for the action
		bool hitSomething = Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, interactableLayerMask);

		if (currentlyHeldItem != null)
		{
			DropItem(true);
			return;
		}

		if (hitSomething)
		{
			GameObject hitObject = hit.collider.gameObject;
			int hitLayer = hitObject.layer;

			if (hitLayer == pickupableLayerValue)
			{
				Pickupable pickupable = hitObject.GetComponent<Pickupable>();
				if (pickupable != null && pickupable.Rb != null && !pickupable.Rb.isKinematic)
				{ GrabExistingItem(pickupable, hit.collider); return; }
			}
			else if (hitLayer == ingredientSourceLayerValue)
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
				ReadableBook book = hitObject.GetComponent<ReadableBook>();
				if (book != null)
				{
					book.InteractWithBook();
				}
				else
				{
					Debug.LogWarning($"Interacted with Book: {hitObject.name}, but ReadableBook script not found on it.");
				}
				return;
			}
		}
	}

	private void HandleCookActionInput() // Q key pressed
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

	private void HandleCookActionInputReleased()
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
		// if (newItemCollider == null) { Debug.LogWarning($"Spawned item {itemPrefab.name} is missing a Collider!"); } // Collider isn't strictly necessary for holding logic

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
