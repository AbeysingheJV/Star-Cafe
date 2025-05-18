using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for Linq operations like Find and Contains

public class BackgroundMusicPlayer : MonoBehaviour
{
	public static BackgroundMusicPlayer Instance { get; private set; }

	[Header("Music Source")]
	[SerializeField] private AudioSource audioSource;

	[Header("Music Tracks")]
	// This list is for tracks available from the very start of a brand new game,
	// before any save data is processed or any rewards are unlocked.
	// GameDataManager will also add these to the SaveData for a new game.
	public List<AudioClip> initialMusicTracks;

	// This list must contain ALL AudioClip assets that can EVER be unlocked as rewards.
	// GameDataManager loads track *names* from save data, and this list is used to find the actual AudioClips.
	[SerializeField] private List<AudioClip> allPossibleUnlockableTracks;

	[Header("Settings")]
	[SerializeField] private bool playOnStart = true;
	[SerializeField] private int startTrackIndexInInitial = 0; // Index within initialMusicTracks for a new game

	private List<AudioClip> currentlyPlayableTracks = new List<AudioClip>();
	private int currentPlayableTrackIndex = -1;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject); // This manager should persist across scenes

		if (audioSource == null) audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			Debug.LogError("BackgroundMusicPlayer: AudioSource component not found! Music will not play.");
			enabled = false; // Disable script if no AudioSource
			return;
		}

		// Initialize with only the initial tracks.
		// GameDataManager will call ApplyUnlockedMusicFromLoad to add more from save data.
		InitializeWithInitialTracksOnly();
	}

	void Start()
	{
		audioSource.loop = true;
		audioSource.playOnAwake = false;

		// Attempt to play a track if playOnStart is true and tracks are available.
		// This might play an initial track, which could then be changed by ApplyUnlockedMusicFromLoad
		// if a different track was saved as "last played" (though we don't save last played track index currently).
		if (playOnStart && currentlyPlayableTracks.Count > 0)
		{
			int playIndex = startTrackIndexInInitial;
			// Ensure playIndex is valid for the current state of currentlyPlayableTracks
			if (playIndex < 0 || playIndex >= currentlyPlayableTracks.Count)
			{
				playIndex = 0; // Default to first track if index is bad
			}

			if (currentlyPlayableTracks.Count > 0) // Check again after potential adjustment
			{
				PlayTrackByIndex(playIndex);
			}
		}
		else if (currentlyPlayableTracks.Count == 0)
		{
			Debug.LogWarning("BackgroundMusicPlayer: No tracks available to play on start (initial list empty or not yet fully loaded by GameDataManager).");
		}
	}

	private void InitializeWithInitialTracksOnly()
	{
		currentlyPlayableTracks.Clear();
		if (initialMusicTracks != null)
		{
			foreach (AudioClip track in initialMusicTracks)
			{
				if (track != null && !currentlyPlayableTracks.Contains(track))
				{
					currentlyPlayableTracks.Add(track);
				}
			}
		}
		Debug.Log($"BackgroundMusicPlayer: Initialized with {currentlyPlayableTracks.Count} initial tracks in playable list.");
	}

	// Called by GameDataManager when loading a save or starting a new game
	public void ApplyUnlockedMusicFromLoad(List<string> loadedUnlockedTrackNames)
	{
		// Start fresh with initial tracks, then add loaded/unlocked ones
		InitializeWithInitialTracksOnly();

		if (loadedUnlockedTrackNames != null && allPossibleUnlockableTracks != null)
		{
			foreach (string trackName in loadedUnlockedTrackNames)
			{
				if (string.IsNullOrEmpty(trackName)) continue;

				// Find the actual AudioClip asset from the master list of all unlockable tracks
				AudioClip track = allPossibleUnlockableTracks.Find(t => t != null && t.name == trackName);
				if (track != null)
				{
					if (!currentlyPlayableTracks.Contains(track)) // Add if not already in the list (e.g., from initialMusicTracks)
					{
						currentlyPlayableTracks.Add(track);
					}
				}
				else
				{
					Debug.LogWarning($"BackgroundMusicPlayer: Could not find AudioClip asset named '{trackName}' in allPossibleUnlockableTracks pool during load.");
				}
			}
		}
		Debug.Log($"BackgroundMusicPlayer: Applied save data. Total playable tracks now: {currentlyPlayableTracks.Count}");

		// If playOnStart is true and no music is currently playing (e.g., after loading a save),
		// try to start playing a track from the now fully populated list.
		if (playOnStart && !audioSource.isPlaying && currentlyPlayableTracks.Count > 0)
		{
			// Play the first available track.
			// A more advanced system might save and load the last played track index.
			PlayTrackByIndex(0);
		}
	}

	public void PlayTrackByIndex(int trackIndex)
	{
		if (currentlyPlayableTracks.Count == 0)
		{
			Debug.LogWarning("BackgroundMusicPlayer: No tracks available in currentlyPlayableTracks to play.");
			return;
		}
		if (trackIndex < 0 || trackIndex >= currentlyPlayableTracks.Count)
		{
			Debug.LogWarning($"Track index {trackIndex} is out of bounds for currentlyPlayableTracks (count: {currentlyPlayableTracks.Count}). Defaulting to 0.");
			trackIndex = 0;
			if (currentlyPlayableTracks.Count == 0) return; // Still no tracks after defaulting
		}

		currentPlayableTrackIndex = trackIndex;
		if (currentlyPlayableTracks[currentPlayableTrackIndex] != null)
		{
			if (audioSource.clip == currentlyPlayableTracks[currentPlayableTrackIndex] && audioSource.isPlaying)
			{
				// Already playing this track, do nothing or restart if desired
				// Debug.Log($"BackgroundMusicPlayer: Already playing '{audioSource.clip.name}'.");
				return;
			}
			audioSource.clip = currentlyPlayableTracks[currentPlayableTrackIndex];
			audioSource.Play();
			Debug.Log($"BackgroundMusicPlayer: Playing '{audioSource.clip.name}' (Index: {currentPlayableTrackIndex} in current playable list of {currentlyPlayableTracks.Count} tracks)");
		}
		else
		{
			Debug.LogError($"BackgroundMusicPlayer: Track at index {currentPlayableTrackIndex} in currentlyPlayableTracks is null!");
		}
	}

	// Called by player interaction (e.g., radio)
	public void ChangeTrack()
	{
		if (currentlyPlayableTracks.Count == 0)
		{
			Debug.LogWarning("BackgroundMusicPlayer: No tracks to change to.");
			return;
		}

		currentPlayableTrackIndex++;
		if (currentPlayableTrackIndex >= currentlyPlayableTracks.Count)
		{
			currentPlayableTrackIndex = 0;
		}
		PlayTrackByIndex(currentPlayableTrackIndex);
	}

	// Called by RewardManager when a new track is unlocked IN THE CURRENT SESSION
	public void UnlockAndPlayTrackByName(string trackName)
	{
		if (string.IsNullOrEmpty(trackName))
		{
			Debug.LogWarning("BackgroundMusicPlayer: UnlockAndPlayTrackByName called with null or empty track name.");
			return;
		}
		if (allPossibleUnlockableTracks == null)
		{
			Debug.LogWarning($"BackgroundMusicPlayer: allPossibleUnlockableTracks list is not assigned. Cannot process unlock for '{trackName}'.");
			return;
		}

		AudioClip trackToUnlock = allPossibleUnlockableTracks.Find(t => t != null && t.name == trackName);

		if (trackToUnlock == null)
		{
			Debug.LogWarning($"BackgroundMusicPlayer: Track named '{trackName}' not found in allPossibleUnlockableTracks pool. Cannot unlock.");
			return;
		}

		bool newAdditionToPlayableList = false;
		if (!currentlyPlayableTracks.Contains(trackToUnlock))
		{
			currentlyPlayableTracks.Add(trackToUnlock);
			newAdditionToPlayableList = true;
			// GameDataManager is responsible for adding this to its list for saving (this is already done by RewardManager calling GameDataManager)
			Debug.Log($"BackgroundMusicPlayer: Added '{trackToUnlock.name}' to playable list for current session.");
		}
		else
		{
			Debug.Log($"BackgroundMusicPlayer: Music track '{trackToUnlock.name}' was already in playable list.");
		}

		// Play the newly unlocked (or already available) track
		int newTrackIndex = currentlyPlayableTracks.IndexOf(trackToUnlock);
		if (newTrackIndex != -1)
		{
			// Switch to it if it's a new addition or if it's not already the current track
			if (newAdditionToPlayableList || audioSource.clip != trackToUnlock || !audioSource.isPlaying)
			{
				PlayTrackByIndex(newTrackIndex);
			}
		}
	}

	public void StopMusic()
	{
		if (audioSource != null)
		{
			audioSource.Stop();
		}
	}

	// PlayerPrefs reset for music is no longer relevant here as GameDataManager handles save data.
	// A debug function to clear GameDataManager's music list would be in GameDataManager.
}
