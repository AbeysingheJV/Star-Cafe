using UnityEngine;

[RequireComponent(typeof(Rigidbody))] 
public class PickupableItem : MonoBehaviour
{
	public Rigidbody Rb { get; private set; }
	public Collider Col { get; private set; } 

	void Awake()
	{
		Rb = GetComponent<Rigidbody>(); 
		Col = GetComponent<Collider>(); 
	}

	
}