
// MainMenu.cs
// version: 1.9
using Godot;
using System.Collections.Generic;

public partial class MainMenu : CanvasLayer
{

    // Declare a custom signal (event)
    [Signal]
    public delegate void GameStartedEventHandler();
    [Signal]
    public delegate void ResetEventHandler();
    [Signal]
    public delegate void SetModeEventHandler();
    [Signal]
    public delegate void MuteButtonEventHandler();
    [Signal]
    public delegate void VolumeChangedEventHandler(float value);
    [Signal]
    public delegate void ResetPlayerDataEventHandler();
    [Signal]
    public delegate void ABCAnyEventHandler(bool value);

    public Button startButton;
    public ColorRect modeMenu;
    public Label startLabel;
    public ConfirmationDialog confirmationDialog;

    // Mode checkboxes
    public CheckBox[] modeCheckBoxes = new CheckBox[9];

    // Best time labels
    public Label[] bestTimeLabels = new Label[9];

    // Mode data
    private string[] modeTexts = {
        "Default a+?=10",
        "a+b=?",
        "a+b+c=?",
        "10-?=a",
        "a-b=?",
        "a+b-c=?",
        "a-b+c=?",
        "a-b-c=?",
        "Create"
    };

    private string[] modeValues = {
        "a+?=10",
        "a+b=?",
        "a+b+c=?",
        "10-?=a",
        "a-b=?",
        "a+b-c=?",
        "a-b+c=?",
        "a-b-c=?",
        "create"
    };

    private int selectedModeIndex = 0;


    private int[] maxTimes;

    public int medalCount = 0; // For Checken Count

    public override void _Ready()
    {
        startButton = GetNode<Button>("%StartButton");
        modeMenu = GetNode<ColorRect>("ModeMenu");
        startLabel = GetNode<Label>("%StartLabel");
        confirmationDialog = GetNode<ConfirmationDialog>("ConfirmationDialog");

        //string targetNodePath = $"ModeMenu/VBoxContainer2/HBoxContainer{i + 1}";
        
        
        // Initialize mode checkboxes
        for (int i = 0; i < 9; i++)
        {
            Label bestTimeLabel = new Label();
            bestTimeLabel.Name = $"BestTimeLabel{i + 1}"; // Set a unique name for each label but not currently used
            bestTimeLabel.Text = "something";
            GetNode<HBoxContainer>($"%HBoxContainer{i+1}").AddChild(bestTimeLabel);
            modeCheckBoxes[i] = GetNode<CheckBox>($"%CheckBox{i + 1}");
            modeCheckBoxes[i].Text = modeTexts[i];
            bestTimeLabels[i] = bestTimeLabel;

        }

        // Hide mode menu initially
        modeMenu.Hide();

        SetMaxTimes();

        // Setup confirmation dialog
        confirmationDialog.DialogText = "Are you sure you want to delete all player data? This action cannot be undone.";
        confirmationDialog.Title = "Confirm Data Deletion";
        confirmationDialog.Confirmed += OnConfirmResetPlayerData;

        // Set initial mode
        UpdateModeSelection(0);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
    public void SetMaxTimes()
    {
        maxTimes = new int[9];
        for (int i = 0; i < 9; i++)
        {
            Label timeLabel = GetNode<Label>($"%TimeLabel{i + 1}");
            string[] parts = timeLabel.Text.Split(':');
            int minutes = int.Parse(parts[0]);
            int seconds = int.Parse(parts[1]);
            maxTimes[i] = minutes * 60 + seconds;
        }
    }
    public int GetMaxTime()
    {
        return maxTimes[selectedModeIndex];
    }
    public void UpdateBestTimes(System.Collections.Generic.List<Win> winHistory)
    {
        medalCount = 0; // Reset medal count

        // Initialize all labels to show no best time
        for (int i = 0; i < bestTimeLabels.Length; i++)
        {
            bestTimeLabels[i].Text = "Best: --:--";
        }

        int[] bestTimes = new int[9];

        // Update labels with actual best times from win history
        foreach (var win in winHistory)
        {
            int modeIndex = GetModeIndex(win.Mode);
            if (modeIndex >= 0 && modeIndex < bestTimeLabels.Length)
            {
                // Check if this is a better time than currently displayed
                string currentText = bestTimeLabels[modeIndex].Text;
                if (currentText == "Best: --:--" || win.Time < GetTimeFromLabel(currentText))
                {
                    int minutes = win.Time / 60;
                    int seconds = win.Time % 60;
                    bestTimeLabels[modeIndex].Text = $"Best: {minutes:D2}:{seconds:D2}";
                    bestTimes[modeIndex] = win.Time;
                }
            }
        }
        for (int i = 0; i<bestTimes.Length; i++)
        {
            Texture2D henGrey = GD.Load<Texture2D>("res://images/henGrey.png");
            Texture2D hen = GD.Load<Texture2D>("res://images/hen.png");
            Texture2D henWhite = GD.Load<Texture2D>("res://images/henWhite.png");
            Texture2D henSilver = GD.Load<Texture2D>("res://images/henSilver.png");
            Texture2D henGold = GD.Load<Texture2D>("res://images/henGold.png");
            Texture2D[] textures = new Texture2D[3];
            if (bestTimes[i] == 0)
            {
                textures[0] = henGrey;
                textures[1] = henGrey;
                textures[2] = henGrey;

            }
            else if (bestTimes[i] <= maxTimes[i] / 3)
            {
                medalCount += 3;
                textures[0] = henWhite;
                textures[1] = henSilver;
                textures[2] = henGold;
            }
            else if (bestTimes[i] <= maxTimes[i] / 2)
            {
                medalCount += 2;
                textures[0] = henWhite;
                textures[1] = henSilver;
                textures[2] = henGrey;
            }
            else if (bestTimes[i] <= maxTimes[i])
            {
                medalCount++;
                textures[0] = henWhite;
                textures[1] = henGrey;
                textures[2] = henGrey;
            }
            else
            {
                textures[0] = henGrey;
                textures[1] = henGrey;
                textures[2] = henGrey;
            }
                //Set the hen TexturRects 
                for (int j = 0; j < 3; j++)
                {
                    // Get the TextureRect node
                    TextureRect textureRect = GetNode<TextureRect>($"%TextureRect{i + 1}{j + 1}");
                    // Assign the texture to the TextureRect
                    textureRect.Texture = textures[j];
                }
        }
    }

    private int GetModeIndex(string mode)
    {
        for (int i = 0; i < modeValues.Length; i++)
        {
            if (modeValues[i] == mode)
                return i;
        }
        return -1;
    }

    private int GetTimeFromLabel(string labelText)
    {
        // Extract time from "Best: MM:SS" format
        if (labelText.StartsWith("Best: ") && labelText.Length > 6)
        {
            string timeStr = labelText.Substring(6);
            string[] parts = timeStr.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int minutes) && int.TryParse(parts[1], out int seconds))
            {
                return minutes * 60 + seconds;
            }
        }
        return int.MaxValue; // Return max value if parsing fails
    }
    private void _on_start_button_pressed()
    {
        EmitSignal("GameStarted");
    }
    private void _on_reset_button_pressed()
    {
        EmitSignal("Reset");
    }
    private void _on_mute_button_pressed()
    {
        EmitSignal("MuteButton");
    }
    private void _on_volume_slider_value_changed(float value)
    {
        GD.Print($"Volume changed to: {value}");

        // Convert slider value (0-100) to decibels for AudioServer
        // Godot's AudioServer expects volume in decibels, typically ranging from -80 to 0
        float volumeDb;
        if (value <= 0)
        {
            volumeDb = -80; // Effectively mute
        }
        else
        {
            // Convert 0-100 range to -30 to 0 dB range for better audio experience
            volumeDb = (value / 100.0f) * 30.0f - 30.0f;
        }

        // Set the master bus volume
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), volumeDb);

        EmitSignal("VolumeChanged", value);
    }

    private void _on_mode_button_pressed()
    {
        modeMenu.Show();
        
    }

    private void _on_reset_player_data_button_pressed()
    {
        confirmationDialog.PopupCentered();
    }

    private void OnConfirmResetPlayerData()
    {
        EmitSignal("ResetPlayerData");
    }
    private void _on_abc_any_check_box_pressed()
    {
        EmitSignal("ABCAny", GetNode<CheckBox>("%ABCAnyCheckBox").ButtonPressed);
    }
    private void UpdateModeSelection(int selectedIndex)
    {
        selectedModeIndex = selectedIndex;

        // Update checkboxes to behave like radio buttons
        for (int i = 0; i < modeCheckBoxes.Length; i++)
        {
            modeCheckBoxes[i].ButtonPressed = (i == selectedIndex);
        }
        EmitSignal("SetMode");
        // Update start label
        startLabel.Text = $"Mode: {modeTexts[selectedIndex]}";
    }

    public string GetCurrentMode()
    {
        return modeValues[selectedModeIndex];
    }
    private void _on_mode_back_button_pressed()
    {
        modeMenu.Hide();
    }
    private void _on_check_box_1_toggled(bool buttonPressed)
    {
        if (buttonPressed)
        {
            UpdateModeSelection(0);
        }
    }

    private void _on_check_box_2_toggled(bool buttonPressed)
    {
        if (buttonPressed)
        {
            UpdateModeSelection(1);
        }
    }

    private void _on_check_box_3_toggled(bool buttonPressed)
    {
        if (buttonPressed)
        {
            UpdateModeSelection(2);
        }
    }

    private void _on_check_box_4_toggled(bool buttonPressed)
    {
        if (buttonPressed)
        {
            UpdateModeSelection(3);
        }
    }

    private void _on_check_box_5_toggled(bool buttonPressed)
    {
        if (buttonPressed)
        {
            UpdateModeSelection(4);
        }
    }

    private void _on_check_box_6_toggled(bool buttonPressed)
    {
        if (buttonPressed)
        {
            UpdateModeSelection(5);
        }
    }

    private void _on_check_box_7_toggled(bool buttonPressed)
    {
        if (buttonPressed)
        {
            UpdateModeSelection(6);
        }
    }

    private void _on_check_box_8_toggled(bool buttonPressed)
    {
        if (buttonPressed)
        {
            UpdateModeSelection(7);
        }
    }

    private void _on_check_box_9_toggled(bool buttonPressed)
    {
        if (buttonPressed)
        {
            UpdateModeSelection(8);
        }
    }
}
