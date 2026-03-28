using UnityEngine;

public class ObjectSwitcher : MonoBehaviour
{
    public GameObject cube;
    public GameObject sphere;
    public GameObject panel;
    public GameObject openButton;

    void Start()
    {
        // Initial state
        cube.SetActive(false);
        sphere.SetActive(false);

        panel.SetActive(true);
        openButton.SetActive(false);
    }

    public void ShowCube()
    {
        cube.SetActive(true);
        sphere.SetActive(false);
    }

    public void ShowSphere()
    {
        cube.SetActive(false);
        sphere.SetActive(true);
    }

    public void ClosePanel()
    {
        panel.SetActive(false);
        openButton.SetActive(true);
    }

    public void OpenPanel()
    {
        panel.SetActive(true);
        openButton.SetActive(false);
    }
}