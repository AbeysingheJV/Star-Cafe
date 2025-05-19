using UnityEngine;

// Requires a Collider component to be attached.
[RequireComponent(typeof(Collider))]
public class DeliveryTray : MonoBehaviour
{
	private OrderManager orderManager; // Reference to the OrderManager.
	private Collider trayCollider; // This GameObject's collider.

	// Called when the script instance is being loaded.
	void Awake()
	{
		orderManager = OrderManager.Instance;
		trayCollider = GetComponent<Collider>();

		if (!trayCollider.isTrigger)
		{
			Debug.LogWarning($"DeliveryTray on {gameObject.name}: Collider is not set to 'Is Trigger'. It needs to be a trigger to detect items.", this);
		}

		if (orderManager == null)
		{
			Debug.LogError($"DeliveryTray on {gameObject.name} could not find the OrderManager instance! Make sure an OrderManager exists in the scene.", this);
			enabled = false;
		}
	}

	// Called when another Collider enters this GameObject's trigger.
	void OnTriggerEnter(Collider other)
	{
		if (orderManager == null) return;

		Pickupable pickupable = other.GetComponent<Pickupable>();
		if (pickupable != null && pickupable.Rb != null && !pickupable.Rb.isKinematic)
		{
			Debug.Log($"DeliveryTray: Detected item {other.gameObject.name} with tag {other.tag}");

			if (orderManager.CheckOrderCompletion(other.gameObject))
			{
				orderManager.OrderCompleted();
				Destroy(other.gameObject);
			}
			else
			{
				Debug.Log($"DeliveryTray: Item {other.gameObject.name} is not the correct order.");
			}
		}
	}
}