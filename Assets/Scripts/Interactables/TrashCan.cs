using UnityEngine;

[RequireComponent(typeof(Collider))] // Ensure there's a collider on this GameObject
public class TrashCan : MonoBehaviour
{
	[Header("Feedback (Optional)")]
	[SerializeField] private AudioClip trashSound;        // Sound to play when item is trashed
	[SerializeField] private ParticleSystem trashVFXPrefab; // VFX to play when item is trashed
	[SerializeField] private Transform vfxSpawnPoint;       // Where the VFX should spawn (e.g., top of can)

	private AudioSource audioSource;

	void Awake()
	{
		// Ensure the collider is set to be a trigger
		Collider col = GetComponent<Collider>();
		if (!col.isTrigger)
		{
			Debug.LogWarning($"TrashCan on {gameObject.name}: Collider was not set to 'Is Trigger'. Forcing it now. Please check its setup.", this);
			col.isTrigger = true;
		}

		// Get or add an AudioSource for sound effects
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null && trashSound != null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.playOnAwake = false;
		}

		// If no specific VFX spawn point is set, default to this object's transform
		if (vfxSpawnPoint == null)
		{
			vfxSpawnPoint = transform;
		}
	}

	void OnTriggerEnter(Collider other)
	{
		// Check if the object entering the trigger has a Pickupable script
		Pickupable pickupableItem = other.GetComponent<Pickupable>();

		if (pickupableItem != null)
		{
			// Important: Check if the item is actually "dropped" and not still held by the player.
			// Dropped items usually have their Rigidbody's isKinematic set to false.
			// Held items (by your PickupController) usually have isKinematic set to true.
			Rigidbody itemRb = pickupableItem.Rb; // Assuming Pickupable has a public Rigidbody property named Rb

			if (itemRb != null && !itemRb.isKinematic)
			{
				Debug.Log($"TrashCan: Item '{other.gameObject.name}' entered the trash and is not kinematic. Destroying it.");

				// Play sound effect
				if (audioSource != null && trashSound != null)
				{
					audioSource.PlayOneShot(trashSound);
				}

				// Play particle effect
				if (trashVFXPrefab != null && vfxSpawnPoint != null)
				{
					ParticleSystem vfxInstance = Instantiate(trashVFXPrefab, vfxSpawnPoint.position, vfxSpawnPoint.rotation);
					vfxInstance.Play();
					// Destroy the VFX instance after its duration + particle lifetime
					float vfxDuration = vfxInstance.main.duration + vfxInstance.main.startLifetime.constantMax;
					Destroy(vfxInstance.gameObject, vfxDuration + 0.5f); // Add buffer
				}

				// Destroy the item
				Destroy(other.gameObject);
			}
			else if (itemRb != null && itemRb.isKinematic)
			{
				// This means the player is likely still holding it and just passed through the trigger area.
				// Debug.Log($"TrashCan: Item '{other.gameObject.name}' passed through but is kinematic (likely held). Ignoring.");
			}
			else if (itemRb == null)
			{
				Debug.LogWarning($"TrashCan: Pickupable item '{other.gameObject.name}' has no Rigidbody. Cannot determine if dropped. Ignoring.");
			}
		}
	}
}
