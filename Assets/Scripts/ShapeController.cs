/* using UnityEngine;

public class ShapeController : MonoBehaviour
{
    public GameObject cube;
    public GameObject sphere;

    // Panels
    public GameObject shapePanel;
    public GameObject movementPanel;

    // Movement Buttons
    public GameObject moveButton;
    public GameObject rotateButton;
    public GameObject colorButton;

    enum ShapeType { None, Cube, Sphere }
    enum MovementType { None, Move, Rotate, Color }

    ShapeType activeShape = ShapeType.None;
    MovementType activeMovement = MovementType.None;

    float startY;
    float maxHeight = 0.5f;
    float moveSpeed = 0.5f;
    float rotateSpeed = 120f;

    bool goingUp = true;

    Renderer sphereRenderer;
    Renderer cubeRenderer;

    void Start()
    {
        cube.SetActive(false);
        sphere.SetActive(false);

        sphereRenderer = sphere.GetComponent<Renderer>();
        cubeRenderer = cube.GetComponent<Renderer>();
    }

    void Update()
    {
        if (activeShape == ShapeType.None) return;

        GameObject obj = GetActiveObject();

        if (activeMovement == MovementType.Move)
            MoveObject(obj);

        if (activeMovement == MovementType.Rotate && activeShape == ShapeType.Cube)
            obj.transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);

        if (activeMovement == MovementType.Color)
            AnimateColor();
    }

    GameObject GetActiveObject()
    {
        if (activeShape == ShapeType.Cube) return cube;
        if (activeShape == ShapeType.Sphere) return sphere;
        return null;
    }

    // -----------------------
    // SHAPE SELECTION
    // -----------------------

    public void SelectCube()
    {
        ResetObjectPosition();

        cube.SetActive(true);
        sphere.SetActive(false);

        activeShape = ShapeType.Cube;
        startY = cube.transform.position.y;

        activeMovement = MovementType.None;
    }

    public void SelectSphere()
    {
        ResetObjectPosition();

        cube.SetActive(false);
        sphere.SetActive(true);

        activeShape = ShapeType.Sphere;
        startY = sphere.transform.position.y;

        activeMovement = MovementType.None;
    }

    // -----------------------
    // PANEL NAVIGATION
    // -----------------------

    public void NextStep()
    {
        shapePanel.SetActive(false);
        movementPanel.SetActive(true);

        ConfigureMovementButtons();
    }

    public void BackStep()
    {
        movementPanel.SetActive(false);
        shapePanel.SetActive(true);

        ResetObjectPosition();
        activeMovement = MovementType.None;
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
    void ConfigureMovementButtons()
    {
        if (activeShape == ShapeType.Cube)
        {
            rotateButton.SetActive(true);
            colorButton.SetActive(true);
        }

        if (activeShape == ShapeType.Sphere)
        {
            rotateButton.SetActive(false);
            colorButton.SetActive(true);
        }
    }

    // -----------------------
    // MOVEMENT BUTTONS
    // -----------------------

  
    public void ToggleMove()
    {
        GameObject obj = GetActiveObject();

        if (activeMovement == MovementType.Move)
        {
            activeMovement = MovementType.None;

            // Reset height
            if (obj != null)
            {
                Vector3 pos = obj.transform.position;
                pos.y = startY;
                obj.transform.position = pos;
            }
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

        if (activeMovement == MovementType.Rotate)
            activeMovement = MovementType.None;
        else
            activeMovement = MovementType.Rotate;
    }

    public void ToggleColor()
    {
        if (activeMovement == MovementType.Color)
            activeMovement = MovementType.None;
        else
            activeMovement = MovementType.Color;
    }

    // -----------------------
    // MOVEMENT LOGIC
    // -----------------------

    void MoveObject(GameObject obj)
    {
        Vector3 pos = obj.transform.position;

        if (goingUp)
            pos.y += moveSpeed * Time.deltaTime;
        else
            pos.y -= moveSpeed * Time.deltaTime;

        if (pos.y >= startY + maxHeight)
            goingUp = false;

        if (pos.y <= startY)
            goingUp = true;

        obj.transform.position = pos;
    }

    void AnimateColor()
    {
        Color c = new Color(
            Mathf.Sin(Time.time) * 0.5f + 0.5f,
            Mathf.Sin(Time.time + 2) * 0.5f + 0.5f,
            Mathf.Sin(Time.time + 4) * 0.5f + 0.5f
        );

        if (activeShape == ShapeType.Cube)
            cubeRenderer.material.color = c;

        if (activeShape == ShapeType.Sphere)
            sphereRenderer.material.color = c;
    }
} */

using UnityEngine;

public class ShapeController : MonoBehaviour
{
    public GameObject cube;
    public GameObject sphere;

    // Panels
    public GameObject shapePanel;
    public GameObject movementPanel;

    // Buttons (optional for enabling/disabling)
    public GameObject rotateButton;
   // public GameObject colorButton;

    // Joystick
    public FixedJoystick joystick;
    public float moveSpeed = 1.5f;

    enum ShapeType { None, Cube, Sphere }
    enum MovementType { None, Move, Rotate }

    ShapeType activeShape = ShapeType.None;
    MovementType activeMovement = MovementType.None;

    float startY;
    float maxHeight = 0.5f;
    float rotateSpeed = 120f;

    bool goingUp = true;

    Renderer cubeRenderer;
    Renderer sphereRenderer;

    void Start()
    {
        cube.SetActive(false);
        sphere.SetActive(false);

        cubeRenderer = cube.GetComponent<Renderer>();
        sphereRenderer = sphere.GetComponent<Renderer>();
    }

    void Update()
    {
        if (activeShape == ShapeType.None) return;

        GameObject obj = GetActiveObject(); //checks which shape is active
        if (obj == null) return;

        // ---------------------------
        // JOYSTICK MOVEMENT (XZ plane)
        // ---------------------------
        Vector3 move = new Vector3(
            joystick.Horizontal,
            0,
            joystick.Vertical
        );

        if (move.magnitude > 0.1f)
        {
            obj.transform.position += move * moveSpeed * Time.deltaTime;
        }

        // ---------------------------
        // AUTO MOVE (UP/DOWN)
        // ---------------------------
        if (activeMovement == MovementType.Move)
        {
            MoveUpDown(obj);
        }

        // ---------------------------
        // ROTATION (Cube only)
        // ---------------------------
        if (activeMovement == MovementType.Rotate && activeShape == ShapeType.Cube)
        {
            obj.transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
        }

        // ---------------------------
        // COLOR CHANGE
        // ---------------------------
        //if (activeMovement == MovementType.Color)
       // {
        //    AnimateColor();
        //}
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
        //colorButton.SetActive(true);
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
       // colorButton.SetActive(true);
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

        if (activeMovement == MovementType.Rotate)
            activeMovement = MovementType.None;
        else
            activeMovement = MovementType.Rotate;
    }

   // public void ToggleColor()
    //{
    //    if (activeMovement == MovementType.Color)
    //        activeMovement = MovementType.None;
    //   else
    //        activeMovement = MovementType.Color;
//}

    // ---------------------------
    // MOVEMENT LOGIC
    // ---------------------------

    void MoveUpDown(GameObject obj)
    {
        Vector3 pos = obj.transform.position;

        if (goingUp)
            pos.y += 1.5f * Time.deltaTime;
        else
            pos.y -= 1.5f * Time.deltaTime;

        if (pos.y >= startY + maxHeight)
            goingUp = false;

        if (pos.y <= startY)
            goingUp = true;

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

    void AnimateColor()
    {
        Color c = new Color(
            Mathf.Sin(Time.time) * 0.5f + 0.5f,
            Mathf.Sin(Time.time + 2) * 0.5f + 0.5f,
            Mathf.Sin(Time.time + 4) * 0.5f + 0.5f
        );

        if (activeShape == ShapeType.Cube)
            cubeRenderer.material.color = c;

        if (activeShape == ShapeType.Sphere)
            sphereRenderer.material.color = c;
    }
}