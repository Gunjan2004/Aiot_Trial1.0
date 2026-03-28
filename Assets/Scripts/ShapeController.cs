using UnityEngine;

public class ShapeController : MonoBehaviour
{
    public GameObject cube;
    public GameObject sphere;

    // Panels
    public GameObject shapePanel;
    public GameObject movementPanel;

    // Buttons
    public GameObject rotateButton;

    enum ShapeType { None, Cube, Sphere }
    enum MovementType { None, Move, Rotate }

    ShapeType activeShape = ShapeType.None;
    MovementType activeMovement = MovementType.None;

    float startY;
    float maxHeight = 0.5f;
    float rotateSpeed = 120f;

    bool goingUp = true;

    void Start()
    {
        cube.SetActive(false);
        sphere.SetActive(false);
    }

    void Update()
    {
        if (activeShape == ShapeType.None) return;

        GameObject obj = GetActiveObject();
        if (obj == null) return;

        // Auto Move (Up/Down)
        if (activeMovement == MovementType.Move)
            MoveUpDown(obj);

        // Rotation (Cube only)
        if (activeMovement == MovementType.Rotate && activeShape == ShapeType.Cube)
            obj.transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
    }

    GameObject GetActiveObject()
    {
        if (activeShape == ShapeType.Cube) return cube;
        if (activeShape == ShapeType.Sphere) return sphere;
        return null;
    }

    // ---------------------------
    // SHAPE SELECTION
    // ---------------------------

    public void SelectCube()
    {
        ResetObjectPosition();
        cube.SetActive(true);
        sphere.SetActive(false);
        activeShape = ShapeType.Cube;
        startY = cube.transform.position.y;
        activeMovement = MovementType.None;
        rotateButton.SetActive(true);
    }

    public void SelectSphere()
    {
        ResetObjectPosition();
        cube.SetActive(false);
        sphere.SetActive(true);
        activeShape = ShapeType.Sphere;
        startY = sphere.transform.position.y;
        activeMovement = MovementType.None;
        rotateButton.SetActive(false);
    }

    // ---------------------------
    // PANEL NAVIGATION
    // ---------------------------

    public void NextStep()
    {
        shapePanel.SetActive(false);
        movementPanel.SetActive(true);
    }

    public void BackStep()
    {
        movementPanel.SetActive(false);
        shapePanel.SetActive(true);
    }

    // ---------------------------
    // BUTTON ACTIONS
    // ---------------------------

    public void ToggleMove()
    {
        if (activeMovement == MovementType.Move)
        {
            activeMovement = MovementType.None;
            ResetObjectPosition();
        }
        else
        {
            activeMovement = MovementType.Move;
            goingUp = true;
        }
    }

    public void ToggleRotate()
    {
        if (activeShape != ShapeType.Cube) return;

        activeMovement = activeMovement == MovementType.Rotate
            ? MovementType.None
            : MovementType.Rotate;
    }

    // ---------------------------
    // MOVEMENT LOGIC
    // ---------------------------

    void MoveUpDown(GameObject obj)
    {
        Vector3 pos = obj.transform.position;

        pos.y += (goingUp ? 1.5f : -1.5f) * Time.deltaTime;

        if (pos.y >= startY + maxHeight) goingUp = false;
        if (pos.y <= startY) goingUp = true;

        obj.transform.position = pos;
    }

    void ResetObjectPosition()
    {
        GameObject obj = GetActiveObject();
        if (obj == null) return;

        Vector3 pos = obj.transform.position;
        pos.y = startY;
        obj.transform.position = pos;
        goingUp = true;
    }
}