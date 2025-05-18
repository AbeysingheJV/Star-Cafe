using UnityEngine;
using System.IO; // For file operations
using System.Collections.Generic; // For List
using System; // For DateTime

public static class SaveSystem
{
	private static readonly string SAVE_FOLDER = Application.persistentDataPath + "/Saves/";
	private const string FILE_EXTENSION = ".json";
	public const int MAX_SAVE_SLOTS = 3; // Or however many slots you want

	// Call this once at game startup to ensure the folder exists
	public static void Initialize()
	{
		if (!Directory.Exists(SAVE_FOLDER))
		{
			Directory.CreateDirectory(SAVE_FOLDER);
			Debug.Log("Save folder created at: " + SAVE_FOLDER);
		}
	}

	private static string GetFilePath(int slotNumber)
	{
		return SAVE_FOLDER + "saveSlot_" + slotNumber + FILE_EXTENSION;
	}

	public static void SaveGame(SaveData data, int slotNumber)
	{
		if (slotNumber < 0 || slotNumber >= MAX_SAVE_SLOTS)
		{
			Debug.LogError($"SaveSystem: Invalid slot number {slotNumber}. Must be between 0 and {MAX_SAVE_SLOTS - 1}.");
			return;
		}

		data.lastUpdatedTicks = DateTime.Now.Ticks; // Update timestamp before saving
		string json = JsonUtility.ToJson(data, true); // 'true' for pretty print (easier to debug)
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
			return null; // Or return new SaveData() if you want to start fresh if no file
		}
	}

	public static bool DoesSaveExist(int slotNumber)
	{
		if (slotNumber < 0 || slotNumber >= MAX_SAVE_SLOTS) return false;
		return File.Exists(GetFilePath(slotNumber));
	}

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

	// Gets metadata for all slots (e.g., for a load game screen)
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
					// We only need the timestamp, so we could parse just that part of JSON,
					// but loading the whole SaveData is simpler for now.
					SaveData data = LoadGame(i); // Use existing LoadGame to get data
					if (data != null)
					{
						infos.Add(new SaveSlotInfo(i, true, data.GetLastUpdatedDateTime()));
					}
					else // File exists but couldn't be loaded (corrupted?)
					{
						infos.Add(new SaveSlotInfo(i, false, DateTime.MinValue)); // Mark as existing but unloadable
					}
				}
				catch (Exception e)
				{
					Debug.LogError($"GetSaveSlotsInfo: Error reading slot {i}. {e.Message}");
					infos.Add(new SaveSlotInfo(i, false, DateTime.MinValue)); // Error, treat as empty/corrupt
				}
			}
			else
			{
				infos.Add(new SaveSlotInfo(i, false, DateTime.MinValue)); // Empty slot
			}
		}
		return infos;
	}
}

// Helper class to store info about each save slot for UI display
public class SaveSlotInfo
{
	public int SlotNumber { get; private set; }
	public bool IsUsed { get; private set; }
	public DateTime LastSaved { get; private set; }

	public SaveSlotInfo(int slotNumber, bool isUsed, DateTime lastSaved)
	{
		SlotNumber = slotNumber;
		IsUsed = isUsed;
		LastSaved = lastSaved;
	}
}
