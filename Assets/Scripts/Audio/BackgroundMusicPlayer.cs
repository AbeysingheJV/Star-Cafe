using UnityEngine;
using System.Collections.Generic;

public class BackgroundMusicPlayer : MonoBehaviour
{
	public static BackgroundMusicPlayer Instance { get; private set; }

	[SerializeField] private AudioSource audioSource;
	[SerializeField] private List<AudioClip> musicTracks;
	[SerializeField] private bool playOnStart = true;
	[SerializeField] private int startTrackIndex = 0;

	private int currentTrackIndex = -1;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject); // Destroy duplicate
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject); // Optional: if you want music to persist between scene loads

		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (audioSource == null)
		{
			Debug.LogError("BackgroundMusicPlayer requires an AudioSource component!");
			enabled = false;
			return;
		}
	}

	void Start()
	{
		if (musicTracks == null || musicTracks.Count == 0)
		{
			Debug.LogWarning("BackgroundMusicPlayer: No music tracks assigned.");
			return;
		}

		// Ensure audioSource settings for background music
		audioSource.loop = true; // Music tracks should generally loop
		audioSource.playOnAwake = false; // We will control playback

		if (playOnStart)
		{
			if (startTrackIndex < 0 || startTrackIndex >= musicTracks.Count)
			{
				startTrackIndex = 0; // Default to first track if index is invalid
			}
			PlayTrack(startTrackIndex);
		}
	}

	public void PlayTrack(int trackIndex)
	{
		if (musicTracks == null || musicTracks.Count == 0) return;
		if (trackIndex < 0 || trackIndex >= musicTracks.Count)
		{
			Debug.LogWarning($"Track index {trackIndex} is out of bounds.");
			return;
		}

		currentTrackIndex = trackIndex;
		audioSource.clip = musicTracks[currentTrackIndex];
		audioSource.Play();
		Debug.Log($"Playing music track: {audioSource.clip.name}");
	}

	public void ChangeTrack()
	{
		if (musicTracks == null || musicTracks.Count == 0) return;

		currentTrackIndex++;
		if (currentTrackIndex >= musicTracks.Count)
		{
			currentTrackIndex = 0; // Loop back to the first track
		}
		PlayTrack(currentTrackIndex);
	}

	public void StopMusic()
	{
		audioSource.Stop();
	}
}