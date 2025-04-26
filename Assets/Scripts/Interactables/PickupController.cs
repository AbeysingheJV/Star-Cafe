using UnityEngine;

public class PickupController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private PlayerInputHandler inputHandler;
	[SerializeField] private Camera mainCamera;
	[SerializeField] private Transform holdPoint;

	[Header("Interaction Parameters")]
	[SerializeField] private float interactionDistance = 3f;
	[SerializeField] private LayerMask ingredientSourceLayerMask;
	[SerializeField] private LayerMask pickupLayerMask; 
	[SerializeField] private LayerMask collisionCheckLayerMask = ~0;

	[Header("Holding Parameters")]
	[SerializeField] private float collisionCheckBuffer = 0.05f;

	private PickupableItem currentlyHeldItem = null;
	private Rigidbody heldItemRigidbody = null;
	private Collider heldItemCollider = null;
	private bool originalGravityState;
	private int originalLayer; 
	private Collider playerCollider;
	private int pickupableLayerInt = -1; 

	void Awake()
	{
		if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
		if (mainCamera == null) mainCamera = Camera.main;
		playerCollider = GetComponent<Collider>();

		if (inputHandler == null || mainCamera == null || holdPoint == null) { Debug.LogError($"{nameof(PickupController)} missing required references!", this); enabled = false; return; }
		if (ingredientSourceLayerMask == 0) { Debug.LogWarning($"'{nameof(ingredientSourceLayerMask)}' not set.", this); }
		if (pickupLayerMask == 0) { Debug.LogWarning($"'{nameof(pickupLayerMask)}' not set. Assign the 'Pickupable' layer.", this); }
		if (collisionCheckLayerMask == 0) { Debug.LogWarning($"'{nameof(collisionCheckLayerMask)}' not set.", this); }

		if (playerCollider != null) { collisionCheckLayerMask &= ~(1 << playerCollider.gameObject.layer); }


		pickupableLayerInt = LayerMask.NameToLayer("Pickupable"); 

		if (pickupableLayerInt == -1)
		{
			
			Debug.LogError("Physics Layer 'Pickupable' not found! Please ensure it exists in Edit -> Project Settings -> Tags and Layers.");
			pickupableLayerInt = 0; 
		}
		else
		{
			Debug.Log($"Found 'Pickupable' layer. Index: {pickupableLayerInt}");
		}
	}

	void OnEnable() { if (inputHandler != null) inputHandler.OnInteractActionStarted += HandleInteraction; }
	void OnDisable() { if (inputHandler != null) inputHandler.OnInteractActionStarted -= HandleInteraction; if (currentlyHeldItem != null) DropItem(); }
	void FixedUpdate() { if (currentlyHeldItem != null && heldItemRigidbody != null && heldItemRigidbody.isKinematic) MoveHeldItemWithCollisionCheck(); }

	private void HandleInteraction()
	{
		if (currentlyHeldItem != null) { DropItem(); }
		else { if (!TryInteractWithSource()) { TryPickupExistingItem(); } }
	}

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
			
			if (hit.collider.gameObject.layer != pickupableLayerInt)
			{
				Debug.LogWarning($"Pickup raycast hit {hit.collider.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}, but expected layer {LayerMask.LayerToName(pickupableLayerInt)}. Check Inspector Mask and Item Layer.", hit.collider.gameObject);
				return; 
			}

			PickupableItem itemToPickup = hit.collider.GetComponent<PickupableItem>();
			if (itemToPickup != null)
			{
				GrabExistingItem(itemToPickup, hit.collider);
			}
		}
	}

	private void GrabExistingItem(PickupableItem item, Collider itemCollider)
	{
		currentlyHeldItem = item;
		heldItemRigidbody = item.Rb;
		heldItemCollider = itemCollider;

		if (heldItemRigidbody != null)
		{
			originalGravityState = heldItemRigidbody.useGravity;
			heldItemRigidbody.useGravity = false;
			heldItemRigidbody.isKinematic = true;

			
			currentlyHeldItem.gameObject.layer = pickupableLayerInt;

			if (playerCollider != null && heldItemCollider != null) { Physics.IgnoreCollision(playerCollider, heldItemCollider, true); }
			Debug.Log("Picked up existing: " + currentlyHeldItem.name);
		}
		else { currentlyHeldItem = null; heldItemCollider = null; Debug.LogWarning($"Item {itemCollider.name} missing Rigidbody."); }
	}

	private void SpawnAndHoldItem(GameObject itemPrefab)
	{
		GameObject newItemGO = Instantiate(itemPrefab, holdPoint.position, holdPoint.rotation);
		currentlyHeldItem = newItemGO.GetComponent<PickupableItem>();
		heldItemRigidbody = newItemGO.GetComponent<Rigidbody>();
		heldItemCollider = newItemGO.GetComponent<Collider>();

		if (currentlyHeldItem == null || heldItemRigidbody == null || heldItemCollider == null) { Debug.LogError($"Spawned item '{newItemGO.name}' missing components!", newItemGO); Destroy(newItemGO); return; }

		Debug.Log($"Spawned and holding: {newItemGO.name}");
		originalGravityState = heldItemRigidbody.useGravity;
		heldItemRigidbody.useGravity = false;
		heldItemRigidbody.isKinematic = true;
		originalLayer = newItemGO.layer;
		newItemGO.layer = pickupableLayerInt; 

		if (playerCollider != null) { Physics.IgnoreCollision(playerCollider, heldItemCollider, true); }
	}

	private void DropItem()
	{
		if (currentlyHeldItem == null || heldItemRigidbody == null) return;
		GameObject itemGO = currentlyHeldItem.gameObject;
		Debug.Log($"Dropped: {itemGO.name}");

		heldItemRigidbody.isKinematic = false;
		heldItemRigidbody.useGravity = originalGravityState;

		if (playerCollider != null && heldItemCollider != null) { Physics.IgnoreCollision(playerCollider, heldItemCollider, false); }

		
		itemGO.layer = pickupableLayerInt;

		currentlyHeldItem = null;
		heldItemRigidbody = null;
		heldItemCollider = null;
	}

	
	private void MoveHeldItemWithCollisionCheck()
	{
		Vector3 startPosition = heldItemRigidbody.position;
		Vector3 targetPosition = holdPoint.position;
		Quaternion targetRotation = holdPoint.rotation;

		Vector3 direction = targetPosition - startPosition;
		float distance = direction.magnitude;

		if (distance < Mathf.Epsilon)
		{
			heldItemRigidbody.MoveRotation(targetRotation);
			return;
		}

		RaycastHit hitInfo;
		
		bool collisionDetected = heldItemRigidbody.SweepTest(direction.normalized, out hitInfo, distance, QueryTriggerInteraction.Ignore);

		Vector3 finalPosition = targetPosition;

		if (collisionDetected)
		{
			
			Debug.Log($"// DEBUG: SweepTest Hit: {hitInfo.collider.name} on Layer: {LayerMask.LayerToName(hitInfo.collider.gameObject.layer)}");

			
			int hitLayerIndex = hitInfo.collider.gameObject.layer;
			int hitLayerBit = (1 << hitLayerIndex);
			int maskValue = collisionCheckLayerMask.value; 
			int andResult = hitLayerBit & maskValue;

			Debug.Log($"// DEBUG Values: Hit Layer Index={hitLayerIndex}, Layer Bit={hitLayerBit}, Mask Value={maskValue}, Bitwise AND Result={andResult}");
			

			
			if ((hitLayerBit & maskValue) != 0) 
			{
				finalPosition = startPosition + direction.normalized * (hitInfo.distance - collisionCheckBuffer);
				Debug.Log($"// DEBUG: Layer WAS in mask (Check Result != 0). Adjusting position.");
			}
			else
			{
				Debug.Log($"// DEBUG: Layer was NOT in mask (Check Result == 0). Not adjusting position.");
			}
		}

		
		heldItemRigidbody.MovePosition(finalPosition);
		heldItemRigidbody.MoveRotation(targetRotation);
	}
}