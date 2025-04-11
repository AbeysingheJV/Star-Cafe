// PlayerInputHandler.cs
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
	[SerializeField] private string interact = "Interact"; // For E key (Pickup/Place)
	[SerializeField] private string cookAction = "CookAction"; // *** NEW: For Q key (Cut/Cook etc.) ***

	private InputAction movementAction;
	private InputAction rotationAction;
	private InputAction interactAction;
	private InputAction cookActionInput; // *** NEW ***

	public Vector2 MovementInput { get; private set; }
	public Vector2 RotationInput { get; private set; }

	// Events for Interact (E)
	public UnityAction OnInteractStartedAction; // Called when E is pressed
	public UnityAction OnInteractCanceledAction; // Called when E is released

	// *** NEW: Events for CookAction (Q) ***
	public UnityAction OnCookActionStartedAction; // Called when Q is pressed
	public UnityAction OnCookActionCanceledAction; // Called when Q is released

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
		cookActionInput = mapReference.FindAction(cookAction); // *** NEW ***

		// Check if actions exist before subscribing
		if (movementAction == null || rotationAction == null || interactAction == null || cookActionInput == null) // *** Updated Check ***
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

		// *** NEW: CookAction (Q) ***
		cookActionInput.started += CookActionStarted;
		cookActionInput.canceled += CookActionCanceled;
	}

	// --- Handlers for Interact (E) ---
	private void InteractStarted(InputAction.CallbackContext context)
	{
		OnInteractStartedAction?.Invoke();
	}

	private void InteractCanceled(InputAction.CallbackContext context)
	{
		OnInteractCanceledAction?.Invoke();
	}

	// --- NEW: Handlers for CookAction (Q) ---
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
		// Unsubscribe events
		if (interactAction != null)
		{
			interactAction.started -= InteractStarted;
			interactAction.canceled -= InteractCanceled;
		}
		if (cookActionInput != null) // *** NEW ***
		{
			cookActionInput.started -= CookActionStarted;
			cookActionInput.canceled -= CookActionCanceled;
		}
		// ... (unsubscribe movement/rotation if needed, careful with anonymous methods)


		// Disable the action map
		playerControls.FindActionMap(actionMapName).Disable();
	}
}