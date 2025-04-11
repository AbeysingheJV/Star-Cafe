using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
	[Header("Movement")]
	[SerializeField] private float walkSpeed = 5.0f;
	[SerializeField] private CharacterController characterController;

	[Header("Look Parameters")]
	[SerializeField] private float mouseSensitivity = 0.1f;
	[SerializeField] private float upDownLookRange = 80f;
	[SerializeField] private Camera mainCamera;



	private Vector3 currentMovement;
	private float verticalRotation;
	private PlayerInputHandler playerInputHandler;

	private void Awake()
	{
		// Ensure required components exist
		if (characterController == null)
			characterController = GetComponent<CharacterController>();

		if (mainCamera == null)
			mainCamera = Camera.main;

		playerInputHandler = GetComponent<PlayerInputHandler>();

		if (playerInputHandler == null)
		{
			Debug.LogError("PlayerInputHandler component missing from player!");
			enabled = false;
			return;
		}
	}

	private void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	private void Update()
	{
		HandleMovement();
		HandleRotation();
	}

	private void HandleMovement()
	{
		if (playerInputHandler == null) return;

		Vector2 input = playerInputHandler.MovementInput;
		Vector3 worldDirection = transform.TransformDirection(new Vector3(input.x, 0f, input.y));

		currentMovement.x = worldDirection.x * walkSpeed;
		currentMovement.z = worldDirection.z * walkSpeed;

		if (!characterController.isGrounded)
		{
			currentMovement.y += Physics.gravity.y * Time.deltaTime;
		}
		else
		{
			currentMovement.y = 0f;
		}

		characterController.Move(currentMovement * Time.deltaTime);
	}

	private void HandleRotation()
	{
		if (playerInputHandler == null) return;

		Vector2 input = playerInputHandler.RotationInput;
		float mouseXRotation = input.x * mouseSensitivity;
		float mouseYRotation = input.y * mouseSensitivity;

		transform.Rotate(0, mouseXRotation, 0);

		verticalRotation = Mathf.Clamp(verticalRotation - mouseYRotation, -upDownLookRange, upDownLookRange);
		mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
	}

}