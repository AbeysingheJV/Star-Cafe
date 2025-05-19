using UnityEngine;
using System.Collections;

public class CatAI : MonoBehaviour
{
	public static CatAI Instance { get; private set; }

	[Header("Animation Settings")]
	private Animator animator; // Component for controlling cat animations.
	private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking"); // Hash for IsWalking animation parameter.
	private static readonly int IsSittingHash = Animator.StringToHash("IsSitting"); // Hash for IsSitting animation parameter.
	private static readonly int PlayMiauHash = Animator.StringToHash("PlayMiau");   // Hash for PlayMiau animation trigger.
	private static readonly int PlaySitUpHash = Animator.StringToHash("PlaySitUp"); // Hash for PlaySitUp animation trigger.

	[Header("Sound Settings")]
	[SerializeField] private AudioClip meowSound; // Sound clip for cat meow.
	private AudioSource audioSource; // Component for playing cat sounds.

	[Header("VFX Settings")]
	[SerializeField] private ParticleSystem heartPetVFXPrefab; // Particle effect for when petted.
	[SerializeField] private Transform heartSpawnPoint;      // Where the heart VFX appears.

	[Header("Wandering Settings (On Table)")]
	[SerializeField] private Collider tableCollider; // Collider defining the area cat can walk on.
	[SerializeField] private float walkSpeed = 0.5f; // How fast the cat walks.
	[SerializeField] private float minWanderWaitTime = 3f; // Minimum time cat waits before walking again.
	[SerializeField] private float maxWanderWaitTime = 8f; // Maximum time cat waits.
	[SerializeField] private float rotationSpeed = 120f; // How fast the cat rotates.
	[SerializeField] private float sitDuration = 5f; // How long the cat sits when petted.

	public string interactionPrompt = "Pet"; // Text prompt for player interaction.

	private Coroutine currentActionCoroutine; // Stores the current action coroutine (like wandering).
	private bool isCurrentlySitting = false; // Is the cat currently in a sitting state?

	// Called when the script instance is being loaded.
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

		if (heartSpawnPoint == null)
		{
			Debug.LogWarning("CatAI: Heart Spawn Point not assigned. Defaulting to cat's own transform. Hearts may not appear as intended.");
			heartSpawnPoint = transform;
		}
		if (heartPetVFXPrefab == null)
		{
			Debug.LogWarning("CatAI: Heart Pet VFX Prefab not assigned. Heart effect will not play.");
		}
	}

	// Called before the first frame update.
	void Start()
	{
		if (tableCollider != null)
		{
			if (!isCurrentlySitting)
			{
				StartWandering();
			}
		}
	}

	// Triggers the cat's miau animation and sound.
	public void TriggerMiauAndSound()
	{
		if (animator != null)
		{
			animator.SetTrigger(PlayMiauHash);
		}
		PlayMeowSound();
	}

	// Triggers the cat to sit up (or stand up if already sitting) when petted.
	public void TriggerSitUpAndSound()
	{
		if (isCurrentlySitting)
		{
			StandUpAndWander();
			PlayHeartVFX();
			return;
		}

		StopCurrentAction();

		isCurrentlySitting = true;
		if (animator != null)
		{
			animator.SetBool(IsWalkingHash, false);
			animator.SetTrigger(PlaySitUpHash);
			animator.SetBool(IsSittingHash, true);
		}
		PlayMeowSound();
		PlayHeartVFX();

		currentActionCoroutine = StartCoroutine(ResumeWanderingAfterSitDelay());
	}

	// Plays the heart particle effect.
	private void PlayHeartVFX()
	{
		if (heartPetVFXPrefab != null && heartSpawnPoint != null)
		{
			ParticleSystem heartsInstance = Instantiate(heartPetVFXPrefab, heartSpawnPoint.position, heartSpawnPoint.rotation);
			heartsInstance.Play();
			float maxLifetime = heartsInstance.main.duration + heartsInstance.main.startLifetime.constantMax;
			Destroy(heartsInstance.gameObject, maxLifetime + 0.5f);
			Debug.Log("CatAI: Played Heart VFX.");
		}
		else
		{
			if (heartPetVFXPrefab == null) Debug.LogWarning("CatAI: HeartPetVFXPrefab is not assigned.");
			if (heartSpawnPoint == null) Debug.LogWarning("CatAI: HeartSpawnPoint is not assigned.");
		}
	}

	// Makes the cat stand up from a sitting position and start wandering.
	private void StandUpAndWander()
	{
		StopCurrentAction();
		isCurrentlySitting = false;
		if (animator != null)
		{
			animator.SetBool(IsSittingHash, false);
		}
		currentActionCoroutine = StartCoroutine(DelayedStartWandering(0.5f));
	}

	// Coroutine to make the cat resume wandering after a sitting duration.
	private IEnumerator ResumeWanderingAfterSitDelay()
	{
		yield return new WaitForSeconds(sitDuration);
		if (isCurrentlySitting)
		{
			isCurrentlySitting = false;
			if (animator != null)
			{
				animator.SetBool(IsSittingHash, false);
			}
			yield return new WaitForSeconds(0.5f);
			StartWandering();
		}
	}

	// Coroutine to start wandering after a specified delay.
	private IEnumerator DelayedStartWandering(float delay)
	{
		yield return new WaitForSeconds(delay);
		StartWandering();
	}

	// Initiates the cat's wandering behavior.
	private void StartWandering()
	{
		if (tableCollider == null) return;
		StopCurrentAction();
		isCurrentlySitting = false;
		if (animator != null)
		{
			animator.SetBool(IsSittingHash, false);
		}
		currentActionCoroutine = StartCoroutine(WanderOnTableRoutine());
	}

	// Stops the cat's current action (like wandering or sitting).
	private void StopCurrentAction()
	{
		if (currentActionCoroutine != null)
		{
			StopCoroutine(currentActionCoroutine);
			currentActionCoroutine = null;
		}
		if (animator != null)
		{
			animator.SetBool(IsWalkingHash, false);
		}
	}

	// handles the cat's wandering logic on the table.
	private IEnumerator WanderOnTableRoutine()
	{
		while (true)
		{
			if (animator != null)
			{
				animator.SetBool(IsWalkingHash, false);
				animator.SetBool(IsSittingHash, false);
			}
			float waitTime = Random.Range(minWanderWaitTime, maxWanderWaitTime);
			yield return new WaitForSeconds(waitTime);
			Vector3 randomPointOnTable = GetRandomPointOnTable();
			if (Vector3.Distance(transform.position, randomPointOnTable) > 0.01f)
			{
				Quaternion targetRotation = Quaternion.LookRotation(randomPointOnTable - transform.position);
				targetRotation.x = 0; targetRotation.z = 0;
				if (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
				{
					float rotateDuration = Quaternion.Angle(transform.rotation, targetRotation) / rotationSpeed;
					float t = 0; Quaternion initialRotation = transform.rotation;
					while (t < rotateDuration)
					{
						transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, t / rotateDuration);
						t += Time.deltaTime; yield return null;
					}
					transform.rotation = targetRotation;
				}
			}
			if (animator != null) animator.SetBool(IsWalkingHash, true);
			Vector3 initialPosition = transform.position;
			float distanceToTarget = Vector3.Distance(initialPosition, randomPointOnTable);
			if (walkSpeed <= 0) walkSpeed = 0.1f;
			float walkDuration = distanceToTarget / walkSpeed;
			float tWalk = 0;
			while (tWalk < walkDuration)
			{
				transform.position = Vector3.MoveTowards(transform.position, randomPointOnTable, walkSpeed * Time.deltaTime);
				tWalk += Time.deltaTime; yield return null;
			}
			transform.position = randomPointOnTable;
			if (animator != null) animator.SetBool(IsWalkingHash, false);
		}
	}

	// Calculates a random point within the bounds of the table for the cat to wander to.
	private Vector3 GetRandomPointOnTable()
	{
		Bounds tableBounds = tableCollider.bounds;
		float randomX = Random.Range(tableBounds.min.x, tableBounds.max.x);
		float randomZ = Random.Range(tableBounds.min.z, tableBounds.max.z);
		float yPos = tableBounds.max.y;
		return new Vector3(randomX, yPos, randomZ);
	}

	// Plays the cat's meow sound effect.
	private void PlayMeowSound()
	{
		if (audioSource != null && meowSound != null)
		{
			audioSource.PlayOneShot(meowSound);
		}
		else if (meowSound == null) Debug.LogWarning("CatAI: Meow sound clip not assigned.");
	}
}