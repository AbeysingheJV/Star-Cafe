using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
	public static AudioManager Instance { get; private set; }

	[Header("Audio Mixer & Groups")]
	[SerializeField] private AudioMixer mainAudioMixer;

	public const string MASTER_VOL_KEY = "MasterVolume";
	public const string MUSIC_VOL_KEY = "MusicVolume";
	public const string SFX_VOL_KEY = "SFXVolume";

	private const string MASTER_PREF_KEY = "MasterVolumePreference";
	private const string MUSIC_PREF_KEY = "MusicVolumePreference";
	private const string SFX_PREF_KEY = "SFXVolumePreference";

	public float MasterVolumeSetting { get; private set; }
	public float MusicVolumeSetting { get; private set; }
	public float SFXVolumeSetting { get; private set; }

	// To do when the script is loaded.
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
			enabled = false;
			return;
		}
		LoadVolumeSettings();
	}

	
	void Start()
	{
		ApplyAllLoadedVolumes();
	}

	// Applies all currently stored volume settings to the audio mixer.
	private void ApplyAllLoadedVolumes()
	{
		SetMasterVolume(MasterVolumeSetting);
		SetMusicVolume(MusicVolumeSetting);
		SetSFXVolume(SFXVolumeSetting);
	}

	// Sets the master volume level based on a slider's value.
	public void SetMasterVolume(float sliderValue)
	{
		MasterVolumeSetting = sliderValue;
		mainAudioMixer.SetFloat(MASTER_VOL_KEY, Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f);
		PlayerPrefs.SetFloat(MASTER_PREF_KEY, sliderValue);
		PlayerPrefs.Save();
	}

	// Sets the music volume level based on a slider's value.
	public void SetMusicVolume(float sliderValue)
	{
		MusicVolumeSetting = sliderValue;
		mainAudioMixer.SetFloat(MUSIC_VOL_KEY, Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f);
		PlayerPrefs.SetFloat(MUSIC_PREF_KEY, sliderValue);
		PlayerPrefs.Save();
	}

	// Sets the sound effects volume level based on a slider's value.
	public void SetSFXVolume(float sliderValue)
	{
		SFXVolumeSetting = sliderValue;
		mainAudioMixer.SetFloat(SFX_VOL_KEY, Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f);
		PlayerPrefs.SetFloat(SFX_PREF_KEY, sliderValue);
		PlayerPrefs.Save();
	}

	// Loads saved volume settings from PlayerPrefs when the game starts.
	private void LoadVolumeSettings()
	{
		MasterVolumeSetting = PlayerPrefs.GetFloat(MASTER_PREF_KEY, 0.75f);
		MusicVolumeSetting = PlayerPrefs.GetFloat(MUSIC_PREF_KEY, 0.75f);
		SFXVolumeSetting = PlayerPrefs.GetFloat(SFX_PREF_KEY, 0.75f);

		Debug.Log($"Loaded Volumes: Master={MasterVolumeSetting}, Music={MusicVolumeSetting}, SFX={SFXVolumeSetting}");
		ApplyAllLoadedVolumes();
	}

	// Resets all volume settings to their default values.
	public void ResetVolumeToDefaults()
	{
		SetMasterVolume(0.75f);
		SetMusicVolume(0.75f);
		SetSFXVolume(0.75f);
	}
}