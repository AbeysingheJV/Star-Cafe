using UnityEngine;
using UnityEngine.Rendering; // Required for Volume

// Add the correct using statement based on your Render Pipeline for Bloom
#if USING_URP
using UnityEngine.Rendering.Universal; // For URP Bloom
#elif UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing; // For Built-in PPv2
#elif USING_HDRP
// using UnityEngine.Rendering.HighDefinition; // For HDRP Bloom (ensure correct class name if different)
#endif

public class GraphicsSettingsManager : MonoBehaviour
{
	public static GraphicsSettingsManager Instance { get; private set; }

	[Header("Post-Processing References")]
	[SerializeField] private Volume postProcessVolume; // Assign your global PostProcessVolume GameObject's Volume component

	private const string BLOOM_PREF_KEY = "BloomEnabledPreference";
	public bool IsBloomActive { get; private set; }

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);

		if (postProcessVolume == null)
		{
			postProcessVolume = FindObjectOfType<Volume>(); // Try to find it
			if (postProcessVolume == null)
			{
				Debug.LogError("GraphicsSettingsManager: PostProcessVolume not assigned and not found in scene! Bloom control will not work.");
			}
		}
		LoadGraphicsSettings();
	}

	void Start()
	{
		// Apply loaded settings at start, ensuring the volume profile is likely ready
		if (postProcessVolume != null && postProcessVolume.profile != null)
		{
			ApplyBloomSetting(IsBloomActive);
		}
		else if (postProcessVolume != null && postProcessVolume.sharedProfile != null) // Fallback for sharedProfile if .profile is null initially
		{
			ApplyBloomSetting(IsBloomActive);
		}
		else
		{
			Debug.LogWarning("GraphicsSettingsManager: PostProcessVolume or its profile not ready in Start. Bloom might not apply immediately.");
		}
	}

	public void SetBloom(bool isActive)
	{
		IsBloomActive = isActive;
		ApplyBloomSetting(isActive);
		PlayerPrefs.SetInt(BLOOM_PREF_KEY, isActive ? 1 : 0);
		PlayerPrefs.Save();
		Debug.Log("Bloom setting saved: " + isActive);
	}

	private void ApplyBloomSetting(bool isActive)
	{
		if (postProcessVolume == null)
		{
			Debug.LogWarning("GraphicsSettingsManager: PostProcessVolume is not assigned. Cannot apply Bloom setting.");
			return;
		}

		// Attempt to get the profile. Use .profile if you want to modify an instance,
		// or .sharedProfile if you want to modify the asset directly (generally .profile is safer for runtime changes if instanced).
		// However, for global volumes, .profile might be what you want if it's instanced at runtime,
		// or sharedProfile if you directly assigned the asset. Let's try with .profile first, then sharedProfile.
		VolumeProfile activeProfile = postProcessVolume.profile;
		if (activeProfile == null) activeProfile = postProcessVolume.sharedProfile;


		if (activeProfile == null)
		{
			Debug.LogWarning("GraphicsSettingsManager: No Volume Profile found on the PostProcessVolume. Cannot apply Bloom setting.");
			return;
		}

		// --- For URP (Universal Render Pipeline) ---
#if USING_URP
		if (activeProfile.TryGet<Bloom>(out var urpBloom))
		{
			urpBloom.active = isActive; // Enable/Disable the entire Bloom effect override
			Debug.Log($"URP Bloom active set to: {isActive}");
		}
		else Debug.LogWarning("GraphicsSettingsManager: Bloom effect not found in URP Volume Profile.");
		return;
#endif

		// --- For Built-in Render Pipeline with Post Processing Stack v2 ---
		// This block will only compile if UNITY_POST_PROCESSING_STACK_V2 is defined AND USING_URP is NOT defined.
#if UNITY_POST_PROCESSING_STACK_V2 && !USING_URP
        // This conversion was the error point. It's only valid if activeProfile IS a PostProcessProfile.
        // This whole block should only be considered if you are actually using PPv2.
        // For PPv2, the `postProcessVolume` field would ideally be of type `PostProcessVolume` not `UnityEngine.Rendering.Volume`
        // If you assigned a GameObject with a PPv2 `PostProcessVolume` component, you'd get its profile.
        // Let's assume if we reach here, it's intended for PPv2.
        UnityEngine.Rendering.PostProcessing.PostProcessProfile builtInProfile = activeProfile as UnityEngine.Rendering.PostProcessing.PostProcessProfile;
        if (builtInProfile != null && builtInProfile.TryGetSettings<UnityEngine.Rendering.PostProcessing.Bloom>(out var builtInBloom))
        {
            builtInBloom.enabled.Override(isActive); 
            // builtInBloom.enabled.value = isActive; // For PPv2, .Override should be enough if the parameter itself is checked.
                                                 // Or more directly: builtInBloom.active = isActive; (if PPv2 Bloom has .active)
                                                 // Check PPv2 Bloom API, it usually is builtInBloom.enabled.value
            Debug.Log($"Built-in PPv2 Bloom enabled set to: {isActive}");
        }
        else Debug.LogWarning("GraphicsSettingsManager: Bloom effect not found in Built-in PPv2 Profile or profile type mismatch.");
        return;
#endif

		// If neither URP nor Built-in PPv2 specific code ran
		// This part should ideally not be reached if one of the defines is correctly set.
#if !USING_URP && !UNITY_POST_PROCESSING_STACK_V2
        Debug.LogWarning("GraphicsSettingsManager: No active Render Pipeline define (USING_URP or UNITY_POST_PROCESSING_STACK_V2) for Bloom control. Please set the correct Scripting Define Symbol in Player Settings (e.g., USING_URP if you use Universal Render Pipeline).");
#endif
	}

	private void LoadGraphicsSettings()
	{
		IsBloomActive = PlayerPrefs.GetInt(BLOOM_PREF_KEY, 1) == 1; // Default to Bloom ON
		Debug.Log($"Loaded Bloom Active: {IsBloomActive}");
	}

	public void ResetGraphicsToDefaults()
	{
		SetBloom(true);
	}
}