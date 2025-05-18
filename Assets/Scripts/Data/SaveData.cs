using System;
using System.Collections.Generic;

[System.Serializable] // This makes the class serializable by JsonUtility
public class SaveData
{
	public long lastUpdatedTicks; // For storing DateTime as Ticks
	public int totalDishesCompleted;
	public List<string> unlockedRecipeNames;
	public List<string> unlockedMusicTrackNames;
	// Add other data you want to save here, e.g.:
	// public string currentActiveOrderName;
	// public PlayerStats playerStats; // If you have a separate class for player stats

	public SaveData()
	{
		lastUpdatedTicks = DateTime.Now.Ticks;
		totalDishesCompleted = 0;
		unlockedRecipeNames = new List<string>();
		unlockedMusicTrackNames = new List<string>();
	}

	// Helper to get DateTime from Ticks
	public DateTime GetLastUpdatedDateTime()
	{
		return new DateTime(lastUpdatedTicks);
	}
}