using UnityEngine;
using UnityEngine.UI; 


[RequireComponent(typeof(PickupableItem))]
public class CuttableItem : MonoBehaviour
{
	[Header("Cutting Settings")]
	[Tooltip("The prefab to spawn when cutting is complete.")]
	[SerializeField] private GameObject cutPrefab;

	[Tooltip("How long the player needs to hold interact to cut.")]
	[SerializeField] private float cutTime = 2.0f;

	[Header("UI Feedback (Optional)")]
	[Tooltip("Assign a UI Slider to show cutting progress.")]
	[SerializeField] private Slider progressBar; 

	private float currentCutProgress = 0f;
	private bool isBeingCut = false;

	void Start()
	{
		
		if (progressBar != null)
		{
			progressBar.gameObject.SetActive(false);
			progressBar.minValue = 0;
			progressBar.maxValue = cutTime;
			progressBar.value = 0;
		}
	}

	
	public void StartCutting()
	{
		if (isBeingCut || cutPrefab == null) return; 

		Debug.Log($"Started cutting {gameObject.name}");
		isBeingCut = true;
		currentCutProgress = 0f;

		if (progressBar != null)
		{
			progressBar.gameObject.SetActive(true);
			progressBar.value = 0;
		}
	}

	
	public bool UpdateCutting(float deltaTime)
	{
		if (!isBeingCut) return false;

		currentCutProgress += deltaTime;

		if (progressBar != null)
		{
			progressBar.value = currentCutProgress;
		}

		if (currentCutProgress >= cutTime)
		{
			CompleteCutting();
			return true; 
		}
		return false; 
	}

	
	public void CancelCutting()
	{
		if (!isBeingCut) return;

		Debug.Log($"Canceled cutting {gameObject.name}");
		isBeingCut = false;
		currentCutProgress = 0f;

		if (progressBar != null)
		{
			progressBar.value = 0;
			progressBar.gameObject.SetActive(false);
		}
	}

	
	private void CompleteCutting()
	{
		if (!isBeingCut || cutPrefab == null) return; 

		Debug.Log($"Finished cutting {gameObject.name}");
		isBeingCut = false;

		
		Instantiate(cutPrefab, transform.position, transform.rotation);

		if (progressBar != null)
		{
			progressBar.gameObject.SetActive(false); 
		}


		
		Destroy(gameObject);
	}

	
	void Update()
	{
		if (progressBar != null && progressBar.gameObject.activeSelf && progressBar.GetComponentInParent<Canvas>().renderMode == RenderMode.WorldSpace)
		{
			
			progressBar.transform.position = transform.position + Vector3.up * 0.5f; 
																					 
			if (Camera.main != null)
			{
				progressBar.transform.LookAt(Camera.main.transform);
				progressBar.transform.Rotate(0, 180, 0); 
			}
		}
	}


}