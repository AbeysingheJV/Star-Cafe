using UnityEngine;

public class RadioInteractable : MonoBehaviour
{
	public string interactionPrompt = "Change Music";
	
	public void Interact()
	{
		if (BackgroundMusicPlayer.Instance != null)
		{
			Debug.Log("Radio interacted with, changing track.");
			BackgroundMusicPlayer.Instance.ChangeTrack();
		}
		else
		{
			Debug.LogWarning("RadioInteractable: BackgroundMusicPlayer instance not found!");
		}
	}
}