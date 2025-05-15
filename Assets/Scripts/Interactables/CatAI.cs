using UnityEngine;
using System.Collections;

public class CatAI : MonoBehaviour
{
	[Header("Animation Settings")]
	private Animator animator;
	private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
	private static readonly int IsSittingHash = Animator.StringToHash("IsSitting");
	private static readonly int PlayMiauHash = Animator.StringToHash("PlayMiau");
	private static readonly int PlaySitUpHash = Animator.StringToHash("PlaySitUp");

	[Header("Sound Settings")]
	[SerializeField] private AudioClip meowSound;
	private AudioSource audioSource;

	[Header("VFX Settings")]
	[SerializeField] private ParticleSystem heartPetVFXPrefab; // Assign your Heart VFX Prefab here
	[SerializeField] private Transform heartSpawnPoint;      // Assign the empty child GameObject here

	[Header("Wandering Settings (On Table)")]
	[SerializeField] private Collider tableCollider;
	[SerializeField] private float walkSpeed = 0.5f;
	[SerializeField] private float minWanderWaitTime = 3f;
	[SerializeField] private float maxWanderWaitTime = 8f;
	[SerializeField] private float rotationSpeed = 120f;
	[SerializeField] private float sitDuration = 5f;

	private Coroutine currentActionCoroutine;
	private bool isCurrentlySitting = false;

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

		// Setup default heart spawn point if not assigned
		if (heartSpawnPoint == null)
		{
			Debug.LogWarning("CatAI: Heart Spawn Point not assigned. Defaulting to cat's own transform. Hearts may not appear as intended.");
			heartSpawnPoint = transform; // Default to the cat's own position
		}
		if (heartPetVFXPrefab == null)
		{
			Debug.LogWarning("CatAI: Heart Pet VFX Prefab not assigned. Heart effect will not play.");
		}
	}

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

	public void TriggerMiauAndSound() // Called by OrderManager
	{
		if (animator != null)
		{
			animator.SetTrigger(PlayMiauHash);
		}
		PlayMeowSound();
		// Optionally, play hearts here too if you want the cat to show hearts for completed orders
		// PlayHeartVFX();
	}

	public void TriggerSitUpAndSound() // Called by PickupController when player interacts (pets)
	{
		if (isCurrentlySitting)
		{
			StandUpAndWander(); // If already sitting, stand up
			PlayHeartVFX();     // Still play hearts because player interacted
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
		PlayHeartVFX(); // --- Play Heart VFX on Pet ---

		currentActionCoroutine = StartCoroutine(ResumeWanderingAfterSitDelay());
	}

	private void PlayHeartVFX()
	{
		if (heartPetVFXPrefab != null && heartSpawnPoint != null)
		{
			// Instantiate the VFX at the heartSpawnPoint's position and rotation
			ParticleSystem heartsInstance = Instantiate(heartPetVFXPrefab, heartSpawnPoint.position, heartSpawnPoint.rotation);
			heartsInstance.Play(); // Should play automatically if PlayOnAwake is false and it's a burst

			// If the particle system doesn't destroy itself (e.g., Stop Action is not Destroy),
			// destroy it after its estimated duration.
			// Get the maximum lifetime of particles in the system to estimate when to destroy.
			float maxLifetime = heartsInstance.main.duration + heartsInstance.main.startLifetime.constantMax;
			Destroy(heartsInstance.gameObject, maxLifetime + 0.5f); // Add a small buffer
			Debug.Log("CatAI: Played Heart VFX.");
		}
		else
		{
			if (heartPetVFXPrefab == null) Debug.LogWarning("CatAI: HeartPetVFXPrefab is not assigned.");
			if (heartSpawnPoint == null) Debug.LogWarning("CatAI: HeartSpawnPoint is not assigned.");
		}
	}

	private void StandUpAndWander()
	{
		StopCurrentAction();
		isCurrentlySitting = false;
		if (animator != null)
		{
			animator.SetBool(IsSittingHash, false);
		}
		// PlayMeowSound(); // Meow sound is already played by TriggerSitUpAndSound if called from there
		currentActionCoroutine = StartCoroutine(DelayedStartWandering(0.5f));
	}

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

	private IEnumerator DelayedStartWandering(float delay)
	{
		yield return new WaitForSeconds(delay);
		StartWandering();
	}

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
			if (walkSpeed <= 0) walkSpeed = 0.1f; // Prevent division by zero if speed is not set
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

	private Vector3 GetRandomPointOnTable()
	{
		Bounds tableBounds = tableCollider.bounds;
		float randomX = Random.Range(tableBounds.min.x, tableBounds.max.x);
		float randomZ = Random.Range(tableBounds.min.z, tableBounds.max.z);
		float yPos = tableBounds.max.y;
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