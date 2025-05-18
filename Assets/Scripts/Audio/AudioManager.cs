using UnityEngine;
using UnityEngine.Audio; // Required for AudioMixer
using UnityEngine.UI;    // Required for Slider (if you link sliders directly)

public class AudioManager : MonoBehaviour
{
	public static AudioManager Instance { get; private set; }

	[Header("Audio Mixer & Groups")]
	[SerializeField] private AudioMixer mainAudioMixer; // Assign your MainAudioMixer asset here

	// These are the exact names you used when exposing parameters
	public const string MASTER_VOL_KEY = "MasterVolume";
	public const string MUSIC_VOL_KEY = "MusicVolume";
	public const string SFX_VOL_KEY = "SFXVolume";

	// PlayerPrefs keys for saving settings
	private const string MASTER_PREF_KEY = "MasterVolumePreference";
	private const string MUSIC_PREF_KEY = "MusicVolumePreference";
	private const string SFX_PREF_KEY = "SFXVolumePreference";

	// Public properties to hold current slider values (0.0 to 1.0)
	// These can be used to initialize UI sliders when a settings panel opens
	public float MasterVolumeSetting { get; private set; }
	public float MusicVolumeSetting { get; private set; }
	public float SFXVolumeSetting { get; private set; }


	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);

		if (mainAudioMixer == null)
		{
			Debug.LogError("AudioManager: MainAudioMixer not assigned in Inspector!");
			enabled = false; // Disable script if mixer is missing
			return;
		}

		LoadVolumeSettings();
	}

	void Start()
	{
		// Apply loaded settings at start
		// This ensures that if Start is called after Awake (e.g. scene reload), volumes are set
		ApplyAllLoadedVolumes();
	}

	private void ApplyAllLoadedVolumes()
	{
		SetMasterVolume(MasterVolumeSetting);
		SetMusicVolume(MusicVolumeSetting);
		SetSFXVolume(SFXVolumeSetting);
	}

	public void SetMasterVolume(float sliderValue) // sliderValue is 0.0 to 1.0
	{
		MasterVolumeSetting = sliderValue;
		// Mixer uses decibels: 0dB is full, -80dB is silent.
		// Mathf.Log10(0) is negative infinity, so clamp sliderValue to avoid issues.
		mainAudioMixer.SetFloat(MASTER_VOL_KEY, Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f);
		PlayerPrefs.SetFloat(MASTER_PREF_KEY, sliderValue);
		PlayerPrefs.Save();
	}

	public void SetMusicVolume(float sliderValue)
	{
		MusicVolumeSetting = sliderValue;
		mainAudioMixer.SetFloat(MUSIC_VOL_KEY, Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f);
		PlayerPrefs.SetFloat(MUSIC_PREF_KEY, sliderValue);
		PlayerPrefs.Save();
	}

	public void SetSFXVolume(float sliderValue)
	{
		SFXVolumeSetting = sliderValue;
		mainAudioMixer.SetFloat(SFX_VOL_KEY, Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f);
		PlayerPrefs.SetFloat(SFX_PREF_KEY, sliderValue);
		PlayerPrefs.Save();
	}

	private void LoadVolumeSettings()
	{
		MasterVolumeSetting = PlayerPrefs.GetFloat(MASTER_PREF_KEY, 0.75f); // Default to 75%
		MusicVolumeSetting = PlayerPrefs.GetFloat(MUSIC_PREF_KEY, 0.75f);
		SFXVolumeSetting = PlayerPrefs.GetFloat(SFX_PREF_KEY, 0.75f);

		Debug.Log($"Loaded Volumes: Master={MasterVolumeSetting}, Music={MusicVolumeSetting}, SFX={SFXVolumeSetting}");
		// Applying them here ensures they are set on first Awake.
		// Start() will also apply them, which is fine as a fallback.
		ApplyAllLoadedVolumes();
	}

	// Call this if you add a "Reset to Defaults" button
	public void ResetVolumeToDefaults()
	{
		SetMasterVolume(0.75f);
		SetMusicVolume(0.75f);
		SetSFXVolume(0.75f);
		// You'll need to update any UI sliders connected to these too
	}
}