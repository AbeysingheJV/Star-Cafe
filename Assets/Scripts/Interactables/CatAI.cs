using UnityEngine;
using System.Collections;

public class CatAI : MonoBehaviour
{
	[Header("Animation Settings")]
	private Animator animator;
	private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
	private static readonly int PlayMiauHash = Animator.StringToHash("PlayMiau");
	private static readonly int PlaySitUpHash = Animator.StringToHash("PlaySitUp");
	// Add IsSittingHash if you implement a separate standing up logic
	// private static readonly int IsSittingHash = Animator.StringToHash("IsSitting");


	[Header("Sound Settings")]
	[SerializeField] private AudioClip meowSound;
	private AudioSource audioSource;

	[Header("Wandering Settings (On Table)")]
	[SerializeField] private Collider tableCollider; // Assign the table's collider
	[SerializeField] private float walkSpeed = 0.5f;
	[SerializeField] private float minWanderWaitTime = 2f;
	[SerializeField] private float maxWanderWaitTime = 7f;
	[SerializeField] private float rotationSpeed = 120f; // Degrees per second
	private Coroutine wanderCoroutine;
	private bool isWandering = false;

	public static CatAI Instance { get; private set; } // Simple Singleton

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;

		animator = GetComponent<Animator>();
		audioSource = GetComponent<AudioSource>();

		if (animator == null) Debug.LogError("CatAI: Animator component not found!");
		if (audioSource == null) Debug.LogError("CatAI: AudioSource component not found! Please add one.");
		if (tableCollider == null) Debug.LogWarning("CatAI: Table Collider not assigned. Cat will not wander.");
	}

	void Start()
	{
		if (tableCollider != null)
		{
			// Start wandering if a table is assigned
			wanderCoroutine = StartCoroutine(WanderOnTableRoutine());
		}
	}

	// --- Public Methods to be called by other scripts ---

	public void TriggerMiauAndSound()
	{
		if (animator != null)
		{
			animator.SetTrigger(PlayMiauHash);
			Debug.Log("Cat: PlayMiau trigger set.");
		}
		PlayMeowSound();
	}

	public void TriggerSitUpAndSound()
	{
		if (isWandering && wanderCoroutine != null) // Stop wandering if told to sit
		{
			StopCoroutine(wanderCoroutine);
			isWandering = false;
			if (animator != null) animator.SetBool(IsWalkingHash, false);
		}

		if (animator != null)
		{
			animator.SetTrigger(PlaySitUpHash);
			// If you want it to stay sitting, you might set a bool:
			// animator.SetBool(IsSittingHash, true);
			// animator.SetBool(IsWalkingHash, false);
			Debug.Log("Cat: PlaySitUp trigger set.");
		}
		PlayMeowSound();
	}

	// --- Wandering Logic ---
	private IEnumerator WanderOnTableRoutine()
	{
		isWandering = true;
		while (isWandering && tableCollider != null)
		{
			// 1. Wait for a random duration
			float waitTime = Random.Range(minWanderWaitTime, maxWanderWaitTime);
			yield return new WaitForSeconds(waitTime);

			if (!isWandering) yield break; // Stop if commanded to do something else

			// 2. Pick a random point on the table
			Vector3 randomPointOnTable = GetRandomPointOnTable();

			// 3. Rotate towards the point
			Quaternion targetRotation = Quaternion.LookRotation(randomPointOnTable - transform.position);
			targetRotation.x = 0; // Keep cat upright
			targetRotation.z = 0;

			float angleToTarget = Quaternion.Angle(transform.rotation, targetRotation);
			float rotateDuration = angleToTarget / rotationSpeed;

			if (animator != null) animator.SetBool(IsWalkingHash, false); // Idle while turning

			float t = 0;
			Quaternion initialRotation = transform.rotation;
			while (t < rotateDuration)
			{
				transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, t / rotateDuration);
				t += Time.deltaTime;
				yield return null;
			}
			transform.rotation = targetRotation; // Ensure exact rotation

			if (!isWandering) yield break;

			// 4. Walk to the point
			if (animator != null) animator.SetBool(IsWalkingHash, true);
			float distanceToTarget = Vector3.Distance(transform.position, randomPointOnTable);
			float walkDuration = distanceToTarget / walkSpeed;

			t = 0;
			Vector3 initialPosition = transform.position;
			while (t < walkDuration)
			{
				transform.position = Vector3.MoveTowards(transform.position, randomPointOnTable, walkSpeed * Time.deltaTime);
				// Make sure cat stays on table Y level if it drifts
				Vector3 currentPos = transform.position;
				currentPos.y = tableCollider.bounds.max.y + (transform.localScale.y * 0.5f); // Assuming cat pivot is at its base
																							 // Or more simply, if the cat is parented to the table, its local Y can be kept constant.
																							 // transform.position = currentPos;

				t += Time.deltaTime;
				yield return null;
				if (!isWandering) // Check if we should stop mid-walk
				{
					if (animator != null) animator.SetBool(IsWalkingHash, false);
					yield break;
				}
			}
			transform.position = randomPointOnTable; // Ensure exact position
			if (animator != null) animator.SetBool(IsWalkingHash, false);
		}
		isWandering = false;
	}

	private Vector3 GetRandomPointOnTable()
	{
		Bounds tableBounds = tableCollider.bounds;
		float randomX = Random.Range(tableBounds.min.x, tableBounds.max.x);
		float randomZ = Random.Range(tableBounds.min.z, tableBounds.max.z);

		// Assuming the cat should be on top of the table.
		// The Y position should be the top of the table + half the cat's height (if pivot is at base)
		// For simplicity, if the cat is a child of the table, you can work in local coordinates.
		// Here, we assume world coordinates and the cat is not necessarily a child.
		float yPos = tableBounds.max.y; // This might need adjustment based on cat's pivot point

		return new Vector3(randomX, yPos, randomZ);
	}


	// --- Sound ---
	private void PlayMeowSound()
	{
		if (audioSource != null && meowSound != null)
		{
			audioSource.PlayOneShot(meowSound);
		}
		else if (meowSound == null)
		{
			Debug.LogWarning("CatAI: Meow sound clip not assigned.");
		}
	}

	// Call this if you want to stop wandering externally (e.g. before sitting)
	public void StopWandering()
	{
		if (isWandering && wanderCoroutine != null)
		{
			StopCoroutine(wanderCoroutine);
			isWandering = false;
			if (animator != null)
			{
				animator.SetBool(IsWalkingHash, false);
			}
			Debug.Log("Cat: Wandering stopped.");
		}
	}
}