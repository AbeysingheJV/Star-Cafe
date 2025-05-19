using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RecipeBookUI : MonoBehaviour
{
	public static RecipeBookUI Instance { get; private set; }

	[Header("UI Elements (Assign in Inspector)")]
	[SerializeField] private GameObject recipeBookPanel; // The main UI panel for the recipe book.
	[SerializeField] private Image recipeImageDisplay; // UI Image element to display recipe pages.

	[Header("Recipe Images (Assign in Inspector)")]
	[SerializeField] private List<Sprite> recipePages; // List of Sprites, each being a recipe page.

	private int currentPageIndex = 0; // Index of the currently displayed recipe page.
	private bool isBookOpen = false; // Is the recipe book currently open?

	// Called when the script instance is being loaded.
	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;

		if (recipeBookPanel == null) Debug.LogError("RecipeBookUI: RecipeBookPanel not assigned!");
		if (recipeImageDisplay == null) Debug.LogError("RecipeBookUI: RecipeImageDisplay not assigned!");
		if (recipePages == null || recipePages.Count == 0) Debug.LogWarning("RecipeBookUI: No recipe pages assigned!");

		if (recipeBookPanel != null) recipeBookPanel.SetActive(false);
	}

	// Toggles the recipe book open or closed.
	public void ToggleRecipeBook()
	{
		if (isBookOpen)
		{
			CloseBook();
		}
		else
		{
			OpenBook();
		}
	}

	// Opens the recipe book UI.
	public void OpenBook()
	{
		if (recipeBookPanel == null) return;

		isBookOpen = true;
		recipeBookPanel.SetActive(true);
		currentPageIndex = 0;
		DisplayCurrentPage();

		Time.timeScale = 0f;
		if (PauseMenuManager.Instance != null) PauseMenuManager.isGamePaused = true;

		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		Debug.Log("Recipe Book Opened. Game Paused.");
	}

	// Closes the recipe book UI.
	public void CloseBook()
	{
		if (recipeBookPanel == null) return;

		isBookOpen = false;
		recipeBookPanel.SetActive(false);

		Time.timeScale = 1f;
		if (PauseMenuManager.Instance != null) PauseMenuManager.isGamePaused = false;

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		Debug.Log("Recipe Book Closed. Game Resumed.");
	}

	// Displays the next page in the recipe book.
	public void ShowNextPage()
	{
		if (recipePages == null || recipePages.Count == 0) return;

		currentPageIndex++;
		if (currentPageIndex >= recipePages.Count)
		{
			currentPageIndex = 0;
		}
		DisplayCurrentPage();
	}

	// Updates the recipe image display with the current page.
	private void DisplayCurrentPage()
	{
		if (recipeImageDisplay != null && recipePages != null && recipePages.Count > 0 &&
			currentPageIndex >= 0 && currentPageIndex < recipePages.Count)
		{
			if (recipePages[currentPageIndex] != null)
			{
				recipeImageDisplay.sprite = recipePages[currentPageIndex];
				recipeImageDisplay.gameObject.SetActive(true);
			}
			else
			{
				recipeImageDisplay.gameObject.SetActive(false);
				Debug.LogWarning($"RecipeBookUI: Sprite at index {currentPageIndex} is null.");
			}
		}
		else if (recipeImageDisplay != null)
		{
			recipeImageDisplay.gameObject.SetActive(false);
		}
	}
}