using UnityEngine;
using UnityEngine.UI; 
using TMPro;        

public class PickupController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private PlayerInputHandler inputHandler;
	[SerializeField] private Camera mainCamera;
	[SerializeField] private Transform holdPoint;
	[Header("UI")] 
	[Tooltip("Assign the TextMeshPro UI element for showing interaction prompts (E/Q).")]
	[SerializeField] private TextMeshProUGUI interactionPromptText; 

	[Header("Interaction Parameters")]
	[SerializeField] private float interactionDistance = 3f; 
	[SerializeField] private LayerMask pickupLayerMask;     
	[SerializeField] private LayerMask placeLayerMask;      
	[SerializeField] private LayerMask interactLayerMask;   

	[Header("Counter Layer")]
	[Tooltip("Set this to the layer your counter objects are on.")]
	[SerializeField] private LayerMask counterLayerMask;    

	[Header("Physics Parameters")]
	[SerializeField] private CollisionDetectionMode heldItemCollisionMode = CollisionDetectionMode.ContinuousDynamic; 
	[SerializeField] private bool ignorePlayerCollision = true;
	[SerializeField] private string heldItemLayerName = "HeldItem";

	private PickupableItem currentlyHeldItem = null; 
	private Rigidbody heldItemRigidbody = null;     
	private Collider heldItemCollider = null;        
	private int originalLayer;                      
	private int heldItemLayer;                       

	
	private CuttableItem currentTargetCuttable = null; 
	private bool isHoldingCookAction = false;          

	void Awake()
	{
		if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
		if (mainCamera == null) mainCamera = Camera.main; 

		if (interactionPromptText == null)
		{
			Debug.LogWarning($"{nameof(PickupController)} is missing the Interaction Prompt Text reference. UI prompts will not be shown.", this);
		}
		

		if (inputHandler == null || mainCamera == null || holdPoint == null) { enabled = false; return; } 


	
		heldItemLayer = LayerMask.NameToLayer(heldItemLayerName);
		if (heldItemLayer == -1) heldItemLayer = 0;
		if (interactLayerMask == 0) interactLayerMask = pickupLayerMask; 
		if (counterLayerMask == 0) Debug.LogError("Counter Layer Mask is not set!"); 
		if (pickupLayerMask == 0) Debug.LogError("Pickup Layer Mask is not set!"); 

		
		if (interactionPromptText != null)
		{
			interactionPromptText.text = ""; 
		}
		
	}

	void OnEnable()
	{
		if (inputHandler != null)
		{
			
			inputHandler.OnInteractStartedAction += HandleInteractStarted; 
			inputHandler.OnInteractCanceledAction += HandleInteractCanceled; 

			
			inputHandler.OnCookActionStartedAction += HandleCookActionStarted;  
			inputHandler.OnCookActionCanceledAction += HandleCookActionCanceled; 
		}
	}

	void OnDisable()
	{
		if (inputHandler != null)
		{
			
			inputHandler.OnInteractStartedAction -= HandleInteractStarted;
			inputHandler.OnInteractCanceledAction -= HandleInteractCanceled;

			
			inputHandler.OnCookActionStartedAction -= HandleCookActionStarted;
			inputHandler.OnCookActionCanceledAction -= HandleCookActionCanceled;
		}

		
		if (currentlyHeldItem != null) DropItem();
		if (currentTargetCuttable != null) CancelCuttingProcess();

		
		if (interactionPromptText != null)
		{
			interactionPromptText.text = "";
		}
		
	}

	void Update() 
	{
		
		HandleInteractionPrompt();
		

		
		if (isHoldingCookAction && currentTargetCuttable != null)
		{
			
			if (currentTargetCuttable.UpdateCutting(Time.deltaTime))
			{
				currentTargetCuttable = null; 
				isHoldingCookAction = false; 
			}
		}

		
		if (currentlyHeldItem != null && heldItemRigidbody != null && holdPoint != null)
		{
			MoveHeldItem();
		}
	}

	
	private void HandleInteractStarted() 
	{
		if (currentlyHeldItem != null) return; 

		RaycastHit hit; 
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, pickupLayerMask)) // *** Use pickupLayerMask *** [cite: 21]
		{
			PickupableItem itemToPickup = hit.collider.GetComponent<PickupableItem>();
			if (itemToPickup != null) 
			{
				TryPickup(itemToPickup, hit.collider); 
			}
		}
	}

	private void HandleInteractCanceled() 
	{
		if (currentlyHeldItem != null) 
		{
			TryPlace(); 
		}
	}

	
	private void HandleCookActionStarted() 
	{
		isHoldingCookAction = true; 

		if (currentlyHeldItem != null) 
		{
			isHoldingCookAction = false; 
			return; 
		}

		RaycastHit hit; 
						
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, interactLayerMask)) 
		{
			CuttableItem cuttable = hit.collider.GetComponent<CuttableItem>(); 

			
			if (cuttable != null && IsItemOnCounter(cuttable.gameObject)) 
			{
				
				currentTargetCuttable = cuttable;
				currentTargetCuttable.StartCutting();
				Debug.Log($"Cook Action (Q) started on: {cuttable.name}"); 
			}
			else
			{
				
				isHoldingCookAction = false; 
											 
			}
		}
		else
		{
			
			isHoldingCookAction = false; 
										
		}
	}

	private void HandleCookActionCanceled()
	{
		isHoldingCookAction = false; 

		if (currentTargetCuttable != null) 
		{
			CancelCuttingProcess(); 
		}
	}


	
	private void HandleInteractionPrompt()
	{
		if (interactionPromptText == null) return; 
		string prompt = ""; 

		
		if (currentlyHeldItem != null)
		{
			
			RaycastHit placeHit;
			if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out placeHit, interactionDistance, placeLayerMask))
			{
				prompt = "Press E to Place";
			}
			
		}
		
		else
		{
			RaycastHit lookHit;
			
			LayerMask combinedMask = pickupLayerMask | interactLayerMask;
			if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out lookHit, interactionDistance, combinedMask))
			{
				
				CuttableItem cuttable = lookHit.collider.GetComponent<CuttableItem>();

				if (cuttable != null && IsItemOnCounter(cuttable.gameObject) && !isHoldingCookAction)
				
				{
					prompt = "Press Q to Cut"; 
				}
				else if (!isHoldingCookAction) 
				{
					
					PickupableItem pickupable = lookHit.collider.GetComponent<PickupableItem>();
					
					if (pickupable != null && ((1 << lookHit.collider.gameObject.layer) & pickupLayerMask) != 0)
					{
						prompt = "Press E to Pickup";
					}
				}
				 
			}
			
		}

		
		interactionPromptText.text = prompt;
	}
	


	
	private bool IsItemOnCounter(GameObject item) 
	{
		if (item == null) { return false; } 

		RaycastHit downHit;
		float checkDistance = 0.6f; 
		Vector3 rayStart = item.transform.position + Vector3.up * 0.01f; 
																		

		bool didHit = Physics.Raycast(rayStart, Vector3.down, out downHit, checkDistance, counterLayerMask); 

		
		return didHit; 
	}

	private void CancelCuttingProcess() 
	{
		if (currentTargetCuttable != null)
		{
			currentTargetCuttable.CancelCutting();
			currentTargetCuttable = null; 
			Debug.Log("Cutting process canceled.");
		}
	}

	private void TryPickup(PickupableItem item, Collider itemCollider) 
	{
		if (item == null || currentlyHeldItem != null) return;

		currentlyHeldItem = item;
		heldItemRigidbody = item.Rb;
		heldItemCollider = itemCollider;

		if (heldItemRigidbody == null || heldItemCollider == null) { currentlyHeldItem = null; return; }

		
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
		

		item.transform.SetParent(holdPoint); 
		item.transform.position = holdPoint.position; 
		item.transform.rotation = holdPoint.rotation; 
		heldItemRigidbody.velocity = Vector3.zero;
		heldItemRigidbody.angularVelocity = Vector3.zero; 
		Debug.Log("Picked up: " + item.name); 
	}




	private void TryPlace()
	{
		if (currentlyHeldItem == null) return; 

		RaycastHit hit;
		
		if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, interactionDistance, placeLayerMask))
		{
			
			CounterSurface counter = hit.collider.GetComponent<CounterSurface>();
			if (counter != null)
			{
				
				Transform placementPoint = counter.GetPlacementPoint();
				if (placementPoint != null)
				{
					PlaceItem(placementPoint.position, placementPoint.rotation); 
					Debug.Log($"Placed item on Counter: {hit.collider.name} at designated spot.");
				}
				else
				{
					
					Debug.LogWarning($"Counter {hit.collider.name} has no placement point assigned. Placing at hit location.", counter);
					PlaceItemAtHitLocation(hit); 
				}
			}
			
			else
			{
				
				PlaceItemAtHitLocation(hit); 
				Debug.Log("Placed item on surface: " + hit.collider.name); 
			}
		}
		else
		{
			
			Debug.Log("No valid placement surface found. Still holding item."); 
																				
		}
	}

	
	private void PlaceItemAtHitLocation(RaycastHit hit)
	{
		
		PlaceItem(hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
	}
	


	private void PlaceItem(Vector3 position, Quaternion rotation) 
	{
		if (currentlyHeldItem == null) return; 
		GameObject itemGO = currentlyHeldItem.gameObject; 

		RestoreItemPhysics(); 
		itemGO.transform.SetParent(null); 

		
		Vector3 slightlyAbove = position + (rotation * Vector3.up * 0.01f); 
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
		if (itemRB != null) itemRB.AddForce(mainCamera.transform.forward * 1.5f, ForceMode.VelocityChange); 
		Debug.Log("Dropped: " + itemGO.name); 
		currentlyHeldItem = null; heldItemRigidbody = null; heldItemCollider = null; 
	}

	private void RestoreItemPhysics() 
	{
		if (currentlyHeldItem == null) return; 
		GameObject itemGO = currentlyHeldItem.gameObject; Rigidbody itemRB = heldItemRigidbody; Collider itemCol = heldItemCollider; 

		if (itemRB != null) { itemRB.useGravity = true; itemRB.velocity = Vector3.zero; itemRB.angularVelocity = Vector3.zero; } 

		if (itemGO != null) 
		{
			
			if (heldItemLayer != 0 && itemGO.layer == heldItemLayer)
			{
				itemGO.layer = originalLayer;
			}

			if (ignorePlayerCollision) 
			{
				Collider playerCollider = GetComponent<Collider>(); 
				if (playerCollider != null && itemCol != null) Physics.IgnoreCollision(itemCol, playerCollider, false); 
			}
		}
	}

	private void MoveHeldItem() 
	{
		if (currentlyHeldItem != null && heldItemRigidbody != null && holdPoint != null) 
		{
			Vector3 targetPosition = holdPoint.position; Quaternion targetRotation = holdPoint.rotation; 
			heldItemRigidbody.MovePosition(targetPosition); 
			heldItemRigidbody.MoveRotation(targetRotation); 
		}
	}
}