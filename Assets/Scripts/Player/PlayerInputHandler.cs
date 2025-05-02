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

	
	public UnityAction OnInteractActionStarted; 
	public UnityAction OnCookActionStarted; 

	private void Awake()
	{
		InputActionMap mapReference = playerControls.FindActionMap(actionMapName);
		if (mapReference == null) { Debug.LogError($"Action Map '{actionMapName}' not found!"); enabled = false; return; }

		movementAction = mapReference.FindAction(movement);
		rotationAction = mapReference.FindAction(rotation);
		interactAction = mapReference.FindAction(interact);
		cookActionInput = mapReference.FindAction(cookAction); 

		
		if (movementAction == null || rotationAction == null || interactAction == null || cookActionInput == null) 
		{
			Debug.LogError("One or more Input Actions not found! (Movement, Rotation, Interact, CookAction)"); 
			enabled = false; return;
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
		// CookAction (Q) 
		cookActionInput.started += CookActionStarted;
	}

	private void InteractStarted(InputAction.CallbackContext context) { OnInteractActionStarted?.Invoke(); }
	
	private void CookActionStarted(InputAction.CallbackContext context) { OnCookActionStarted?.Invoke(); }

	private void OnEnable() { playerControls.FindActionMap(actionMapName).Enable(); }
	private void OnDisable()
	{
		
		if (interactAction != null) { interactAction.started -= InteractStarted; }
		
		if (cookActionInput != null) { cookActionInput.started -= CookActionStarted; }

		playerControls.FindActionMap(actionMapName).Disable();
	}
}