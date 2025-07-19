
// EndGameCanvas.cs
// version: 1.9
using Godot;
using System;

public partial class EndGameCanvas : CanvasLayer
{
    [Signal]
    public delegate void MenuRequestedEventHandler();
    [Signal]
    public delegate void RetryRequestedEventHandler();

    private Label messageLabel;
    private Label timeLabel;
    private Label rankLabel;
    private Button menuButton;
    private Button retryButton;

    private AudioStreamPlayer audioPlayer;
    private AudioStream winSound;
    private AudioStream loseSound;
    public bool AudioEnabled = true;

    public override void _Ready()
    {
        messageLabel = GetNode<Label>("%MessageLabel");
        timeLabel = GetNode<Label>("%TimeLabel");
        rankLabel = GetNode<Label>("%RankLabel");
        menuButton = GetNode<Button>("%MenuButton");
        retryButton = GetNode<Button>("%RetryButton");

        audioPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        winSound = GD.Load<AudioStream>("res://audio/victory.mp3");
        loseSound = GD.Load<AudioStream>("res://audio/gameOver.mp3");
    }

    public void SetupEndGame(bool won, double completionTime, int maxTime)
    {
        SetMessage(won ? "You Won!" : "Nice Try!");
        SetTime(completionTime);
        SetRank(completionTime, maxTime, won);
        if(AudioEnabled)
        {
            GD.Print(won);
            if (won) { 
                audioPlayer.Stream = winSound;

            }
            else
            {
                audioPlayer.Stream = loseSound; 
            }
            audioPlayer.Play();
        }
    }

    public void SetMessage(string message)
    {
        if (messageLabel != null)
            messageLabel.Text = message;
    }

    public void SetTime(double timeInSeconds)
    {
        if (timeLabel != null)
        {
            int minutes = (int)timeInSeconds / 60;
            int seconds = (int)timeInSeconds % 60;
            timeLabel.Text = $"Time: {minutes:D2}:{seconds:D2}";
        }
    }

    public void SetRank(double completionTime, int maxTime, bool won)
    {
        if (rankLabel == null) return;

        string rankText;
        Color rankColor;

        if (!won)
        {
            rankText = "";
            rankColor = Colors.White;
        }
        else if (completionTime <= maxTime / 3.0)
        {
            rankText = "Gold Medal";
            rankColor = Colors.Gold;
        }
        else if (completionTime <= maxTime / 2.0)
        {
            rankText = "Silver Medal";
            rankColor = Colors.Silver;
        }
        else
        {
            rankText = "White Medal";
            rankColor = Colors.White;
        }

        rankLabel.Text = rankText;
        rankLabel.AddThemeColorOverride("font_color", rankColor);
    }

    private void _on_menu_button_pressed()
    {
        audioPlayer.Stop();
        EmitSignal("MenuRequested");
    }

    private void _on_retry_button_pressed()
    {
        audioPlayer.Stop();
        EmitSignal("RetryRequested");
    }
}
