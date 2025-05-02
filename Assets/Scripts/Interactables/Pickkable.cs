using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Pickupable : MonoBehaviour
{
	[Header("Ingredient Identification (Optional)")] 
	[Tooltip("Assign the IngredientData Scriptable Object for this item IF it's an ingredient.")]
	public IngredientData ingredientData; 

	public Rigidbody Rb { get; private set; }

	void Awake()
	{
		Rb = GetComponent<Rigidbody>();

	}
}