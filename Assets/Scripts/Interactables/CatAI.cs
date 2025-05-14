using UnityEngine;
using System.Collections;

public class CatAI : MonoBehaviour
{
	[Header("Animation Settings")]
	private Animator animator;
	private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
	private static readonly int IsSittingHash = Animator.StringToHash("IsSitting"); // For controlling sitting state
	private static readonly int PlayMiauHash = Animator.StringToHash("PlayMiau");
	private static readonly int PlaySitUpHash = Animator.StringToHash("PlaySitUp");


	[Header("Sound Settings")]
	[SerializeField] private AudioClip meowSound;
	private AudioSource audioSource;

	[Header("Wandering Settings (On Table)")]
	[SerializeField] private Collider tableCollider;
	[SerializeField] private float walkSpeed = 0.5f;
	[SerializeField] private float minWanderWaitTime = 3f;
	[SerializeField] private float maxWanderWaitTime = 8f;
	[SerializeField] private float rotationSpeed = 120f;
	[SerializeField] private float sitDuration = 5f; // How long the cat will sit before wandering again

	private Coroutine currentActionCoroutine; // To manage both wandering and sitting delays
	private bool isCurrentlySitting = false; // Script's internal state tracking

	public static CatAI Instance { get; private set; }

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
		if (tableCollider == null) Debug.LogWarning("CatAI: Table Collider not assigned. Cat will not wander initially.");
	}

	void Start()
	{
		if (tableCollider != null)
		{
			// Start wandering if a table is assigned and not already sitting
			if (!isCurrentlySitting)
			{
				StartWandering();
			}
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
		if (isCurrentlySitting) // If already sitting, maybe stand up and wander? Or do nothing.
		{
			Debug.Log("Cat is already sitting. Making it stand and wander.");
			StandUpAndWander();
			return;
		}

		StopCurrentAction(); // Stop wandering or any other timed action

		isCurrentlySitting = true;
		if (animator != null)
		{
			animator.SetBool(IsWalkingHash, false); // Ensure not trying to walk
			animator.SetTrigger(PlaySitUpHash);     // Play the "sit down" animation
			animator.SetBool(IsSittingHash, true);  // Tell animator to enter/stay in sitting loop
			Debug.Log("Cat: PlaySitUp trigger set, IsSitting set to true.");
		}
		PlayMeowSound();

		// Start coroutine to stand up and wander after a delay
		currentActionCoroutine = StartCoroutine(ResumeWanderingAfterSitDelay());
	}

	private void StandUpAndWander()
	{
		StopCurrentAction(); // Stop the sit delay if it was active

		isCurrentlySitting = false;
		if (animator != null)
		{
			animator.SetBool(IsSittingHash, false); // Tell animator to transition out of sitting
			Debug.Log("Cat: IsSitting set to false (to stand up).");
		}
		PlayMeowSound(); // Optional: meow when standing from interaction

		// Give a very short delay for stand-up animation to start/play
		// before immediately trying to walk.
		currentActionCoroutine = StartCoroutine(DelayedStartWandering(0.5f));
	}


	private IEnumerator ResumeWanderingAfterSitDelay()
	{
		yield return new WaitForSeconds(sitDuration);

		if (isCurrentlySitting) // Only proceed if still meant to be sitting
		{
			Debug.Log("Cat: Sit duration over. Standing up to wander.");
			isCurrentlySitting = false;
			if (animator != null)
			{
				animator.SetBool(IsSittingHash, false); // Trigger stand up
			}
			// Wait a brief moment for stand-up animation to potentially start
			yield return new WaitForSeconds(0.5f); // Adjust if you have a long stand-up animation
			StartWandering();
		}
	}

	private IEnumerator DelayedStartWandering(float delay)
	{
		yield return new WaitForSeconds(delay);
		StartWandering();
	}


	// --- Wandering Logic ---
	private void StartWandering()
	{
		if (tableCollider == null)
		{
			Debug.Log("Cat: Cannot wander, tableCollider not set.");
			return;
		}
		StopCurrentAction(); // Make sure no other action coroutine is running

		isCurrentlySitting = false; // Ensure not marked as sitting
		if (animator != null)
		{
			animator.SetBool(IsSittingHash, false); // Make sure it's not stuck in sitting animation
		}
		currentActionCoroutine = StartCoroutine(WanderOnTableRoutine());
	}

	private void StopCurrentAction()
	{
		if (currentActionCoroutine != null)
		{
			StopCoroutine(currentActionCoroutine);
			currentActionCoroutine = null;
		}
		// Reset animation states that might be stuck if action was interrupted
		if (animator != null)
		{
			animator.SetBool(IsWalkingHash, false);
		}
	}


	private IEnumerator WanderOnTableRoutine()
	{
		Debug.Log("Cat: Starting WanderOnTableRoutine.");
		while (true) // Loop indefinitely until explicitly stopped
		{
			// 1. Ensure not sitting and set to idle animation
			if (animator != null)
			{
				animator.SetBool(IsWalkingHash, false);
				animator.SetBool(IsSittingHash, false); // Double ensure
			}

			// 2. Wait for a random duration
			float waitTime = Random.Range(minWanderWaitTime, maxWanderWaitTime);
			Debug.Log($"Cat: Wandering - waiting for {waitTime} seconds.");
			yield return new WaitForSeconds(waitTime);

			// 3. Pick a random point on the table
			Vector3 randomPointOnTable = GetRandomPointOnTable();
			Debug.Log($"Cat: Wandering - new target point: {randomPointOnTable}");

			// 4. Rotate towards the point
			if (Vector3.Distance(transform.position, randomPointOnTable) > 0.01f) // Only rotate if not already there
			{
				Quaternion targetRotation = Quaternion.LookRotation(randomPointOnTable - transform.position);
				targetRotation.x = 0;
				targetRotation.z = 0;

				float angleToTarget = Quaternion.Angle(transform.rotation, targetRotation);
				if (angleToTarget > 1f) // Only rotate if significant angle
				{
					Debug.Log("Cat: Wandering - Rotating.");
					float rotateDuration = angleToTarget / rotationSpeed;
					float t = 0;
					Quaternion initialRotation = transform.rotation;
					while (t < rotateDuration)
					{
						transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, t / rotateDuration);
						t += Time.deltaTime;
						yield return null;
					}
					transform.rotation = targetRotation;
				}
			}


			// 5. Walk to the point
			if (animator != null) animator.SetBool(IsWalkingHash, true);
			Debug.Log("Cat: Wandering - Walking.");
			Vector3 initialPosition = transform.position;
			float distanceToTarget = Vector3.Distance(initialPosition, randomPointOnTable);
			float walkDuration = distanceToTarget / walkSpeed;
			float tWalk = 0;

			while (tWalk < walkDuration)
			{
				transform.position = Vector3.MoveTowards(transform.position, randomPointOnTable, walkSpeed * Time.deltaTime);
				tWalk += Time.deltaTime;
				yield return null;
			}
			transform.position = randomPointOnTable;
			if (animator != null) animator.SetBool(IsWalkingHash, false);
			Debug.Log("Cat: Wandering - Reached target.");
		}
	}

	private Vector3 GetRandomPointOnTable()
	{
		Bounds tableBounds = tableCollider.bounds;
		float randomX = Random.Range(tableBounds.min.x, tableBounds.max.x);
		float randomZ = Random.Range(tableBounds.min.z, tableBounds.max.z);
		float yPos = tableBounds.max.y; // Adjust if cat's pivot isn't at its base
		return new Vector3(randomX, yPos, randomZ);
	}

	private void PlayMeowSound()
	{
		if (audioSource != null && meowSound != null)
		{
			audioSource.PlayOneShot(meowSound);
		}
		else if (meowSound == null) Debug.LogWarning("CatAI: Meow sound clip not assigned.");
	}
}