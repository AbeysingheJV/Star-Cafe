using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class PlayerInputHandler : MonoBehaviour
{
	[Header("Input Action Asset")]
	[SerializeField] private InputActionAsset playerControls;

	[Header("Action Map Name Reference")]
	[SerializeField] private string actionMapName = "Player";

	[Header("Action Name References")]
	[SerializeField] private string movement = "Movement";
	[SerializeField] private string rotation = "Rotation";
	[SerializeField] private string interact = "Interact";
	[SerializeField] private string cookAction = "CookAction"; 

	private InputAction movementAction;
	private InputAction rotationAction;
	private InputAction interactAction;
	private InputAction cookActionInput; 

	public Vector2 MovementInput { get; private set; }
	public Vector2 RotationInput { get; private set; }

	
	public UnityAction OnInteractStartedAction;
	public UnityAction OnInteractCanceledAction; 

	
	public UnityAction OnCookActionStartedAction; 
	public UnityAction OnCookActionCanceledAction; 

	private void Awake()
	{
		InputActionMap mapReference = playerControls.FindActionMap(actionMapName);
		if (mapReference == null)
		{
			Debug.LogError($"Action Map '{actionMapName}' not found!");
			enabled = false;
			return;
		}

		movementAction = mapReference.FindAction(movement);
		rotationAction = mapReference.FindAction(rotation);
		interactAction = mapReference.FindAction(interact);
		cookActionInput = mapReference.FindAction(cookAction);

		
		if (movementAction == null || rotationAction == null || interactAction == null || cookActionInput == null) 
		{
			Debug.LogError("One or more Input Actions not found! (Movement, Rotation, Interact, CookAction)");
			enabled = false;
			return;
		}

		SubscribeActionValuesToInputEvents();
	}

	private void SubscribeActionValuesToInputEvents()
	{
		// Movement
		movementAction.performed += ctx => MovementInput = ctx.ReadValue<Vector2>();
		movementAction.canceled += ctx => MovementInput = Vector2.zero;

		// Rotation
		rotationAction.performed += ctx => RotationInput = ctx.ReadValue<Vector2>();
		rotationAction.canceled += ctx => RotationInput = Vector2.zero;

		// Interact (E)
		interactAction.started += InteractStarted;
		interactAction.canceled += InteractCanceled;

		//CookAction (Q)
		cookActionInput.started += CookActionStarted;
		cookActionInput.canceled += CookActionCanceled;
	}

	
	private void InteractStarted(InputAction.CallbackContext context)
	{
		OnInteractStartedAction?.Invoke();
	}

	private void InteractCanceled(InputAction.CallbackContext context)
	{
		OnInteractCanceledAction?.Invoke();
	}

	
	private void CookActionStarted(InputAction.CallbackContext context)
	{
		OnCookActionStartedAction?.Invoke();
	}

	private void CookActionCanceled(InputAction.CallbackContext context)
	{
		OnCookActionCanceledAction?.Invoke();
	}


	private void OnEnable()
	{
		playerControls.FindActionMap(actionMapName).Enable();
	}

	private void OnDisable()
	{
		
		if (interactAction != null)
		{
			interactAction.started -= InteractStarted;
			interactAction.canceled -= InteractCanceled;
		}
		if (cookActionInput != null) 
		{
			cookActionInput.started -= CookActionStarted;
			cookActionInput.canceled -= CookActionCanceled;
		}
		


	
		playerControls.FindActionMap(actionMapName).Disable();
	}
}