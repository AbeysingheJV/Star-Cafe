using UnityEngine;

public class ReadableBook : MonoBehaviour
{
	// This method will be called by PickupController when 'E' is pressed on this book
	public void InteractWithBook()
	{
		if (RecipeBookUI.Instance != null)
		{
			Debug.Log("Interacting with book, telling RecipeBookUI to toggle.");
			RecipeBookUI.Instance.ToggleRecipeBook(); // Or just OpenBook() if you only want E to open it
		}
		else
		{
			Debug.LogError("ReadableBook: RecipeBookUI.Instance not found! Make sure RecipeBookUI_Handler is in the scene.");
		}
	}
}