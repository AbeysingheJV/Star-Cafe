using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Image
using System.Collections.Generic; // Required for List

public class RecipeBookUI : MonoBehaviour
{
	public static RecipeBookUI Instance { get; private set; }

	[Header("UI Elements (Assign in Inspector)")]
	[SerializeField] private GameObject recipeBookPanel;
	[SerializeField] private Image recipeImageDisplay;
	// Next and Close buttons will be linked via their OnClick() events

	[Header("Recipe Images (Assign in Inspector)")]
	[SerializeField] private List<Sprite> recipePages; // Assign your recipe image Sprites here

	private int currentPageIndex = 0;
	private bool isBookOpen = false;

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

		if (recipeBookPanel != null) recipeBookPanel.SetActive(false); // Ensure it's hidden at start
	}

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

	public void OpenBook()
	{
		if (recipeBookPanel == null) return;

		isBookOpen = true;
		recipeBookPanel.SetActive(true);
		currentPageIndex = 0; // Always start from the first page
		DisplayCurrentPage();

		Time.timeScale = 0f; // Pause the game
		if (PauseMenuManager.Instance != null) PauseMenuManager.isGamePaused = true; // Inform PauseMenuManager

		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		Debug.Log("Recipe Book Opened. Game Paused.");
	}

	public void CloseBook()
	{
		if (recipeBookPanel == null) return;

		isBookOpen = false;
		recipeBookPanel.SetActive(false);

		Time.timeScale = 1f; // Resume the game
		if (PauseMenuManager.Instance != null) PauseMenuManager.isGamePaused = false; // Inform PauseMenuManager

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		Debug.Log("Recipe Book Closed. Game Resumed.");
	}

	public void ShowNextPage()
	{
		if (recipePages == null || recipePages.Count == 0) return;

		currentPageIndex++;
		if (currentPageIndex >= recipePages.Count)
		{
			currentPageIndex = 0; // Loop back to the first page
		}
		DisplayCurrentPage();
	}

	// ShowPreviousPage() could be added similarly if you add a "Previous" button

	private void DisplayCurrentPage()
	{
		if (recipeImageDisplay != null && recipePages != null && recipePages.Count > 0 &&
			currentPageIndex >= 0 && currentPageIndex < recipePages.Count)
		{
			if (recipePages[currentPageIndex] != null)
			{
				recipeImageDisplay.sprite = recipePages[currentPageIndex];
				recipeImageDisplay.gameObject.SetActive(true); // Ensure image component is active
			}
			else
			{
				recipeImageDisplay.gameObject.SetActive(false); // Hide if sprite is null
				Debug.LogWarning($"RecipeBookUI: Sprite at index {currentPageIndex} is null.");
			}
		}
		else if (recipeImageDisplay != null)
		{
			recipeImageDisplay.gameObject.SetActive(false); // Hide if no pages or index out of bounds
		}
	}
}
