using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DeliveryTray : MonoBehaviour
{
	private OrderManager orderManager;
	private Collider trayCollider;

	void Awake()
	{
		orderManager = OrderManager.Instance; // Find the OrderManager instance
		trayCollider = GetComponent<Collider>();

		if (!trayCollider.isTrigger)
		{
			Debug.LogWarning($"DeliveryTray on {gameObject.name}: Collider is not set to 'Is Trigger'. It needs to be a trigger to detect items.", this);
			// Optionally force it: trayCollider.isTrigger = true;
		}

		if (orderManager == null)
		{
			Debug.LogError($"DeliveryTray on {gameObject.name} could not find the OrderManager instance! Make sure an OrderManager exists in the scene.", this);
			enabled = false; // Disable script if OrderManager is missing
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (orderManager == null) return; // Safety check

		// Check if the object entering has a Pickupable component (meaning it's likely a dish)
		// and importantly, if its Rigidbody is NOT kinematic (meaning it was dropped, not held)
		Pickupable pickupable = other.GetComponent<Pickupable>();
		if (pickupable != null && pickupable.Rb != null && !pickupable.Rb.isKinematic)
		{
			Debug.Log($"DeliveryTray: Detected item {other.gameObject.name} with tag {other.tag}");

			// Ask the OrderManager if this dish matches the current order
			if (orderManager.CheckOrderCompletion(other.gameObject))
			{
				// If it matches:
				orderManager.OrderCompleted(); // Tell the manager the order is done
				Destroy(other.gameObject); // Destroy the submitted dish
			}
			else
			{
				// Optional: Add feedback if the wrong dish is submitted (e.g., a sound, particle effect)
				Debug.Log($"DeliveryTray: Item {other.gameObject.name} is not the correct order.");
			}
		}
	}
}
