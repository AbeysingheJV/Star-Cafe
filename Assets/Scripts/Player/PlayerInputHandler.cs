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

	private InputAction movementAction; 
	private InputAction rotationAction; 
	private InputAction interactAction; 

	public Vector2 MovementInput { get; private set; } 
	public Vector2 RotationInput { get; private set; } 

	
	public UnityAction OnInteractActionStarted; 

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

		
		if (movementAction == null || rotationAction == null || interactAction == null) 
		{
			
			Debug.LogError("One or more Input Actions not found! (Movement, Rotation, Interact)"); 
			enabled = false; 
			return; 
		}

		SubscribeActionValuesToInputEvents(); 
	}

	private void SubscribeActionValuesToInputEvents()
	{
		
		movementAction.performed += ctx => MovementInput = ctx.ReadValue<Vector2>(); 
		movementAction.canceled += ctx => MovementInput = Vector2.zero; 

		
		rotationAction.performed += ctx => RotationInput = ctx.ReadValue<Vector2>(); 
		rotationAction.canceled += ctx => RotationInput = Vector2.zero; 

		
		interactAction.started += InteractStarted; 
	}

	
	private void InteractStarted(InputAction.CallbackContext context) 
	{
		OnInteractActionStarted?.Invoke(); 
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
		}

		playerControls.FindActionMap(actionMapName).Disable(); 
	}
}