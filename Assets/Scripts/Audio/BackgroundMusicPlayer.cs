using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For Find and Contains

public class BackgroundMusicPlayer : MonoBehaviour
{
	public static BackgroundMusicPlayer Instance { get; private set; }

	[Header("Music Source")]
	[SerializeField] private AudioSource audioSource;

	[Header("Music Tracks")]
	[SerializeField] private List<AudioClip> initialMusicTracks; // Tracks available from the start
	[SerializeField] private List<AudioClip> allPossibleUnlockableTracks; // ALL tracks that *can* be unlocked

	[Header("Settings")]
	[SerializeField] private bool playOnStart = true;
	[SerializeField] private int startTrackIndexInInitial = 0; // Index within initialMusicTracks

	private List<AudioClip> currentlyPlayableTracks = new List<AudioClip>();
	private int currentPlayableTrackIndex = -1;

	private const string UnlockedMusicTrackKeyPrefix = "UnlockedMusic_"; // PlayerPrefs key

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

		InitializePlayableTracks();
	}

	void Start()
	{
		audioSource.loop = true;
		audioSource.playOnAwake = false;

		if (playOnStart && currentlyPlayableTracks.Count > 0)
		{
			int playIndex = startTrackIndexInInitial;
			if (playIndex < 0 || playIndex >= currentlyPlayableTracks.Count) // Safety check against initial list size
			{
				playIndex = 0;
			}
			PlayTrackByIndex(playIndex);
		}
		else if (currentlyPlayableTracks.Count == 0)
		{
			Debug.LogWarning("BackgroundMusicPlayer: No tracks available to play on start.");
		}
	}

	private void InitializePlayableTracks()
	{
		currentlyPlayableTracks.Clear();
		if (initialMusicTracks != null)
		{
			currentlyPlayableTracks.AddRange(initialMusicTracks);
		}

		// Check PlayerPrefs for additionally unlocked tracks
		if (allPossibleUnlockableTracks != null)
		{
			foreach (AudioClip track in allPossibleUnlockableTracks)
			{
				if (PlayerPrefs.GetInt(UnlockedMusicTrackKeyPrefix + track.name, 0) == 1)
				{
					if (!currentlyPlayableTracks.Contains(track)) // Avoid duplicates if also in initial list
					{
						currentlyPlayableTracks.Add(track);
					}
				}
			}
		}
		Debug.Log($"BackgroundMusicPlayer: Initialized with {currentlyPlayableTracks.Count} playable tracks.");
	}

	public void PlayTrackByIndex(int trackIndex)
	{
		if (currentlyPlayableTracks.Count == 0)
		{
			Debug.LogWarning("No tracks available in currentlyPlayableTracks.");
			return;
		}
		if (trackIndex < 0 || trackIndex >= currentlyPlayableTracks.Count)
		{
			Debug.LogWarning($"Track index {trackIndex} is out of bounds for currentlyPlayableTracks (count: {currentlyPlayableTracks.Count}). Defaulting to 0.");
			trackIndex = 0;
			if (currentlyPlayableTracks.Count == 0) return; // Still no tracks
		}

		currentPlayableTrackIndex = trackIndex;
		audioSource.clip = currentlyPlayableTracks[currentPlayableTrackIndex];
		audioSource.Play();
		Debug.Log($"BackgroundMusicPlayer: Playing '{audioSource.clip.name}' (Index: {currentPlayableTrackIndex})");
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

	// Called by RewardManager
	public void UnlockAndPlayTrackByName(string trackName)
	{
		if (allPossibleUnlockableTracks == null)
		{
			Debug.LogWarning($"BackgroundMusicPlayer: allPossibleUnlockableTracks list is not set. Cannot unlock '{trackName}'.");
			return;
		}

		AudioClip trackToUnlock = allPossibleUnlockableTracks.Find(t => t.name == trackName);

		if (trackToUnlock == null)
		{
			Debug.LogWarning($"BackgroundMusicPlayer: Track named '{trackName}' not found in allPossibleUnlockableTracks.");
			return;
		}

		if (!currentlyPlayableTracks.Contains(trackToUnlock))
		{
			currentlyPlayableTracks.Add(trackToUnlock);
			PlayerPrefs.SetInt(UnlockedMusicTrackKeyPrefix + trackToUnlock.name, 1);
			PlayerPrefs.Save();
			Debug.Log($"BackgroundMusicPlayer: Unlocked music track '{trackToUnlock.name}'. Added to playable list.");
		}
		else
		{
			Debug.Log($"BackgroundMusicPlayer: Music track '{trackToUnlock.name}' was already unlocked/available.");
		}

		// Play the newly unlocked (or already available) track
		int newTrackIndex = currentlyPlayableTracks.IndexOf(trackToUnlock);
		if (newTrackIndex != -1)
		{
			PlayTrackByIndex(newTrackIndex);
		}
	}

	public void StopMusic()
	{
		audioSource.Stop();
	}

	[ContextMenu("Reset Unlocked Music Tracks (PlayerPrefs)")]
	public void ResetUnlockedMusicTracksPlayerPrefs()
	{
		if (allPossibleUnlockableTracks != null)
		{
			foreach (AudioClip track in allPossibleUnlockableTracks)
			{
				PlayerPrefs.DeleteKey(UnlockedMusicTrackKeyPrefix + track.name);
			}
		}
		PlayerPrefs.Save();
		InitializePlayableTracks(); // Re-initialize to reflect changes in the editor state and current session
		Debug.Log("PlayerPrefs for unlocked music tracks have been reset. Restart the game to see the full effect if tracks were already added to the live list.");
	}
}