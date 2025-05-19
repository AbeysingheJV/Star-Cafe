using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

public static class SaveSystem
{
	private static readonly string SAVE_FOLDER = Application.persistentDataPath + "/Saves/"; // Folder where save files are stored.
	private const string FILE_EXTENSION = ".json"; // File extension for save files.
	public const int MAX_SAVE_SLOTS = 3; // Maximum number of save slots allowed.

	// Initializes the save system, ensuring the save folder exists.
	public static void Initialize()
	{
		if (!Directory.Exists(SAVE_FOLDER))
		{
			Directory.CreateDirectory(SAVE_FOLDER);
			Debug.Log("Save folder created at: " + SAVE_FOLDER);
		}
	}

	// Gets the full file path for a given save slot number.
	private static string GetFilePath(int slotNumber)
	{
		return SAVE_FOLDER + "saveSlot_" + slotNumber + FILE_EXTENSION;
	}

	// Saves the provided game data to the specified slot number.
	public static void SaveGame(SaveData data, int slotNumber)
	{
		if (slotNumber < 0 || slotNumber >= MAX_SAVE_SLOTS)
		{
			Debug.LogError($"SaveSystem: Invalid slot number {slotNumber}. Must be between 0 and {MAX_SAVE_SLOTS - 1}.");
			return;
		}

		data.lastUpdatedTicks = DateTime.Now.Ticks;
		string json = JsonUtility.ToJson(data, true);
		string filePath = GetFilePath(slotNumber);

		try
		{
			File.WriteAllText(filePath, json);
			Debug.Log($"Game saved to slot {slotNumber} at {filePath}");
		}
		catch (Exception e)
		{
			Debug.LogError($"SaveSystem: Failed to save game to slot {slotNumber}. Error: {e.Message}");
		}
	}

	// Loads game data from the specified slot number.
	public static SaveData LoadGame(int slotNumber)
	{
		if (slotNumber < 0 || slotNumber >= MAX_SAVE_SLOTS)
		{
			Debug.LogError($"SaveSystem: Invalid slot number {slotNumber}.");
			return null;
		}

		string filePath = GetFilePath(slotNumber);
		if (File.Exists(filePath))
		{
			try
			{
				string json = File.ReadAllText(filePath);
				SaveData data = JsonUtility.FromJson<SaveData>(json);
				Debug.Log($"Game loaded from slot {slotNumber}. Last saved: {data.GetLastUpdatedDateTime()}");
				return data;
			}
			catch (Exception e)
			{
				Debug.LogError($"SaveSystem: Failed to load game from slot {slotNumber}. Error: {e.Message}. File might be corrupted.");
				return null;
			}
		}
		else
		{
			Debug.Log($"SaveSystem: No save file found for slot {slotNumber}.");
			return null;
		}
	}

	// Checks if a save file exists for the given slot number.
	public static bool DoesSaveExist(int slotNumber)
	{
		if (slotNumber < 0 || slotNumber >= MAX_SAVE_SLOTS) return false;
		return File.Exists(GetFilePath(slotNumber));
	}

	// Deletes the save file for the given slot number.
	public static void DeleteSave(int slotNumber)
	{
		if (slotNumber < 0 || slotNumber >= MAX_SAVE_SLOTS) return;
		string filePath = GetFilePath(slotNumber);
		if (File.Exists(filePath))
		{
			try
			{
				File.Delete(filePath);
				Debug.Log($"SaveSystem: Deleted save file for slot {slotNumber}.");
			}
			catch (Exception e)
			{
				Debug.LogError($"SaveSystem: Failed to delete save file for slot {slotNumber}. Error: {e.Message}");
			}
		}
	}

	// Retrieves information about all save slots (used/empty, last saved time).
	public static List<SaveSlotInfo> GetSaveSlotsInfo()
	{
		List<SaveSlotInfo> infos = new List<SaveSlotInfo>();
		for (int i = 0; i < MAX_SAVE_SLOTS; i++)
		{
			string filePath = GetFilePath(i);
			if (File.Exists(filePath))
			{
				try
				{
					SaveData data = LoadGame(i);
					if (data != null)
					{
						infos.Add(new SaveSlotInfo(i, true, data.GetLastUpdatedDateTime()));
					}
					else
					{
						infos.Add(new SaveSlotInfo(i, false, DateTime.MinValue));
					}
				}
				catch (Exception e)
				{
					Debug.LogError($"GetSaveSlotsInfo: Error reading slot {i}. {e.Message}");
					infos.Add(new SaveSlotInfo(i, false, DateTime.MinValue));
				}
			}
			else
			{
				infos.Add(new SaveSlotInfo(i, false, DateTime.MinValue));
			}
		}
		return infos;
	}
}

// Helper class to store display information for a save slot.
public class SaveSlotInfo
{
	public int SlotNumber { get; private set; } // The slot number.
	public bool IsUsed { get; private set; } // True if the slot contains save data.
	public DateTime LastSaved { get; private set; } // The date and time the slot was last saved.

	// Constructor for creating SaveSlotInfo.
	public SaveSlotInfo(int slotNumber, bool isUsed, DateTime lastSaved)
	{
		SlotNumber = slotNumber;
		IsUsed = isUsed;
		LastSaved = lastSaved;
	}
}