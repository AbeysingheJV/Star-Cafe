// PickupController.cs
using UnityEngine;
using UnityEngine.UI;

public class PickupController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private PlayerInputHandler inputHandler;
	[SerializeField] private Camera mainCamera;
	[SerializeField] private Transform holdPoint;

	[Header("Interaction Parameters")]
	[SerializeField] private float interactionDistance = 3f;
	[SerializeField] private LayerMask pickupLayerMask; // Layer for items that can ONLY be picked up with E
	[SerializeField] private LayerMask placeLayerMask;  // Layer for surfaces to place items on
	[SerializeField] private LayerMask interactLayerMask; // Layer for items that can be interacted with using Q (e.g., cuttable items on counters)

	[Header("Counter Layer")]
	[Tooltip("Set this to the layer your counter objects are on.")]
	[SerializeField] private LayerMask counterLayerMask;

	[Header("Physics Parameters")]
	[SerializeField] private CollisionDetectionMode heldItemCollisionMode = CollisionDetectionMode.ContinuousDynamic;
	[SerializeField] private bool ignorePlayerCollision = true;
	[SerializeField] private string heldItemLayerName = "HeldItem";

	// State variables
	private PickupableItem currentlyHeldItem = null;
	private Rigidbody heldItemRigidbody = null;
	private Collider heldItemCollider = null;
	private int originalLayer;
	private int heldItemLayer;

	// State for Cooking Action (Q)
	private CuttableItem currentTargetCuttable = null; // Item we are currently cutting
	private bool isHoldingCookAction = false; // Is Q key being held?

	void Awake()
	{
		// (Assign references - Same as before)
		if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
		if (mainCamera == null) mainCamera = Camera.main;
		if (inputHandler == null || mainCamera == null || holdPoint == null) { /* Error handling */ enabled = false; return; }

		// (Layer setup - Same as before)
		heldItemLayer = LayerMask.NameToLayer(heldItemLayerName);
		if (heldItemLayer == -1) heldItemLayer = 0;
		if (interactLayerMask == 0) interactLayerMask = pickupLayerMask; // Default if not set
		if (counterLayerMask == 0) Debug.LogError("Counter Layer Mask not set!");
		if (pickupLayerMask == 0) Debug.LogError("Pickup Layer Mask not set!");

	}

	void OnEnable()
	{
		if (inputHandler != null)
		{
			// Subscribe Interact (E) events
			inputHandler.OnInteractStartedAction += HandleInteractStarted; // E Pressed
			inputHandler.OnInteractCanceledAction += HandleInteractCanceled; // E Released

			// *** NEW: Subscribe CookAction (Q) events ***
			inputHandler.OnCookActionStartedAction += HandleCookActionStarted; // Q Pressed
			inputHandler.OnCookActionCanceledAction += HandleCookActionCanceled; // Q Released
		}
	}

	void OnDisable()
	{
		if (inputHandler != null)
		{
			// Unsubscribe Interact (E) events
			inputHandler.OnInteractStartedAction -= HandleInteractStarted;
			inputHandler.OnInteractCanceledAction -= HandleInteractCanceled;

			// *** NEW: Unsubscribe CookAction (Q) events ***
			inputHandler.OnCookActionStartedAction -= HandleCookActionStarted;
			inputHandler.OnCookActionCanceledAction -= HandleCookActionCanceled;
		}

		// Clean up state
		if (currentlyHeldItem != null) DropItem();
		if (currentTargetCuttable != null) CancelCuttingProcess();
	}

	void Update()
	{
		// Update cutting progress ONLY if Q is held and we have a target
		if (isHoldingCookAction && currentTargetCuttable != null)
		{
			// UpdateCutting returns true if cutting completed
			if (currentTargetCuttable.UpdateCutting(Time.deltaTime))
			{
				currentTargetCuttable = null; // Clear target after cutting
				isHoldingCookAction = false; // Stop processing hold
			}
		}

		// Move held item if applicable (independent of Q or E)
		if (currentlyHeldItem != null && heldItemRigidbody != null && holdPoint != null)
		{
			MoveHeldItem();
		}
	}

	// --- E Key Logic (Interact: Pickup/Place) ---

	// Called when E is PRESSED
	private void HandleInteractStarted()
	{
		// If already holding item, E press does nothing (placement happens on release)
		if (currentlyHeldItem != null)
		{
			return;
		}

		// If not holding, try to pick up what's in front (only using pickupLayerMask)
		RaycastHit hit;
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, pickupLayerMask)) // *** Use pickupLayerMask ***
		{
			PickupableItem itemToPickup = hit.collider.GetComponent<PickupableItem>();
			if (itemToPickup != null)
			{
				TryPickup(itemToPickup, hit.collider);
			}
		}
	}

	// Called when E is RELEASED
	private void HandleInteractCanceled()
	{
		// If holding an item, try to place it
		if (currentlyHeldItem != null)
		{
			TryPlace();
		}
		// If not holding, E release does nothing
	}


	// --- Q Key Logic (Cook Action: Cutting etc.) ---

	// Called when Q is PRESSED
	private void HandleCookActionStarted()
	{
		isHoldingCookAction = true; // Set flag: Q is now being held

		// Cannot start cooking action if already holding an item with E
		if (currentlyHeldItem != null)
		{
			isHoldingCookAction = false; // Immediately cancel hold state if holding item
			return;
		}

		// Raycast to see what interactable item (e.g., cuttable) is in front
		RaycastHit hit;
		// *** Use interactLayerMask: This mask should contain Cuttable items etc. ***
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, interactLayerMask))
		{
			CuttableItem cuttable = hit.collider.GetComponent<CuttableItem>();

			// Check if it's a cuttable item AND it's on a counter
			if (cuttable != null && IsItemOnCounter(cuttable.gameObject))
			{
				// Start cutting process
				currentTargetCuttable = cuttable;
				currentTargetCuttable.StartCutting();
				// isHoldingCookAction remains true (set at start of method)
				Debug.Log($"Cook Action (Q) started on: {cuttable.name}");
			}
			else
			{
				// Hit something interactable, but wasn't cuttable or wasn't on counter
				isHoldingCookAction = false; // Cancel hold state, Q press does nothing here
				Debug.Log($"Cook Action (Q) ignored: Target {hit.collider.name} not cuttable on counter.");
			}
		}
		else
		{
			// Didn't hit anything interactable with Q
			isHoldingCookAction = false; // Cancel hold state
			Debug.Log("Cook Action (Q) ignored: Nothing interactable in range.");
		}
	}

	// Called when Q is RELEASED
	private void HandleCookActionCanceled()
	{
		isHoldingCookAction = false; // Q is no longer held

		// If we were in the middle of cutting something, cancel it
		if (currentTargetCuttable != null)
		{
			CancelCuttingProcess();
		}
		// If Q is released and we weren't cutting anything, do nothing.
	}


	// --- Helper Methods --- (Mostly Unchanged)

	private bool IsItemOnCounter(GameObject item)
	{
		if (item == null)
		{
			Debug.LogWarning("IsItemOnCounter: Item is null.");
			return false;
		}

		RaycastHit downHit;
		// Slightly increase check distance for debugging, adjust if needed
		float checkDistance = 0.6f;
		// Start the ray slightly above the item's pivot point
		Vector3 rayStart = item.transform.position + Vector3.up * 0.01f;

		Debug.Log($"IsItemOnCounter: Checking below {item.name} from {rayStart} downwards {checkDistance} units for layers in mask value {counterLayerMask.value}");

		// Perform the raycast
		bool didHit = Physics.Raycast(rayStart, Vector3.down, out downHit, checkDistance, counterLayerMask);

		if (didHit)
		{
			// We hit something on the correct layer!
			Debug.Log($"IsItemOnCounter: Raycast HIT {downHit.collider.name} on layer {LayerMask.LayerToName(downHit.collider.gameObject.layer)}. SUCCESS - Item is on counter.");
			return true;
		}
		else
		{
			// Raycast either hit nothing, or hit something NOT on the counter layer.
			// Let's do a secondary check without the layer mask to see what *was* hit (if anything)
			RaycastHit anyHit;
			if (Physics.Raycast(rayStart, Vector3.down, out anyHit, checkDistance))
			{
				Debug.LogWarning($"IsItemOnCounter: Raycast HIT {anyHit.collider.name} but it's on layer '{LayerMask.LayerToName(anyHit.collider.gameObject.layer)}', which is NOT in the CounterLayerMask. FAILURE.");
			}
			else
			{
				Debug.LogWarning($"IsItemOnCounter: Raycast downwards DID NOT HIT anything within {checkDistance} units. FAILURE.");
			}
			return false;
		}
	}

	private void CancelCuttingProcess()
	{
		if (currentTargetCuttable != null)
		{
			currentTargetCuttable.CancelCutting();
			currentTargetCuttable = null; // Clear the target
			Debug.Log("Cutting process canceled.");
		}
	}

	private void TryPickup(PickupableItem item, Collider itemCollider)
	{
		if (item == null || currentlyHeldItem != null) return; // Already holding or invalid item

		currentlyHeldItem = item;
		heldItemRigidbody = item.Rb;
		heldItemCollider = itemCollider;

		if (heldItemRigidbody == null || heldItemCollider == null) { /* Error Log */ currentlyHeldItem = null; return; }

		// --- Physics Setup ---
		originalLayer = item.gameObject.layer;
		heldItemRigidbody.collisionDetectionMode = heldItemCollisionMode;
		heldItemRigidbody.useGravity = false;
		heldItemRigidbody.isKinematic = false;
		if (heldItemLayer != 0) item.gameObject.layer = heldItemLayer;
		if (ignorePlayerCollision)
		{
			Collider playerCollider = GetComponent<Collider>();
			if (playerCollider != null) Physics.IgnoreCollision(heldItemCollider, playerCollider, true);
		}
		// --- End Physics Setup ---

		item.transform.SetParent(holdPoint);
		item.transform.position = holdPoint.position;
		item.transform.rotation = holdPoint.rotation;
		heldItemRigidbody.velocity = Vector3.zero;
		heldItemRigidbody.angularVelocity = Vector3.zero;

		Debug.Log("Picked up: " + item.name); // Log pickup
	}

	private void TryPlace()
	{
		if (currentlyHeldItem == null) return;

		RaycastHit hit;
		// Use placeLayerMask to find valid surfaces
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, placeLayerMask))
		{
			PlaceItem(hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
			Debug.Log("Placed item on: " + hit.collider.name);
		}
		else
		{
			// Option: DropItem() or just keep holding
			Debug.Log("No valid placement surface found. Still holding.");
		}
	}

	// (PlaceItem, DropItem, RestoreItemPhysics, MoveHeldItem methods remain the same)
	private void PlaceItem(Vector3 position, Quaternion rotation)
	{
		if (currentlyHeldItem == null) return;
		GameObject itemGO = currentlyHeldItem.gameObject;
		RestoreItemPhysics();
		itemGO.transform.SetParent(null);
		Vector3 slightlyAbove = position + rotation * Vector3.up * 0.02f;
		itemGO.transform.position = slightlyAbove;
		itemGO.transform.rotation = rotation;
		if (heldItemRigidbody != null) { heldItemRigidbody.velocity = Vector3.zero; heldItemRigidbody.angularVelocity = Vector3.zero; }
		currentlyHeldItem = null; heldItemRigidbody = null; heldItemCollider = null;
	}

	private void DropItem()
	{
		if (currentlyHeldItem == null) return;
		GameObject itemGO = currentlyHeldItem.gameObject; Rigidbody itemRB = heldItemRigidbody;
		RestoreItemPhysics();
		itemGO.transform.SetParent(null);
		if (itemRB != null) itemRB.AddForce(mainCamera.transform.forward * 2f, ForceMode.VelocityChange);
		Debug.Log("Dropped: " + currentlyHeldItem.name);
		currentlyHeldItem = null; heldItemRigidbody = null; heldItemCollider = null;
	}

	private void RestoreItemPhysics()
	{
		if (currentlyHeldItem == null) return;
		GameObject itemGO = currentlyHeldItem.gameObject; Rigidbody itemRB = heldItemRigidbody; Collider itemCol = heldItemCollider;
		if (itemRB != null) { itemRB.useGravity = true; itemRB.velocity = Vector3.zero; itemRB.angularVelocity = Vector3.zero; }
		if (itemGO != null)
		{
			itemGO.layer = originalLayer;
			if (ignorePlayerCollision)
			{
				Collider playerCollider = GetComponent<Collider>();
				if (playerCollider != null && itemCol != null) Physics.IgnoreCollision(itemCol, playerCollider, false);
			}
		}
	}

	private void MoveHeldItem()
	{
		Vector3 targetPosition = holdPoint.position; Quaternion targetRotation = holdPoint.rotation;
		heldItemRigidbody.MovePosition(targetPosition); heldItemRigidbody.MoveRotation(targetRotation);
	}

}