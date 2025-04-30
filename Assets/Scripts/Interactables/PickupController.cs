using UnityEngine;

public class PickupController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private PlayerInputHandler inputHandler;
	[SerializeField] private Camera mainCamera;
	[SerializeField] private Transform holdPoint;

	[Header("Interaction Parameters")]
	[SerializeField] private float interactionDistance = 3f;
	[SerializeField] private LayerMask ingredientSourceLayerMask; // For spawning
	[SerializeField] private LayerMask pickupLayerMask;         // For picking up dropped/placed items
	[SerializeField] private LayerMask placeLayerMask;          // NEW: For finding valid placement surfaces (Counters/Cutting Boards)
	[SerializeField] private LayerMask collisionCheckLayerMask = ~0;

	[Header("Holding Parameters")]
	[SerializeField] private float collisionCheckBuffer = 0.05f;

	// --- State Variables ---
	private PickupableItem currentlyHeldItem = null;
	private Rigidbody heldItemRigidbody = null;
	private Collider heldItemCollider = null;
	private bool originalGravityState;
	private int originalLayer;
	private Collider playerCollider;
	private int pickupableLayerInt = -1;

	// --- NEW State for Cutting ---
	private CuttableItem targetCuttable = null; // Item player is currently looking at/cutting
	private bool isHoldingCookAction = false;  // Is Q key currently held down?

	void Awake()
	{
		// Cache component references
		if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>(); 
        if (mainCamera == null) mainCamera = Camera.main; 
        playerCollider = GetComponent<Collider>(); 

        // Validate references
        if (inputHandler == null || mainCamera == null || holdPoint == null) { Debug.LogError($"{nameof(PickupController)} missing required references!", this); enabled = false; return; }
		
        if (ingredientSourceLayerMask == 0) { Debug.LogWarning($"'{nameof(ingredientSourceLayerMask)}' not set.", this); }
		
        if (pickupLayerMask == 0) { Debug.LogWarning($"'{nameof(pickupLayerMask)}' not set. Assign the 'Pickupable' layer.", this); }
		
        // NEW: Validate Place Layer Mask
        if (placeLayerMask == 0) { Debug.LogWarning($"'{nameof(placeLayerMask)}' not set. Assign the 'Counter' layer (or similar). You won't be able to place items.", this); }
		if (collisionCheckLayerMask == 0) { Debug.LogWarning($"'{nameof(collisionCheckLayerMask)}' not set.", this); }
		

        // Configure physics interactions
        if (playerCollider != null) { collisionCheckLayerMask &= ~(1 << playerCollider.gameObject.layer); }
	
        pickupableLayerInt = LayerMask.NameToLayer("Pickupable");
        if (pickupableLayerInt == -1) { Debug.LogError("Physics Layer 'Pickupable' not found!"); pickupableLayerInt = 0; }
		

		else { Debug.Log($"Found 'Pickupable' layer. Index: {pickupableLayerInt}"); }
		
    }

	void OnEnable()
	{
		if (inputHandler != null)
		{
			inputHandler.OnInteractActionStarted += HandleInteractStarted;
			// Subscribe to Cook actions
			inputHandler.OnCookActionStarted += HandleCookActionStarted; // NEW
			inputHandler.OnCookActionCanceled += HandleCookActionCanceled; // NEW
		}
	}

	void OnDisable()
	{
		if (inputHandler != null)
		{
			inputHandler.OnInteractActionStarted -= HandleInteractStarted;
			// Unsubscribe from Cook actions
			inputHandler.OnCookActionStarted -= HandleCookActionStarted; // NEW
			inputHandler.OnCookActionCanceled -= HandleCookActionCanceled; // NEW
		}
		// Cleanup state
		if (currentlyHeldItem != null) DropItem(false); // Drop without force if disabled
		if (targetCuttable != null) CancelCuttingProcess(); // Cancel cutting if disabled
	}

	void Update()
	{
		// Update cutting progress if Q is held
		if (isHoldingCookAction && targetCuttable != null)
		{
			if (targetCuttable.UpdateCutting(Time.deltaTime)) // UpdateCutting returns true when done
			{
				CancelCuttingProcess(); // Reset state after completion
			}
		}
	}


	void FixedUpdate()
	{
		// Move held item
		if (currentlyHeldItem != null && heldItemRigidbody != null && heldItemRigidbody.isKinematic)
		{
			MoveHeldItemWithCollisionCheck();
		}
	}

	// Called when E is pressed
	private void HandleInteractStarted()
	{
		if (currentlyHeldItem != null)
		{
			// If holding item, try to PLACE it instead of dropping
			TryPlaceItem();
		}
		else
		{
			// If not holding item, try to Spawn or Pickup
			if (!TryInteractWithSource())
			{
				TryPickupExistingItem();
			}
		}
	}

	// Called when Q is pressed
	private void HandleCookActionStarted()
	{
		if (currentlyHeldItem != null) return; // Can't cut while holding something

		isHoldingCookAction = true;
		targetCuttable = null; // Reset target

		RaycastHit hit;
		// Raycast to find cuttable items (usually on Pickupable layer)
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, pickupLayerMask))
		{
			CuttableItem cuttable = hit.collider.GetComponent<CuttableItem>();
			// Check if it's cuttable AND placed (kinematic)
			Rigidbody hitRb = hit.collider.GetComponent<Rigidbody>();
			if (cuttable != null && hitRb != null && hitRb.isKinematic)
			{
				targetCuttable = cuttable;
				targetCuttable.StartCutting(); // Tell the item to start its cutting process
				Debug.Log($"Cook Action (Q) started on: {cuttable.name}");
			}
			else
			{
				isHoldingCookAction = false; // Didn't find a valid cuttable item
			}
		}
		else
		{
			isHoldingCookAction = false; // Didn't hit anything relevant
		}
	}

	// Called when Q is released
	private void HandleCookActionCanceled()
	{
		isHoldingCookAction = false;
		CancelCuttingProcess(); // Cancel if Q is released
	}

	// NEW: Try to place the held item
	private void TryPlaceItem()
	{
		if (currentlyHeldItem == null) return;

		RaycastHit hit;
		// Raycast to find valid placement surfaces (Counters)
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, placeLayerMask))
		{
			CounterSurface surface = hit.collider.GetComponent<CounterSurface>();
			if (surface != null)
			{
				PlaceItemOnSurface(surface);
			}
			else
			{
				// Optional: Drop item if not looking at a valid surface? Or do nothing?
				Debug.Log("Tried to place, but not looking at a valid CounterSurface.");
				// DropItem(true); // Uncomment to drop if placement fails
			}
		}
		else
		{
			Debug.Log("Tried to place, but raycast hit nothing on PlaceLayerMask.");
			// DropItem(true); // Uncomment to drop if placement fails
		}
	}

	// NEW: Place the item on the counter surface
	private void PlaceItemOnSurface(CounterSurface surface)
	{
		Transform placementPoint = surface.GetPlacementPoint();
		GameObject itemGO = currentlyHeldItem.gameObject;

		Debug.Log($"Placing {itemGO.name} on {surface.gameObject.name}");

		// Release from hold
		itemGO.transform.SetParent(null);

		// Disable physics, make kinematic
		heldItemRigidbody.useGravity = false;
		heldItemRigidbody.isKinematic = true; 
		//heldItemRigidbody.velocity = Vector3.zero;
		//heldItemRigidbody.angularVelocity = Vector3.zero;

		// Position precisely
		itemGO.transform.position = placementPoint.position;
		itemGO.transform.rotation = placementPoint.rotation;

		// Restore collision settings (stop ignoring player)
		if (playerCollider != null && heldItemCollider != null)
		{
			Physics.IgnoreCollision(playerCollider, heldItemCollider, false);
		}

		// Ensure it's on the pickupable layer so Q can find it
		itemGO.layer = pickupableLayerInt;

		// Clear player's held item references
		currentlyHeldItem = null;
		heldItemRigidbody = null;
		heldItemCollider = null;
	}

	// --- Methods from previous setup (Spawning, Picking Up Existing, Dropping) ---

	private bool TryInteractWithSource()
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

	private void TryPickupExistingItem()
	{
		RaycastHit hit;
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, pickupLayerMask))
		{
			// Ensure we don't pick up something mid-cut? Maybe not necessary if Q requires kinematic.
			// CuttableItem potentialCuttable = hit.collider.GetComponent<CuttableItem>();
			// if (potentialCuttable != null && potentialCuttable.IsBeingCut()) return;

			if (hit.collider.gameObject.layer != pickupableLayerInt) return;

			PickupableItem itemToPickup = hit.collider.GetComponent<PickupableItem>();
			if (itemToPickup != null)
			{
				GrabExistingItem(itemToPickup, hit.collider);
			}
		}
	}

	private void GrabExistingItem(PickupableItem item, Collider itemCollider)
	{
		// Check if item is kinematic (meaning it's placed/being cut) - prevent pickup if so?
		// Rigidbody itemRb = item.GetComponent<Rigidbody>();
		// if(itemRb != null && itemRb.isKinematic) {
		//     Debug.Log($"Cannot pick up {item.name}, it is currently placed or being processed.");
		//     return;
		// }

		currentlyHeldItem = item;
		heldItemRigidbody = item.Rb;
		heldItemCollider = itemCollider;
		if (heldItemRigidbody != null)
		{
			originalGravityState = heldItemRigidbody.useGravity;
			heldItemRigidbody.useGravity = false;
			heldItemRigidbody.isKinematic = true; // Becomes kinematic WHEN HELD
			currentlyHeldItem.gameObject.layer = pickupableLayerInt; // Ensure layer
			if (playerCollider != null && heldItemCollider != null) { Physics.IgnoreCollision(playerCollider, heldItemCollider, true); }
			Debug.Log("Picked up existing: " + currentlyHeldItem.name);
		}
		else { /* Error Handling */ currentlyHeldItem = null; heldItemCollider = null; }
	}

	private void SpawnAndHoldItem(GameObject itemPrefab)
	{
		// Instantiate, get components... (same as before) [cite: 45]
		GameObject newItemGO = Instantiate(itemPrefab, holdPoint.position, holdPoint.rotation);
		currentlyHeldItem = newItemGO.GetComponent<PickupableItem>();
		heldItemRigidbody = newItemGO.GetComponent<Rigidbody>();
		heldItemCollider = newItemGO.GetComponent<Collider>();
		if (currentlyHeldItem == null || heldItemRigidbody == null || heldItemCollider == null) { Debug.LogError($"Spawned item '{newItemGO.name}' missing components!", newItemGO); Destroy(newItemGO); return; }
		

        // Configure held physics (same as before) [cite: 47]
        Debug.Log($"Spawned and holding: {newItemGO.name}");
		originalGravityState = heldItemRigidbody.useGravity;
		heldItemRigidbody.useGravity = false;
		heldItemRigidbody.isKinematic = true;
		originalLayer = newItemGO.layer;
		newItemGO.layer = pickupableLayerInt;
		if (playerCollider != null) { Physics.IgnoreCollision(playerCollider, heldItemCollider, true); }
		
    }

	// Modified DropItem: Now has optional parameter to apply force
	private void DropItem(bool applyForce)
	{
		if (currentlyHeldItem == null || heldItemRigidbody == null) return;
		GameObject itemGO = currentlyHeldItem.gameObject; 
        Debug.Log($"Dropped: {itemGO.name}");

		itemGO.transform.SetParent(null); // Detach from hold point

		heldItemRigidbody.isKinematic = false; 
        heldItemRigidbody.useGravity = originalGravityState; 

        if (playerCollider != null && heldItemCollider != null) { Physics.IgnoreCollision(playerCollider, heldItemCollider, false); }
		
        itemGO.layer = pickupableLayerInt; 

        // Optional: Add force only if specified
        if (applyForce && mainCamera != null)
		{
			heldItemRigidbody.AddForce(mainCamera.transform.forward * 1.5f, ForceMode.VelocityChange); // Example force
		}


		currentlyHeldItem = null; 
        heldItemRigidbody = null; 
        heldItemCollider = null; 
    }

	// Helper to cancel cutting (used in OnDisable and HandleCookActionCanceled)
	private void CancelCuttingProcess()
	{
		if (targetCuttable != null)
		{
			targetCuttable.CancelCutting();
			targetCuttable = null;
			isHoldingCookAction = false; // Ensure flag is reset
			Debug.Log("Cutting process canceled.");
		}
		isHoldingCookAction = false; // Ensure flag is reset even if target was null
	}


	private void MoveHeldItemWithCollisionCheck()
	{
		// This method remains the same as before...
		Vector3 startPosition = heldItemRigidbody.position;
		Vector3 targetPosition = holdPoint.position;
		Quaternion targetRotation = holdPoint.rotation;
		Vector3 direction = targetPosition - startPosition;
		float distance = direction.magnitude;

		if (distance < Mathf.Epsilon) { heldItemRigidbody.MoveRotation(targetRotation); return; }

		RaycastHit hitInfo;
		bool collisionDetected = heldItemRigidbody.SweepTest(direction.normalized, out hitInfo, distance, QueryTriggerInteraction.Ignore);
		Vector3 finalPosition = targetPosition;

		if (collisionDetected)
		{
			// Debug.Log($"// DEBUG: SweepTest Hit: {hitInfo.collider.name} on Layer: {LayerMask.LayerToName(hitInfo.collider.gameObject.layer)}");
			int hitLayerIndex = hitInfo.collider.gameObject.layer;
			int hitLayerBit = (1 << hitLayerIndex);
			int maskValue = collisionCheckLayerMask.value;
			int andResult = hitLayerBit & maskValue;
			// Debug.Log($"// DEBUG Values: Hit Layer Index={hitLayerIndex}, Layer Bit={hitLayerBit}, Mask Value={maskValue}, Bitwise AND Result={andResult}");

			if (andResult != 0)
			{
				finalPosition = startPosition + direction.normalized * (hitInfo.distance - collisionCheckBuffer);
				// Debug.Log($"// DEBUG: Layer WAS in mask (Check Result != 0). Adjusting position.");
			}
			// else { Debug.Log($"// DEBUG: Layer was NOT in mask (Check Result == 0). Not adjusting position."); }
		}
		heldItemRigidbody.MovePosition(finalPosition);
		heldItemRigidbody.MoveRotation(targetRotation);
	}
}