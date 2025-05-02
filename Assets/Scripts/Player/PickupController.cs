using UnityEngine;

public class PickupController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private PlayerInputHandler inputHandler;
	[SerializeField] private Camera mainCamera;
	[SerializeField] private Transform holdPoint;

	[Header("Interaction Parameters")]
	[SerializeField] private float interactionDistance = 3f;
	[SerializeField] private LayerMask pickupLayerMask; // For Pickupable items
	[SerializeField] private LayerMask raycastIgnoreLayerMask; // To ignore Player layer
	[SerializeField] private LayerMask cookingStationLayerMask; // For Cooking Stations
	[SerializeField] private LayerMask ingredientSourceLayerMask; // For ingredient sources/piles

	[Header("Holding Parameters")]
	[SerializeField] private float positionLerpSpeed = 15f; // Speed for smooth position movement
	[SerializeField] private float rotationLerpSpeed = 15f; // Speed for smooth rotation movement


	
	private Pickupable currentlyHeldItem = null;
	private Rigidbody heldItemRigidbody = null;
	private bool originalGravityState;
	private Collider playerCollider; 

	void Awake()
	{
		if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
		if (mainCamera == null) mainCamera = Camera.main;
		playerCollider = GetComponent<Collider>(); 

		if (inputHandler == null || mainCamera == null || holdPoint == null)
		{
			Debug.LogError("PickupController missing required references!", this);
			enabled = false; return;
		}
		if (pickupLayerMask == 0)
		{
			Debug.LogWarning("Pickup Layer Mask is not set. Won't be able to detect pickupable items.", this);
		}
		if (raycastIgnoreLayerMask == 0)
		{
			Debug.LogWarning("Raycast Ignore Layer Mask not set. May hit player collider.", this);
		}
		if (cookingStationLayerMask == 0)
		{
			Debug.LogWarning("Cooking Station Layer Mask is not set. Won't be able to cook.", this);
		}
		
		if (ingredientSourceLayerMask == 0)
		{
			Debug.LogWarning("Ingredient Source Layer Mask is not set. Won't be able to spawn items from sources.", this);
		}
	}

	void OnEnable()
	{
		if (inputHandler != null)
		{
			inputHandler.OnInteractActionStarted += HandleInteraction;
			inputHandler.OnCookActionStarted += HandleCookActionStarted;
		}
	}

	void OnDisable()
	{
		if (inputHandler != null)
		{
			inputHandler.OnInteractActionStarted -= HandleInteraction;
			inputHandler.OnCookActionStarted -= HandleCookActionStarted;
		}
		
		if (currentlyHeldItem != null) DropItem();
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
			
			DropItem();
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
		if (currentlyHeldItem != null)
		{
			Debug.Log("Cannot cook while holding an item.");
			return; 
		}

		RaycastHit hit;
		
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, cookingStationLayerMask))
		{
			CookingStation station = hit.collider.GetComponent<CookingStation>();
			if (station != null)
			{
				Debug.Log($"Attempting to cook at station: {station.name}");
				station.TryCook(); 
			}
			else
			{
				Debug.Log($"Looked at object {hit.collider.name} on Cooking Station layer, but it has no CookingStation script.");
			}
		}
		else
		{
			Debug.Log("Looked around, but didn't see a cooking station to interact with.");
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
		if (itemPrefab == null) return;

		Debug.Log($"Spawning item from source: {itemPrefab.name}");

		GameObject newItemGO = Instantiate(itemPrefab, holdPoint.position, holdPoint.rotation);

		currentlyHeldItem = newItemGO.GetComponent<Pickupable>();
		heldItemRigidbody = newItemGO.GetComponent<Rigidbody>();
		Collider newItemCollider = newItemGO.GetComponent<Collider>();

		if (currentlyHeldItem == null || heldItemRigidbody == null || newItemCollider == null)
		{
			Debug.LogError($"Spawned item '{newItemGO.name}' is missing required components (Pickupable, Rigidbody, or Collider)!", newItemGO);
			Destroy(newItemGO);
			currentlyHeldItem = null;
			heldItemRigidbody = null;
			return;
		}

		originalGravityState = heldItemRigidbody.useGravity;
		heldItemRigidbody.useGravity = false;
		heldItemRigidbody.isKinematic = true;

		if (playerCollider != null)
		{
			Physics.IgnoreCollision(playerCollider, newItemCollider, true);
		}
		Debug.Log($"Spawned and holding: {currentlyHeldItem.name}");
	}


	private void TryPickupExistingItem() 
	{
		RaycastHit hit;
		
		int finalRaycastMask = ~raycastIgnoreLayerMask.value;

		
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, finalRaycastMask))
		{
			
			if (((1 << hit.collider.gameObject.layer) & pickupLayerMask) != 0)
			{
				
				Pickupable itemToPickup = hit.collider.GetComponent<Pickupable>();
				if (itemToPickup != null)
				{
					
					currentlyHeldItem = itemToPickup;
					heldItemRigidbody = itemToPickup.Rb;

					if (heldItemRigidbody != null)
					{
						
						originalGravityState = heldItemRigidbody.useGravity;
						heldItemRigidbody.useGravity = false;
						heldItemRigidbody.isKinematic = true;

						
						if (playerCollider != null)
						{
							Physics.IgnoreCollision(playerCollider, hit.collider, true);
						}
						Debug.Log($"Picked up existing: {currentlyHeldItem.name}");
					}
					else
					{
						Debug.LogError($"Pickupable item {itemToPickup.name} is missing its Rigidbody!", itemToPickup);
						currentlyHeldItem = null; 
					}
				}
			}
		}
	}


	private void DropItem()
	{
		if (currentlyHeldItem == null || heldItemRigidbody == null) return;

		Debug.Log($"Dropped: {currentlyHeldItem.name}");

		Collider itemCollider = currentlyHeldItem.GetComponent<Collider>();

		heldItemRigidbody.isKinematic = false;
		heldItemRigidbody.useGravity = originalGravityState;

		if (playerCollider != null && itemCollider != null)
		{
			Physics.IgnoreCollision(playerCollider, itemCollider, false);
		}

		currentlyHeldItem = null;
		heldItemRigidbody = null;
	}

	
	private void MoveHeldItemSmoothly()
	{
		Vector3 targetPosition = holdPoint.position;
		Quaternion targetRotation = holdPoint.rotation;

		Vector3 smoothedPosition = Vector3.Lerp(
			heldItemRigidbody.position,
			targetPosition,
			Time.fixedDeltaTime * positionLerpSpeed
		);
		heldItemRigidbody.MovePosition(smoothedPosition);

		Quaternion smoothedRotation = Quaternion.Lerp(
			heldItemRigidbody.rotation,
			targetRotation,
			Time.fixedDeltaTime * rotationLerpSpeed
		);
		heldItemRigidbody.MoveRotation(smoothedRotation);
	}
}