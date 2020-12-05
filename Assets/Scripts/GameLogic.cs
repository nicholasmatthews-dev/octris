using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogic : MonoBehaviour
{
    [Header("Assets")]
    [Tooltip("Camera that this logic controller controls")]
    public Camera m_camera;
    [Tooltip("Game asset prefab used to represent tiles in game.")]
    public GameObject m_tile;
    [Tooltip("The object representing the bounding box for the game board.")]
    public GameObject m_boundingbox;
    [Tooltip("The object representing the base platform for the game board.")]
    public GameObject m_baseplate;
    [Tooltip("The particle system that plays when a layer is cleared.")]
    public ParticleSystem m_clearparticles;
    
    [Header("UI")]
    [Tooltip("The canvas representing the main menu")]
    public GameObject m_mainmenu;
    [Tooltip("The canvas representing the in game UI")]
    public GameObject m_ingame;
    [Tooltip("The canvas representing the pause menu.")]
    public GameObject m_pausemenu;
    [Tooltip("The canvas representing the game over screen.")]
    public GameObject m_gameover;
    [Tooltip("The canvas representing the instructions.")]
    public GameObject m_instructions;
    GameObject CurrentCanvas;

    [Header("Grid Size")]
    [Tooltip("Grid size in the X dimension")]
    public int GridX = 5;
    [Tooltip("Grid size in the Y dimension")]
    public int GridY = 8;
    [Tooltip("Grid size in the Z dimension")]
    public int GridZ = 5;

    [Header("Visuals")]
    [Tooltip("Gradient defining possible colors for pieces.")]
    public Gradient GradientColors;

    [Header("Controls")]
    public KeyCode k_Forward = KeyCode.W;
    public KeyCode k_Backward = KeyCode.S;
    public KeyCode k_Left = KeyCode.A;
    public KeyCode k_Right = KeyCode.D;
    public KeyCode k_RotateXZ = KeyCode.Alpha1;
    public KeyCode k_RotateXY = KeyCode.Alpha2;
    public KeyCode k_RotateYZ = KeyCode.Alpha3;
    public KeyCode k_Drop = KeyCode.Space;
    public KeyCode k_Pause = KeyCode.Escape;
    public KeyCode k_RotateCamera = KeyCode.Mouse0;


    /// <summary>
    /// Boolean array representing the fill state of each tile in the grid.
    /// </summary>
    bool[,,] GameState;


    /// <summary>
    /// A class representing the current piece that is in play
    /// </summary>
    GamePiece CurrentPiece;


    /// <summary>
    /// Array containing all the GameObjects associated with the game board.
    /// </summary>
    GameObject[,,] TileLiterals;

    GameObject BoundingBox;
    GameObject BasePlate;


    //Designates where new pieces will spawn in
    int PointerX = 2;
    int PointerY = 7;
    int PointerZ = 2;

    //Variables for controlling the rotation direction of the current game piece
    bool XZClockwise = false;
    bool XYClockwise = false;
    bool YZClockwise = false;

    //Variables related to camera control
    float RotationSpeed = 1f;
    float RotationSpeedMin = 0.5f;
    float RotationSpeedMax = 2f;
    float CameraDistance = 7f;
    float CameraDistanceMax = 18f;
    float CameraDistanceMin = 6f;
    float ZoomSpeed = 4f;
    float ZoomSpeedMin = 2f;
    float ZoomSpeedMax = 8f;
    float CameraAngleXY;
    float CameraAngleXZ;
    float LastCameraAngleXY;
    float LastCameraAngleXZ;
    Vector3 CameraTarget;
    Vector3 LastCameraTarget;

    //Color of the current piece
    Color CurrentColor;

    //DROPPING
    float DropClock = 0f;
    float DropTime = 2f;
    float DropTimeDefault = 2f;
    float DropTimeMin = 0.4f;
    float DropTimeMax = 2f;
    
    //DIFFICULTY
    float DifficultyClock = 0f;
    float DifficultyTime = 10f;

    //CLEARING
    bool Clearing = false;
    float ClearClock = 0f;
    float ClearTime = 2f;
    float ClearTimeDefault = 2f;
    bool[] ClearingLevels;

    //UI
    List<GameObject> CanvasHistory = new List<GameObject>();
    int CanvasDepth = 0;
    bool FreeSpin = true;

    bool NeedsUpdate = true;

    /// <summary>
    /// Represents whether the game is currently paused, true if paused, false if running.
    /// </summary>
    bool Paused = false;

    int Score = 0;

    // Start is called before the first frame update
    void Start()
    {
        //All instantiation goes here
        GameState = new bool[GridX, GridY, GridZ];
        TileLiterals = new GameObject[GridX, GridY, GridZ];
        CurrentPiece = new GamePiece();

        //Sets the pointer position to the top middle of the game board
        PointerX = GridX / 2;
        PointerY = GridY - 1;
        PointerZ = GridZ / 2;

        CurrentPiece.SetPosition(PointerX, PointerY, PointerZ);
        CurrentColor = GradientColors.Evaluate(Random.value);
        
        //Initializes camera settings
        CameraTarget = new Vector3(GridX / 2f, GridY / 2f, GridZ / 2f);
        CameraDistanceMin = GridY;
        CameraDistanceMax = GridY * 3;
        CameraDistance = GridY * 1.5f;
        CameraAngleXY = 135f;
        CameraAngleXZ = 0f;
        LastCameraAngleXY = CameraAngleXY;
        LastCameraAngleXZ = CameraAngleXZ;
        LastCameraTarget = CameraTarget;

        //Starts game on pause with the main menu active
        m_gameover.SetActive(false);
        m_mainmenu.SetActive(true);
        m_ingame.SetActive(false);
        m_pausemenu.SetActive(false);
        CurrentCanvas = m_mainmenu;
        CanvasHistory.Add(CurrentCanvas);
        Paused = true;
        FreeSpin = true;
        Score = 0;
        DropTime = DropTimeMax;
        DropTimeDefault = DropTimeMax;
        
        //Instantiates the bounding box and scales it to fit the whole grid
        BoundingBox = Instantiate(m_boundingbox, new Vector3(GridX/2f - 0.5f,GridY/2f - 0.5f,GridZ/2f - 0.5f), Quaternion.identity);
        BoundingBox.transform.localScale = new Vector3(0.02f + GridX, 0.02f + GridY, 0.02f + GridZ);
        
        //Instantiates the baseplate and scales it to fit under the grid
        BasePlate = Instantiate(m_baseplate, new Vector3(GridX/2f - 0.5f, -0.5f, GridZ/2f - 0.5f), Quaternion.identity);
        BasePlate.transform.localScale = new Vector3(GridX - 0.01f, 0.05f, GridZ - 0.01f);

        
        //Sets all clearing levels to false, readying the ClearingLevels array
        ClearingLevels = new bool[GridY];
        for (int i = 0; i < GridY; i++)
        {
            ClearingLevels[i] = false;
        }

        //Positions the camera looking down over the game board.
        m_camera.transform.SetPositionAndRotation(new Vector3(GridX/2f, GridY/2f + CameraDistance, GridZ/2f), Quaternion.Euler(90, 0, 0));
        
        //Loops through all values in the game state grid and initializes them
        for (int x = 0; x<GridX; x++)
        {
            for (int y = 0; y<GridY; y++)
            {
                for (int z = 0; z < GridZ; z++)
                {
                    GameState[x, y, z] = false;
                }
            }
        }

        //Creates the actual grid of tile GameObjects which are used for rendering
        for (int x = 0; x < GridX; x++)
        {
            for (int y = 0; y < GridY; y++)
            {
                for (int z = 0; z < GridZ; z++)
                {
                    TileLiterals[x, y, z] = Instantiate(m_tile, new Vector3(x, y, z), Quaternion.identity);
                    TileLogic currentTileLogic = TileLiterals[x, y, z].GetComponent(typeof(TileLogic)) as TileLogic;
                    currentTileLogic.SetPosition(x, y, z);
                }
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();

        //Handles the automatic progression of the game, when not paused or clearing
        if (!Paused && !Clearing) 
        {    
            DropClock += Time.deltaTime;

            //Drops the block down the vertical axis
            if (DropClock > DropTime)
            {
                DropClock = 0f;
                GamePiece testPiece = CurrentPiece.Clone();
                testPiece.MoveY(-1);
                if (CheckPosition(testPiece))
                {
                    MoveY(-1);
                }
                else
                {
                    PlacePiece();
                }
            }

            DifficultyClock += Time.deltaTime;

            //Speeds up block dropping as the game goes on
            if (DifficultyClock > DifficultyTime)
            {
                DifficultyClock = 0f;
                if (DropTimeDefault * 0.95f > DropTimeMin)
                {
                    DropTimeDefault *= 0.95f;
                    ClearTime = DropTimeDefault;

                    if (!Input.GetKey(k_Drop))
                    {
                        DropTime = DropTimeDefault;
                    }
                }
            }
        }

        //Handles the clearing animation
        if (!Paused && Clearing)
        {
            ClearClock += Time.deltaTime;

            if (ClearClock > ClearTime)
            {
                for (int i = 0; i < ClearingLevels.Length; i++)
                {
                    if (ClearingLevels[i])
                    {
                        Instantiate(m_clearparticles, new Vector3(GridX/2, i, GridZ/2), Quaternion.identity);
                    }
                }
                PushDown();
                ClearClock = 0f;
                Clearing = false;
            }
        }

        if (FreeSpin)
        {
            CameraAngleXZ += 15f * Time.deltaTime;
            if (CameraAngleXZ > 360f)
            {
                CameraAngleXZ -= 360f;
            }
        }

        if (NeedsUpdate)
        {
            UpdateBoard();
        }

        if (LastCameraAngleXY != CameraAngleXY || LastCameraAngleXZ != CameraAngleXZ || LastCameraTarget != CameraTarget)
        {
            PositionCamera();
        }
    }

    /// <summary>
    /// Handles any possible input for a player command.
    /// </summary>
    void HandleInput()
    {
        if (Input.GetKeyDown(k_Pause))
        {
            if (CurrentCanvas == m_ingame)
            {
                Pause();
            }
            else if (CurrentCanvas == m_pausemenu)
            {
                Resume();
            }
            else if (CurrentCanvas == m_gameover)
            {

            }
            else
            {
                PreviousCanvas();
            }
        }
        if (!Clearing && !Paused)
        {
            //Sets the keys to be relative to the orientation of the camera
            KeyCode relativeLeft = k_Left;
            KeyCode relativeRight = k_Right;
            KeyCode relativeForward = k_Forward;
            KeyCode relativeBackward = k_Backward;
            if ((CameraAngleXZ >= 0f && CameraAngleXZ < 45f) || (CameraAngleXZ >= 315f && CameraAngleXZ <= 360f))
            {
                //Debug.Log("Primed for relative control in Quadrant 1");
                relativeLeft = k_Left;
                relativeRight = k_Right;
                relativeForward = k_Forward;
                relativeBackward = k_Backward;
            }
            else if (CameraAngleXZ >= 45f && CameraAngleXZ < 135f)
            {
                //Debug.Log("Primed for relative control in Quadrant 2");
                relativeLeft = k_Backward;
                relativeRight = k_Forward;
                relativeForward = k_Left;
                relativeBackward = k_Right;
            }
            else if (CameraAngleXZ >= 135f && CameraAngleXZ < 225f)
            {
                //Debug.Log("Primed for relative control in Quadrant 3");
                relativeLeft = k_Right;
                relativeRight = k_Left;
                relativeForward = k_Backward;
                relativeBackward = k_Forward;
            }
            else if (CameraAngleXZ >= 225f && CameraAngleXZ < 315f)
            {
                //Debug.Log("Primed for relative control in Quadrant 4");
                relativeLeft = k_Forward;
                relativeRight = k_Backward;
                relativeForward = k_Right;
                relativeBackward = k_Left;
            }
            else
            {
                Debug.Log("Tracing the value of CameraAngle XZ is: " + CameraAngleXZ.ToString());
                Debug.Log("No conditions met, this shouldn't be happening.");
            }
            if (Input.GetKeyDown(relativeLeft))
            {
                GamePiece testPiece = CurrentPiece.Clone();
                testPiece.MoveX(-1);
                if (CheckPosition(testPiece))
                {
                    MoveX(-1);
                }
            }
            if (Input.GetKeyDown(relativeRight))
            {
                GamePiece testPiece = CurrentPiece.Clone();
                testPiece.MoveX(1);
                if (CheckPosition(testPiece))
                {
                    MoveX(1);
                }
            }
            if (Input.GetKeyDown(relativeBackward))
            {
                GamePiece testPiece = CurrentPiece.Clone();
                testPiece.MoveZ(-1);
                if (CheckPosition(testPiece))
                {
                    MoveZ(-1);
                }
            }
            if (Input.GetKeyDown(relativeForward))
            {
                GamePiece testPiece = CurrentPiece.Clone();
                testPiece.MoveZ(1);
                if (CheckPosition(testPiece))
                {
                    MoveZ(1);
                }
            }
            if (Input.GetKeyDown(k_RotateXZ))
            {
                GamePiece testPiece = CurrentPiece.Clone();
                testPiece.RotateXZ(!XZClockwise);
                if (CheckPosition(testPiece))
                {
                    RotateXZ();
                }
                else
                {
                    testPiece = CurrentPiece.Clone();
                    testPiece.RotateXZ(XZClockwise);
                    if (CheckPosition(testPiece))
                    {
                        XZClockwise = !XZClockwise;
                        RotateXZ();
                    }
                }
            }
            if (Input.GetKeyDown(k_RotateXY))
            {
                GamePiece testPiece = CurrentPiece.Clone();
                testPiece.RotateXY(!XYClockwise);
                if (CheckPosition(testPiece))
                {
                    RotateXY();
                }
                else
                {
                    testPiece = CurrentPiece.Clone();
                    testPiece.RotateXY(XYClockwise);
                    if (CheckPosition(testPiece))
                    {
                        XYClockwise = !XYClockwise;
                        RotateXY();
                    }
                }
            }
            if (Input.GetKeyDown(k_RotateYZ))
            {
                GamePiece testPiece = CurrentPiece.Clone();
                testPiece.RotateYZ(!YZClockwise);
                if (CheckPosition(testPiece))
                {
                    RotateYZ();
                }
                else
                {
                    testPiece = CurrentPiece.Clone();
                    testPiece.RotateYZ(YZClockwise);
                    if (CheckPosition(testPiece))
                    {
                        YZClockwise = !YZClockwise;
                        RotateYZ();
                    }
                }
            }
        }
        if (!Paused)
        {
            //Rotate camera when the rotate camera button is held down
            if (Input.GetKey(k_RotateCamera)){

                //Rotates camera around the XZ plane when the mouse is moved in the X direction
                //m_camera.transform.RotateAround(CameraTarget, Vector3.up, 20 * Input.GetAxisRaw("Mouse X") * RotationSpeed);
                
                //Updates the tracker for camera angle around the XZ plane and keeps its value between 0 and 360 degrees
                CameraAngleXZ += 20 * Input.GetAxisRaw("Mouse X") * RotationSpeed;
                if (CameraAngleXZ > 360f)
                {
                    CameraAngleXZ -= 360f;
                }
                else if (CameraAngleXZ < 0f)
                {
                    CameraAngleXZ += 360f;
                }

                //Checks to make sure that the Y mouse movement doesn't put the camera 20 degrees below the horizon (180 degrees) or go past looking straight down at the target
                if (CameraAngleXY + (20 * Input.GetAxisRaw("Mouse Y") * RotationSpeed) < 200 && CameraAngleXY + (20 * Input.GetAxisRaw("Mouse Y") * RotationSpeed) >= 90f)
                {
                    //Transform to find the axis which is perpindicular to the vertically oriented plane between the camera and target
                    CameraAngleXY += 20 * Input.GetAxisRaw("Mouse Y") * RotationSpeed;
                }
            }
            //Zooms in and out using the mouse scrool wheel
            if (Input.GetAxisRaw("Mouse ScrollWheel") != 0f)
            {
                //Checks to make sure the camera doesn't go past the maximum or minimum zoom distance
                if (CameraDistance + ZoomSpeed * -1f * Input.GetAxisRaw("Mouse ScrollWheel") > CameraDistanceMin && CameraDistance + ZoomSpeed * -1f * Input.GetAxisRaw("Mouse ScrollWheel") < CameraDistanceMax)
                {
                    CameraDistance += ZoomSpeed * -1f * Input.GetAxisRaw("Mouse ScrollWheel");
                }
            }
        }
        if (Input.GetKeyDown(k_Drop))
        {
            DropTime = 0.1f;
        }
        if (Input.GetKeyUp(k_Drop))
        {
            DropTime = DropTimeDefault;
        }
    }


    /// <summary>
    /// Completely refreshes the rendering of the game board and sets NeedsUpdate to false.
    /// </summary>
    void UpdateBoard()
    {
        HidePiece();
        for (int x = 0; x < GridX; x++)
        {
            for (int y = 0; y < GridY; y++)
            {
                for (int z = 0; z < GridZ; z++)
                {
                    TileLogic CurrentTileController = TileLiterals[x, y, z].GetComponent(typeof(TileLogic)) as TileLogic;
                    if (GameState[x, y, z])
                    {
                        CurrentTileController.Show();
                    }
                    else
                    {
                        CurrentTileController.Hide();
                    }
                }
            }
        }
        DrawPiece();
        NeedsUpdate = false;
    }

    public void NewGame()
    {
        for (int x = 0; x < GridX; x++)
        {
            for (int y = 0; y < GridY; y++)
            {
                for (int z = 0; z < GridZ; z++)
                {
                    GameState[x, y, z] = false;
                }
            }
        }
        CurrentPiece = new GamePiece();
        CurrentPiece.SetPosition(PointerX, PointerY, PointerZ);
        CurrentColor = GradientColors.Evaluate(Random.value);
        Score = 0;
        DropTimeDefault = DropTimeMax;
        DropTime = DropTimeMax;
        ClearTime = DropTimeMax;
        SetCanvas(m_ingame);
        Paused = false;
        FreeSpin = false;
        UpdateBoard();
    }

    void PositionCamera()
    {
        m_camera.transform.position = new Vector3(-1 * Mathf.Sin(Mathf.Deg2Rad * CameraAngleXZ) * Mathf.Sqrt(1 - Mathf.Pow(Mathf.Sin(Mathf.Deg2Rad * CameraAngleXY), 2)), Mathf.Sin(Mathf.Deg2Rad * CameraAngleXY), -1 * Mathf.Cos(Mathf.Deg2Rad * CameraAngleXZ) * Mathf.Sqrt(1 - Mathf.Pow(Mathf.Sin(Mathf.Deg2Rad * CameraAngleXY), 2))) * CameraDistance + CameraTarget;
        m_camera.transform.LookAt(CameraTarget);
    }

    void DrawPiece()
    {
        PieceComponent[] pieces = CurrentPiece.GetPieces();
        for (int i = 0; i < pieces.Length; i++)
        {
            int finalX = Round(pieces[i].GetX()) + CurrentPiece.GetPositionX();
            int finalY = Round(pieces[i].GetY()) + CurrentPiece.GetPositionY();
            int finalZ = Round(pieces[i].GetZ()) + CurrentPiece.GetPositionZ();
            if (GridX > finalX && finalX >= 0 && GridY > finalY && finalY >= 0 && GridZ > finalZ && finalZ >= 0)
            {
                TileLogic CurrentTileController = TileLiterals[finalX, finalY, finalZ].GetComponent(typeof(TileLogic)) as TileLogic;
                CurrentTileController.SetColor(CurrentColor);
                CurrentTileController.Show();
            }
            else
            {
                Debug.Log("Failed to place at position: (" + finalX.ToString() + "," + finalY.ToString() + "," + finalZ.ToString() + ")");
            }
        }
    }

    void HidePiece()
    {
        PieceComponent[] pieces = CurrentPiece.GetPieces();
        for (int i = 0; i < pieces.Length; i++)
        {
            int finalX = Round(pieces[i].GetX()) + CurrentPiece.GetPositionX();
            int finalY = Round(pieces[i].GetY()) + CurrentPiece.GetPositionY();
            int finalZ = Round(pieces[i].GetZ()) + CurrentPiece.GetPositionZ();
            if (GridX > finalX && finalX >= 0 && GridY > finalY && finalY >= 0 && GridZ > finalZ && finalZ >= 0)
            {
                TileLogic CurrentTileController = TileLiterals[finalX, finalY, finalZ].GetComponent(typeof(TileLogic)) as TileLogic;
                CurrentTileController.Hide();
            }
            else
            {
                Debug.Log("Failed to remove at position: (" + finalX.ToString() + "," + finalY.ToString() + "," + finalZ.ToString() + ")");
            }
        }
    }

    bool CheckPosition(GamePiece input)
    {
        bool valid = true;
        PieceComponent[] pieces = input.GetPieces();
        for(int i = 0; i < pieces.Length; i++)
        {
            int finalX = Round(pieces[i].GetX()) + input.GetPositionX();
            int finalY = Round(pieces[i].GetY()) + input.GetPositionY();
            int finalZ = Round(pieces[i].GetZ()) + input.GetPositionZ();
            //Debug.Log("Checking validity of position: (" + finalX.ToString() + "," + finalY.ToString() + "," + finalZ.ToString() + ")");
            if (!(GridX > finalX && finalX >= 0 && GridY > finalY && finalY >= 0 && GridZ > finalZ && finalZ >= 0))
            {
                valid = false;
                //Debug.Log("Out of bounds at position: (" + finalX.ToString() + "," + finalY.ToString() + "," + finalZ.ToString() + ")");
                return valid;
            }
            else if(GameState[finalX, finalY, finalZ])
            {
                valid = false;
                //Debug.Log("Piece already exists at: (" + finalX.ToString() + "," + finalY.ToString() + "," + finalZ.ToString() + ")");
                return valid;
            }
        }
        return valid;
    }

    void PlacePiece()
    {
        PieceComponent[] pieces = CurrentPiece.GetPieces();
        for(int i = 0; i < pieces.Length; i++)
        {
            int finalX = Round(pieces[i].GetX()) + CurrentPiece.GetPositionX();
            int finalY = Round(pieces[i].GetY()) + CurrentPiece.GetPositionY();
            int finalZ = Round(pieces[i].GetZ()) + CurrentPiece.GetPositionZ();
            GameState[finalX, finalY, finalZ] = true;
            if (CheckForClear(finalY))
            {
                ClearLevel(finalY);
            }
        }
        GamePiece testNewPiece = new GamePiece();
        testNewPiece.SetPosition(PointerX, PointerY, PointerZ);
        if (CheckPosition(testNewPiece))
        {
            CurrentPiece = testNewPiece.Clone();
            CurrentColor = GradientColors.Evaluate(Random.value);
        }
        else
        {
            GameOver();
        }
        UpdateBoard();
    }

    bool CheckForClear(int level)
    {
        for (int x = 0; x < GridX; x++)
        {
            for (int z = 0; z < GridZ; z++)
            {
                if(!GameState[x, level, z])
                {
                    return false;
                }
            }
        }
        return true;
    }

    void ClearLevel(int level)
    {
        Clearing = true;
        for (int x = 0; x < GridX; x++)
        {
            for (int z = 0; z < GridZ; z++)
            {
                TileLogic CurrentTileController = TileLiterals[x, level, z].GetComponent(typeof(TileLogic)) as TileLogic;
                CurrentTileController.Clear(ClearTime);
            }
        }
        ClearingLevels[level] = true;
    }

    void PushDown()
    {
        int levelsCleared = 0;
        for (int i = 0; i < ClearingLevels.Length; i++)
        {
            if (ClearingLevels[i])
            {
                for (int y = i - levelsCleared; y < GridY; y++)
                {
                    for (int x = 0; x < GridX; x++)
                    {
                        for (int z = 0; z < GridZ; z++)
                        {
                            if (y < GridY - 1)
                            {
                                GameState[x, y, z] = GameState[x, y + 1, z];
                                TileLogic currentTileController = TileLiterals[x, y, z].GetComponent(typeof(TileLogic)) as TileLogic;
                                TileLogic previousTileController = TileLiterals[x, y+1, z].GetComponent(typeof(TileLogic)) as TileLogic;
                                currentTileController.SetColor(previousTileController.GetColor());
                            }
                            else
                            {
                                GameState[x, y, z] = false;
                            }
                        }
                    }
                }
                levelsCleared += 1;
                Score += 1;
                ClearingLevels[i] = false;
            }
        }
        UpdateBoard();
    }

    void RotateXZ()
    {
        HidePiece();
        CurrentPiece.RotateXZ(!XZClockwise);
        DrawPiece();
    }
    void RotateXY()
    {
        HidePiece();
        CurrentPiece.RotateXY(!XYClockwise);
        DrawPiece();
    }
    void RotateYZ()
    {
        HidePiece();
        CurrentPiece.RotateYZ(!YZClockwise);
        DrawPiece();
    }

    void MoveX(int input)
    {
        HidePiece();
        CurrentPiece.MoveX(input);
        DrawPiece();
    }

    void MoveY(int input)
    {
        HidePiece();
        CurrentPiece.MoveY(input);
        DrawPiece();
    }

    void MoveZ(int input)
    {
        HidePiece();
        CurrentPiece.MoveZ(input);
        DrawPiece();
    }

    int Round(float input)
    {
        float output = input;
        int outputModifier = 0;
        output -= (int)input;
        if(output >= 0.5f)
        {
            outputModifier = 1;
        }
        return (int)input + outputModifier;
    }

    void GameOver()
    {
        SetCanvas(m_gameover);
        FreeSpin = true;
        Paused = true;
    }

    public int GetScore()
    {
        return Score;
    }

    public void Pause()
    {
        SetCanvas(m_pausemenu);
        Paused = true;
    }

    public void Resume()
    {
        SetCanvas(m_ingame);
        Paused = false;
        FreeSpin = false;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void GoToMainMenu()
    {
        SetCanvas(m_mainmenu);
        FreeSpin = true;
        Paused = true;
    }

    public void SetCanvas(GameObject input)
    {
        CurrentCanvas.SetActive(false);
        CurrentCanvas = input;
        CurrentCanvas.SetActive(true);
        if (CanvasDepth < 10)
        {
            CanvasHistory.Add(CurrentCanvas);
            CanvasDepth++;
        }
        else
        {
            CanvasHistory.RemoveAt(0);
            CanvasHistory.Add(CurrentCanvas);
        }
    }

    public void PreviousCanvas()
    {
        if (CanvasDepth > 0)
        {
            CanvasHistory.RemoveAt(CanvasDepth);
            SetCanvas(CanvasHistory[CanvasDepth - 1]);
            CanvasHistory.RemoveAt(CanvasDepth - 1);
            CanvasDepth -= 2;
        }
    }

    public void SetRotationSpeed(float input)
    {
        RotationSpeed = Mathf.Lerp(RotationSpeedMin, RotationSpeedMax, input);
    }

    public float GetRotationSpeed()
    {
        return (RotationSpeed - RotationSpeedMin) / (RotationSpeedMax - RotationSpeedMin);
    }

    public void SetZoomSpeed(float input)
    {
        ZoomSpeed = Mathf.Lerp(ZoomSpeedMin, ZoomSpeedMax, input);
    }

    public float GetZoomSpeed()
    {
        return (ZoomSpeed - ZoomSpeedMin) / (ZoomSpeedMax - ZoomSpeedMin);
    }

}
