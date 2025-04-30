using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInputHandler : MonoBehaviour
{
	[Header("Input Action Asset")]
	[SerializeField] private InputActionAsset playerControls;

	[Header("Action Map Name Reference")]
	[SerializeField] private string actionMapName = "Player"; 

	[Header("Action Name References")]
	[SerializeField] private string movement = "Movement";
	[SerializeField] private string rotation = "Rotation"; 
														   

	private InputAction movementAction;
	private InputAction rotationAction;
	

	public Vector2 MovementInput { get; private set; }
	public Vector2 RotationInput { get; private set; }

	

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
		

		
		if (movementAction == null || rotationAction == null) 
		{
			
			Debug.LogError("One or more Input Actions not found! (Movement, Rotation)");
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

		
	}

	

	private void OnEnable()
	{
		playerControls.FindActionMap(actionMapName).Enable(); 
	}

	private void OnDisable()
	{
		playerControls.FindActionMap(actionMapName).Disable();
	}
}