using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] Slider loadingBar;

    public void Show()
    {
        loadingBar.gameObject.SetActive(true);
        loadingBar.value = 0f;
    }

    public void UpdateProgress(float progress)
    {
        loadingBar.value = progress;
    }

    public void Hide()
    {
        loadingBar.value = 1f;
        loadingBar.gameObject.SetActive(false);
    }
}