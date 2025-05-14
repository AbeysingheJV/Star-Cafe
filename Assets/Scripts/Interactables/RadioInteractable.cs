using UnityEngine;

public class RadioInteractable : MonoBehaviour
{
	// This method will be called by the player's interaction script
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