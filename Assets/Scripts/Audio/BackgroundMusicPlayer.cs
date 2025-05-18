using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For Find and Contains

public class BackgroundMusicPlayer : MonoBehaviour
{
	public static BackgroundMusicPlayer Instance { get; private set; }

	[Header("Music Source")]
	[SerializeField] private AudioSource audioSource;

	[Header("Music Tracks")]
	[SerializeField] private List<AudioClip> initialMusicTracks; // Tracks available from the start of a brand new game
	[SerializeField] private List<AudioClip> allPossibleUnlockableTracks; // ALL tracks that *can* be unlocked via rewards (must contain all tracks referenced by name in RewardManager)

	[Header("Settings")]
	[SerializeField] private bool playOnStart = true;
	[SerializeField] private int startTrackIndexInInitial = 0; // Index within initialMusicTracks for a new game

	private List<AudioClip> currentlyPlayableTracks = new List<AudioClip>();
	private int currentPlayableTrackIndex = -1;

	// PlayerPrefs for individual track unlock status is no longer used here.
	// GameDataManager provides the list of unlocked track names.

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);

		if (audioSource == null) audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			Debug.LogError("BackgroundMusicPlayer requires an AudioSource component!");
			enabled = false;
			return;
		}

		// Initialize with only initial tracks.
		// GameDataManager will call ApplyUnlockedMusicFromLoad to add saved/unlocked tracks.
		InitializeWithInitialTracksOnly();
	}

	void Start()
	{
		audioSource.loop = true;
		audioSource.playOnAwake = false;

		// The decision to play on start might be better handled after GameDataManager has loaded data.
		// For now, if playOnStart is true, it will try to play from the initial set,
		// and ApplyUnlockedMusicFromLoad might change the track later if it's different.
		if (playOnStart && currentlyPlayableTracks.Count > 0)
		{
			int playIndex = startTrackIndexInInitial;
			if (playIndex < 0 || playIndex >= currentlyPlayableTracks.Count)
			{
				playIndex = 0;
			}
			PlayTrackByIndex(playIndex);
		}
		else if (currentlyPlayableTracks.Count == 0)
		{
			Debug.LogWarning("BackgroundMusicPlayer: No tracks available to play on start (initial list empty or not yet loaded).");
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
		Debug.Log($"BackgroundMusicPlayer: Initialized with {currentlyPlayableTracks.Count} initial tracks.");
	}

	// Called by GameDataManager when loading a save or starting a new game (which includes initial unlocks)
	public void ApplyUnlockedMusicFromLoad(List<string> loadedUnlockedTrackNames)
	{
		InitializeWithInitialTracksOnly(); // Always start with the base initial tracks

		if (loadedUnlockedTrackNames != null && allPossibleUnlockableTracks != null)
		{
			foreach (string trackName in loadedUnlockedTrackNames)
			{
				if (string.IsNullOrEmpty(trackName)) continue;

				AudioClip track = allPossibleUnlockableTracks.Find(t => t != null && t.name == trackName);
				if (track != null)
				{
					if (!currentlyPlayableTracks.Contains(track)) // Add if not already in the list (e.g. from initial)
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

		// If no music is playing (e.g. after loading a save that had no music playing, or new game),
		// and playOnStart is true, try to start playing a track.
		if (playOnStart && !audioSource.isPlaying && currentlyPlayableTracks.Count > 0)
		{
			// Play the first track from the now-populated currentlyPlayableTracks list
			// or a specific loaded track if you save current track index
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
			trackIndex = 0; // Default to first track if index is bad
			if (currentlyPlayableTracks.Count == 0) return; // Still no tracks after defaulting
		}

		currentPlayableTrackIndex = trackIndex;
		if (currentlyPlayableTracks[currentPlayableTrackIndex] != null)
		{
			audioSource.clip = currentlyPlayableTracks[currentPlayableTrackIndex];
			audioSource.Play();
			Debug.Log($"BackgroundMusicPlayer: Playing '{audioSource.clip.name}' (Index: {currentPlayableTrackIndex} in current list)");
		}
		else
		{
			Debug.LogError($"BackgroundMusicPlayer: Track at index {currentPlayableTrackIndex} in currentlyPlayableTracks is null!");
		}
	}

	// Called by player interaction (e.g., radio)
	public void ChangeTrack()
	{
		if (currentlyPlayableTracks.Count == 0) return;

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
			Debug.LogWarning($"BackgroundMusicPlayer: allPossibleUnlockableTracks list is not set. Cannot unlock '{trackName}'.");
			return;
		}

		AudioClip trackToUnlock = allPossibleUnlockableTracks.Find(t => t != null && t.name == trackName);

		if (trackToUnlock == null)
		{
			Debug.LogWarning($"BackgroundMusicPlayer: Track named '{trackName}' not found in allPossibleUnlockableTracks pool.");
			return;
		}

		bool newAddition = false;
		if (!currentlyPlayableTracks.Contains(trackToUnlock))
		{
			currentlyPlayableTracks.Add(trackToUnlock);
			newAddition = true;
			// GameDataManager is responsible for adding this to its list for saving (already done by RewardManager)
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
			// Only switch to it if it's a new addition or if you always want to switch
			if (newAddition || audioSource.clip != trackToUnlock)
			{
				PlayTrackByIndex(newTrackIndex);
			}
		}
	}

	public void StopMusic()
	{
		audioSource.Stop();
	}

	// PlayerPrefs reset for music is no longer relevant here as GameDataManager handles save data.
	// A debug function to clear GameDataManager's music list would be in GameDataManager.
}