
// BgColorRect.cs
// version: 1.9
using Godot;
using System;

public partial class BgColorRect : ColorRect
{
    PackedScene mainMenu;
    MainMenu mainMenuInstance;
    PackedScene gameScene;
    GameCanvas gameSceneInstace;
    PackedScene endGameScene;
    EndGameCanvas endGameInstance;
    public bool gameStarted = false;

    public bool AudioEnabled = true;

    public override void _Ready()
    {

        // Set a custom mouse cursor
        try 
        {   
            Texture2D customCursorTexture = (Texture2D)GD.Load("res://images/retical.png");
            Vector2 textureSize = customCursorTexture.GetSize();
            Vector2 hotspot = new Vector2(textureSize.X / 2.0f, textureSize.Y / 2.0f);
            Input.SetCustomMouseCursor(customCursorTexture, Input.CursorShape.Arrow, hotspot);
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to set custom mouse cursor: ", e.Message);
        }

        
        gameScene = (PackedScene)GD.Load("res://game_canvas.tscn");
        gameSceneInstace = (GameCanvas)gameScene.Instantiate();
        AddChild(gameSceneInstace);

        mainMenu = (PackedScene)GD.Load("res://main_menu.tscn");
        mainMenuInstance = (MainMenu)mainMenu.Instantiate();
        AddChild(mainMenuInstance);

        gameSceneInstace.maxTime = mainMenuInstance.GetMaxTime();

        endGameScene = (PackedScene)GD.Load("res://end_game_canvas.tscn");
        endGameInstance = (EndGameCanvas)endGameScene.Instantiate();
        AddChild(endGameInstance);
        endGameInstance.Hide();

        mainMenuInstance.GameStarted += OnMainMenuGameStarted;
        mainMenuInstance.Reset += OnMainMenuReset;
        mainMenuInstance.ResetPlayerData += OnMainMenuResetPlayerData;
        mainMenuInstance.ABCAny += SetABC;
        mainMenuInstance.SetMode += OnMainMenuSetMode;
        mainMenuInstance.MuteButton += OnMuteButtonPressed;
        mainMenuInstance.VolumeChanged += OnVolumeChanged; // Add this line

        // Connect end game events
        endGameInstance.MenuRequested += OnEndGameMenuRequested;
        endGameInstance.RetryRequested += OnEndGameRetryRequested;

        // Connect game events
        gameSceneInstace.GameWon += OnGameWon;
        gameSceneInstace.GameTimeUp += OnGameTimeUp;

        // Update best times when ready
        UpdateMainMenuBestTimes();
    }

    public override void _Process(double delta)
    {
        if (!gameStarted)
        {
            mainMenuInstance.Show();
            gameSceneInstace.PauseGame();
        }
        else
        {
            mainMenuInstance.Hide();
            mainMenuInstance.startButton.Text = "Resume"; 
            gameSceneInstace.ResumeGame();
        }
    }

    public override void _Input(InputEvent @event)
	{
        if (@event is InputEventKey keyEvent)
		{
			if (keyEvent.IsPressed())
			{
				if (keyEvent.Keycode == Key.Escape)
				{   
                    if(mainMenuInstance.modeMenu.Visible)
                    {
                        mainMenuInstance.modeMenu.Hide();
                    }
                    else 
                    { 
                        SwapGameScene();
                    }
				}
			}
		}
	}

    public void SwapGameScene()
    {
        gameStarted = !gameStarted;
        if (gameStarted)
        {
            mainMenuInstance.Hide();
            gameSceneInstace.ResumeGame();
        }
        else
        {
            mainMenuInstance.Show();
            gameSceneInstace.PauseGame();
            gameStarted = false;
        }
    }

    public void ShowEndGame(bool won, double completionTime, int maxTime)
    {
        endGameInstance.SetupEndGame(won, completionTime, maxTime);
        endGameInstance.Show();
        gameSceneInstace.PauseGame();
        gameStarted = false;
    }

    private void OnGameTimeUp()
    {
        ShowEndGame(false, gameSceneInstace.maxTime, gameSceneInstace.maxTime);
    }

    private void OnEndGameMenuRequested()
    {
        endGameInstance.Hide();
        gameSceneInstace.ResetGame();
        mainMenuInstance.startButton.Text = "Start";
        gameStarted = false;
    }

    private void OnEndGameRetryRequested()
    {
        endGameInstance.Hide();
        gameSceneInstace.ResetGame();
        gameStarted = true;
    }
    private void OnMainMenuGameStarted()
    {
        gameStarted = true;
    }

    private void OnMainMenuReset()
    {
        gameSceneInstace.ResetGame();
        mainMenuInstance.startButton.Text = "Start"; // Reset the start button text
    }

    private void UpdateMainMenuBestTimes()
    {
        mainMenuInstance.UpdateBestTimes(gameSceneInstace.winHistory);
        gameSceneInstace.AddChickens(mainMenuInstance.medalCount);
    }

    private void OnMainMenuResetPlayerData()
    {
        gameSceneInstace.ResetPlayerData();
        UpdateMainMenuBestTimes(); // Update best times after reset
        gameSceneInstace.AddChickens(mainMenuInstance.medalCount);
    }

    private void OnGameWon(double completionTime)
    {
        ShowEndGame(true, completionTime, gameSceneInstace.maxTime);
        UpdateMainMenuBestTimes(); // Update best times after a win
        gameSceneInstace.AddChickens(mainMenuInstance.medalCount);
    }
    private void SetABC(bool enabled)
    {
        if (enabled)
        {
            gameSceneInstace.ABCAny = true;
        }
        else
        {
            gameSceneInstace.ABCAny = false;
        }
    }
    private void OnMainMenuSetMode()
    {
        string currentMode = mainMenuInstance.GetCurrentMode();
        gameSceneInstace.SetCurrentMode(currentMode);
        int maxTime = mainMenuInstance.GetMaxTime();
        gameSceneInstace.maxTime = maxTime;
        gameSceneInstace.ResetGame();
        mainMenuInstance.startButton.Text = "Start";

    }
    private void OnMuteButtonPressed()
    {
        AudioEnabled = !AudioEnabled;
        gameSceneInstace.AudioEnabled = AudioEnabled;
        endGameInstance.AudioEnabled = AudioEnabled;
    }
    private void OnVolumeChanged(float value)
    {
        // The volume is already set in MainMenu, but you can add additional logic here if needed
        GD.Print($"Volume changed in BgColorRect: {value}");
    }
   
    private void LoadChickens() { 
    }
}
