using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BackgroundMusicPlayer : MonoBehaviour
{
	public static BackgroundMusicPlayer Instance { get; private set; }

	[Header("Music Source")]
	[SerializeField] private AudioSource audioSource;

	[Header("Music Tracks")]
	public List<AudioClip> initialMusicTracks;
	[SerializeField] private List<AudioClip> allPossibleUnlockableTracks;

	[Header("Settings")]
	[SerializeField] private bool playOnStart = true;
	[SerializeField] private int startTrackIndexInInitial = 0;

	private List<AudioClip> currentlyPlayableTracks = new List<AudioClip>();
	private int currentPlayableTrackIndex = -1;

	// Called when the script instance is being loaded.
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
			Debug.LogError("BackgroundMusicPlayer: AudioSource component not found! Music will not play.");
			enabled = false;
			return;
		}
		InitializeWithInitialTracksOnly();
	}

	// Called before the first frame update, after all Awake functions.
	void Start()
	{
		audioSource.loop = true;
		audioSource.playOnAwake = false;

		if (playOnStart && currentlyPlayableTracks.Count > 0)
		{
			int playIndex = startTrackIndexInInitial;
			if (playIndex < 0 || playIndex >= currentlyPlayableTracks.Count)
			{
				playIndex = 0;
			}

			if (currentlyPlayableTracks.Count > 0)
			{
				PlayTrackByIndex(playIndex);
			}
		}
		else if (currentlyPlayableTracks.Count == 0)
		{
			Debug.LogWarning("BackgroundMusicPlayer: No tracks available to play on start (initial list empty or not yet fully loaded by GameDataManager).");
		}
	}

	// Sets up the initial list of playable tracks from the 'initialMusicTracks' list.
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

	// Updates the list of playable tracks based on unlocked tracks from save data.
	public void ApplyUnlockedMusicFromLoad(List<string> loadedUnlockedTrackNames)
	{
		InitializeWithInitialTracksOnly();

		if (loadedUnlockedTrackNames != null && allPossibleUnlockableTracks != null)
		{
			foreach (string trackName in loadedUnlockedTrackNames)
			{
				if (string.IsNullOrEmpty(trackName)) continue;

				AudioClip track = allPossibleUnlockableTracks.Find(t => t != null && t.name == trackName);
				if (track != null)
				{
					if (!currentlyPlayableTracks.Contains(track))
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

		if (playOnStart && !audioSource.isPlaying && currentlyPlayableTracks.Count > 0)
		{
			PlayTrackByIndex(0);
		}
	}

	// Plays a music track from the 'currentlyPlayableTracks' list using its index.
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
			if (currentlyPlayableTracks.Count == 0) return;
		}

		currentPlayableTrackIndex = trackIndex;
		if (currentlyPlayableTracks[currentPlayableTrackIndex] != null)
		{
			if (audioSource.clip == currentlyPlayableTracks[currentPlayableTrackIndex] && audioSource.isPlaying)
			{
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

	// Changes to the next available music track.
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

	// Unlocks a new music track by its name and plays it.
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
			Debug.Log($"BackgroundMusicPlayer: Added '{trackToUnlock.name}' to playable list for current session.");
		}
		else
		{
			Debug.Log($"BackgroundMusicPlayer: Music track '{trackToUnlock.name}' was already in playable list.");
		}

		int newTrackIndex = currentlyPlayableTracks.IndexOf(trackToUnlock);
		if (newTrackIndex != -1)
		{
			if (newAdditionToPlayableList || audioSource.clip != trackToUnlock || !audioSource.isPlaying)
			{
				PlayTrackByIndex(newTrackIndex);
			}
		}
	}

	// Stops the currently playing music.
	public void StopMusic()
	{
		if (audioSource != null)
		{
			audioSource.Stop();
		}
	}
}