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
	[SerializeField] private LayerMask raycastIgnoreLayerMask;
	[SerializeField] private LayerMask cookingStationLayerMask; 
	[SerializeField] private LayerMask ingredientSourceLayerMask;

	[Header("Holding Parameters")]
	[SerializeField] private float positionLerpSpeed = 15f;
	[SerializeField] private float rotationLerpSpeed = 15f;

	private Pickupable currentlyHeldItem = null;
	private Rigidbody heldItemRigidbody = null;
	private bool originalGravityState;
	private Collider playerCollider;
	private int pickupableLayerInt = -1;

	private CookingStation activeCookingStation = null; 

	void Awake()
	{
		if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
		if (mainCamera == null) mainCamera = Camera.main;
		playerCollider = GetComponent<Collider>();
		if (inputHandler == null || mainCamera == null || holdPoint == null) { enabled = false; return; }
		if (pickupLayerMask == 0) { }
		if (raycastIgnoreLayerMask == 0) { }
		if (cookingStationLayerMask == 0) { }
		if (ingredientSourceLayerMask == 0) { }
		pickupableLayerInt = LayerMask.NameToLayer("Pickupable");
		if (pickupableLayerInt == -1) { }
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
		if (currentlyHeldItem != null) DropItem();
		
		if (activeCookingStation != null) { activeCookingStation.CancelHoldToCook(); activeCookingStation = null; }
	}

	void FixedUpdate()
	{
		if (currentlyHeldItem != null && heldItemRigidbody != null) { MoveHeldItemSmoothly(); }
	}

	private void HandleInteraction()
	{
		if (currentlyHeldItem != null) { DropItem(); } else { if (!TrySpawnFromSource()) { TryPickupExistingItem(); } }
	}

	
	private void HandleCookActionStarted()
	{
		if (currentlyHeldItem != null) { return; }
		activeCookingStation = null; 

		RaycastHit hit;
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, cookingStationLayerMask))
		{
			
			ApplianceStation appliance = hit.collider.GetComponent<ApplianceStation>();
			if (appliance != null)
			{
				appliance.StartAutoCooking();
				return; 
			}

			
			CookingStation station = hit.collider.GetComponent<CookingStation>();
			if (station != null)
			{
				if (station.StartHoldToCook()) 
				{
					activeCookingStation = station; 
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

	private bool TrySpawnFromSource() { RaycastHit hit; if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, ingredientSourceLayerMask)) { IngredientSource source = hit.collider.GetComponent<IngredientSource>(); if (source != null) { GameObject prefabToSpawn = source.GetIngredientPrefab(); if (prefabToSpawn != null) { SpawnAndHoldItem(prefabToSpawn); return true; } } } return false; }
	private void SpawnAndHoldItem(GameObject itemPrefab) { GameObject newItemGO = Instantiate(itemPrefab, holdPoint.position, holdPoint.rotation); currentlyHeldItem = newItemGO.GetComponent<Pickupable>(); heldItemRigidbody = newItemGO.GetComponent<Rigidbody>(); Collider newItemCollider = newItemGO.GetComponent<Collider>(); if (currentlyHeldItem == null || heldItemRigidbody == null || newItemCollider == null) { Destroy(newItemGO); currentlyHeldItem = null; heldItemRigidbody = null; return; } originalGravityState = heldItemRigidbody.useGravity; heldItemRigidbody.useGravity = false; heldItemRigidbody.isKinematic = true; if (playerCollider != null) { Physics.IgnoreCollision(playerCollider, newItemCollider, true); } }
	private void TryPickupExistingItem() { RaycastHit hit; if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, pickupLayerMask)) { Pickupable itemToPickup = hit.collider.GetComponent<Pickupable>(); if (itemToPickup != null) { GrabExistingItem(itemToPickup, hit.collider); } } }
	private void GrabExistingItem(Pickupable item, Collider itemCollider) { Rigidbody itemRb = item.Rb; currentlyHeldItem = item; heldItemRigidbody = itemRb; if (heldItemRigidbody != null) { originalGravityState = heldItemRigidbody.useGravity; heldItemRigidbody.useGravity = false; heldItemRigidbody.isKinematic = true; currentlyHeldItem.gameObject.layer = pickupableLayerInt; if (playerCollider != null && itemCollider != null) { Physics.IgnoreCollision(playerCollider, itemCollider, true); } } else { currentlyHeldItem = null; } }
	private void DropItem() { if (currentlyHeldItem == null || heldItemRigidbody == null) return; Collider itemCollider = currentlyHeldItem.GetComponent<Collider>(); heldItemRigidbody.isKinematic = false; heldItemRigidbody.useGravity = originalGravityState; if (playerCollider != null && itemCollider != null) { Physics.IgnoreCollision(playerCollider, itemCollider, false); } currentlyHeldItem.gameObject.layer = pickupableLayerInt; currentlyHeldItem = null; heldItemRigidbody = null; }
	private void MoveHeldItemSmoothly() { Vector3 targetPosition = holdPoint.position; Quaternion targetRotation = holdPoint.rotation; Vector3 smoothedPosition = Vector3.Lerp(heldItemRigidbody.position, targetPosition, Time.fixedDeltaTime * positionLerpSpeed); heldItemRigidbody.MovePosition(smoothedPosition); Quaternion smoothedRotation = Quaternion.Lerp(heldItemRigidbody.rotation, targetRotation, Time.fixedDeltaTime * rotationLerpSpeed); heldItemRigidbody.MoveRotation(smoothedRotation); }
}