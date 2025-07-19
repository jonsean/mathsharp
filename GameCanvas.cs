

//GameCanvas.cs
//vs 1.9
using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using static Godot.HttpRequest;

public class Answer
{
    public DateTime DateTime { get; set; }
    public string Problem { get; set; }
    public int AnswerGiven { get; set; }
    public double TimeToAnswer { get; set; }
}

public class Win
{
    public DateTime DateTime { get; set; }
    public string Mode { get; set; }
    public int Time { get; set; }
}

public partial class GameCanvas : CanvasLayer
{
    [Signal]
    public delegate void GameWonEventHandler(double completionTime);
    [Signal]
    public delegate void GameTimeUpEventHandler();

    // Visible nodes
    public Label problemLabel;
    public Label clockLabel;
    public Label scoreLabel;
    public MarginContainer chickenContainer;
    public ColorRect background;
    private bool shiftRDirection = true;
    private bool shiftGDirection = true; 
    private bool shiftBDirection = true;

    public Timer gameTimer;

    public Timer squakTimer;
    private int squakMaxTime = 60;
    private int chickenCount = 0;

    public int maxTime = 4*60;
    double timeElapsed = 0;

    int score = 0;
    int maxScore = 20;
    private bool gameActive = true;

    // Math problem variables
    private int currentAnswer;
    private Random random = new Random();
    public bool ABCAny = false;
    private int minabc = 0;
    private int maxabc = 10;
    private int maxAnswer = 10;
    private int minanswer = 0;
    private string createPrefix = "Create:";
    private int? neededAnswer = null;

    // AnswerBubbles management
    private List<AnswerBubble> answerBubbles = new List<AnswerBubble>();
    private List<AnswerBubble> recentlyClicked = new List<AnswerBubble>();

    // Data tracking
    public List<Answer> answerHistory = new List<Answer>();
    public List<Win> winHistory = new List<Win>();
    private string answersPath = "user://data.csv";
    private string winsPath = "user://wins.csv";
    public string currentMode = "a+?=10";
    private int problemStartTime = 0;

    // Audio management
    public AudioStreamPlayer musicPlayer;
    public AudioStreamPlayer soundsPlayer;
    public AudioStreamPlayer chickenPlayer1;
    public AudioStreamPlayer chickenPlayer2;
    public List<string> playList = new List<string>();
    private int musicTrackIndex = 0;
    AudioStream correctSound;
    AudioStream incorrectSound;
    AudioStream squak1;
    AudioStream squak2;
    public bool AudioEnabled = true;

    public override void _Ready()
    {


        problemLabel = GetNode<Label>("%ProblemLabel");
        clockLabel = GetNode<Label>("%ClockLabel");
        scoreLabel = GetNode<Label>("%ScoreLabel");
        musicPlayer = GetNode<AudioStreamPlayer>("%MusicPlayer");
        soundsPlayer = GetNode<AudioStreamPlayer>("%SoundsPlayer");
        chickenPlayer1 = GetNode<AudioStreamPlayer>("%ChickenPlayer1");
        chickenPlayer2 = GetNode<AudioStreamPlayer>("%ChickenPlayer2");
        chickenPlayer1.MaxPolyphony = 1;
        chickenPlayer2.MaxPolyphony = 2;
        chickenContainer = GetNode<MarginContainer>("%ChickenContainer");
        background = GetNode<ColorRect>("GBColorRect");

        GenerateNewProblem();
        UpdateScoreDisplay();

        gameTimer = GetNode<Timer>("GameTimer");
        gameTimer.WaitTime = .5f;
        gameTimer.OneShot = false;
        gameTimer.Start();
        gameTimer.Timeout += OnGameTimerTimeout;

        LoadDataFromFiles();

        LoadAudio();
        squakTimer = GetNode<Timer>("SquakTimer");
        squakTimer.WaitTime = 1;
        squakTimer.Timeout += OnSquakTimerTimeout;
    }

    

    public override void _Process(double delta)
    {
        
        if (gameActive)
        {
            UpdateClockDisplay();

            // Clean up destroyed bubbles
            answerBubbles.RemoveAll(bubble => !IsInstanceValid(bubble));

            // Move and check bubbles
            foreach (var bubble in answerBubbles)
            {
                bubble.AnimateMovement(delta);
                bubble.AdjustFallSpeed(delta);
                bubble.CheckIfOffScreen();
            }
            ProcessRecentlyClicked();
            background.Color = shiftColor(background.Color);

        }
    }
    public Color shiftColor(Color color)
    {
        float shift = 0.003f;
        float r = .1f;
        float g = .2f;
        float b = .1f;
        if (color.R >= .70 || color.R <= 0)
        {
            shiftRDirection = !shiftRDirection; // Reverse direction when hitting bounds
        }
        if (color.G >= .3 || color.G <= 0)
        {
            shiftGDirection = !shiftGDirection; // Reverse direction when hitting bounds
        }
        if (color.B >= 1 || color.B <= 0)
        {
            shiftBDirection = !shiftBDirection; // Reverse direction when hitting bounds
        }
        if (shiftRDirection) r = -r; // Reverse shift direction if needed
        if (shiftGDirection) g = -g; // Reverse shift direction if needed
        if (shiftBDirection) b = -b; // Reverse shift direction if needed
        // Shift the color by a given amount
        return new Color(
            Mathf.Clamp(color.R + shift*r, 0, 1),
            Mathf.Clamp(color.G + shift*g, 0, 1),
            Mathf.Clamp(color.B + shift*b, 0, 1),
            color.A
        );
    }
    public void AddChickens(int count)
    {
        if(chickenContainer == null)
        {
            GD.PrintErr("Chicken container not found!");
            return;
        }
        // Clear existing chickens in the container
        foreach (var child in chickenContainer.GetChildren())
        {
                child.QueueFree();
        }

        PackedScene chickenScene = (PackedScene)GD.Load("res://chicken.tscn");
        if (chickenScene == null)
        {
            GD.PrintErr("Failed to load chicken.");
            return;
        }
        float containerWidth = chickenContainer.Size.X;
        float centerWidth = containerWidth * 2f / 3f;
        float startX = (containerWidth - centerWidth) / 2f;

        for (int i = 0; i < count; i++)
        {
            Chicken chicken = (Chicken)chickenScene.Instantiate();
            chickenContainer.AddChild(chicken);

            // Set random position in center 2/3
            float x = startX + GD.Randf() * centerWidth;
            chicken.Position = new Vector2(x, chicken.Position.Y);
        }
        chickenCount = count;
        int t = random.Next(1, Math.Max(squakMaxTime / chickenCount, 2));
        squakTimer.WaitTime = Math.Max(t, .1);
        squakTimer.Start();
    }
    private void _on_gb_color_rect_resized() 
    { 
        GD.Print("Background resized, re-adding chickens.");
        if(chickenCount <= 0)
        {
            GD.Print("No chickens to add, skipping.");
            return;
        }
        Callable callable = new Callable(this, nameof(AddChickens));
        callable.CallDeferred(chickenCount);

    }
    private void OnSquakTimerTimeout()
    {
        int t = random.Next(1,Math.Max(squakMaxTime / chickenCount,2));
        squakTimer.WaitTime = Math.Max(t,.1);
        squakTimer.Start();
        if (AudioEnabled)
        {
            AudioStreamPlayer player = random.Next(0, 2) == 0 ? chickenPlayer1 : chickenPlayer2;
            player.Play();
        }

    }
    private void LoadAudio()
    {
        // Load audio files if needed
        // This is a placeholder for any audio loading logic you might want to implement
        GD.Print("Loading audio files...");
        string path = "res://audio/music"; // Change this to your target directory
        DirAccess dir = DirAccess.Open(path);

        if (dir != null)
        {
            dir.ListDirBegin();
            string fileName = dir.GetNext();

            while (!string.IsNullOrEmpty(fileName))
            {
                // This is required for export. Godot will only export the .import files on build.
                if (fileName.EndsWith(".import"))
                {
                    fileName = fileName.Replace(".import", "");
                }
                if (!dir.CurrentIsDir() && !fileName.Contains("import"))
                {
                    if (fileName.EndsWith(".mp3") || fileName.EndsWith(".wav") || fileName.EndsWith(".ogg"))
                    {
                        playList.Add(path + "/" + fileName);
                    }
                }
                fileName = dir.GetNext();
            }

            dir.ListDirEnd();
            musicTrackIndex = random.Next(0, playList.Count);
            musicPlayer.Stream = GD.Load<AudioStream>(playList[musicTrackIndex]);
            musicPlayer.StreamPaused = true;
            //musicPlayer.Play();

            correctSound = GD.Load<AudioStream>("res://audio/correct.mp3");
            incorrectSound = GD.Load<AudioStream>("res://audio/incorrect.mp3");
            
            squak1 = GD.Load<AudioStream>("res://audio/chicken1.mp3");
            squak2 = GD.Load<AudioStream>("res://audio/chicken2.mp3");
            chickenPlayer1.Stream = squak1;
            chickenPlayer2.Stream = squak2;

        }
        else
        {
            GD.PrintErr("Failed to load music: " + path);
        }
        
    }
    private void _on_music_player_finished() {
        if (AudioEnabled)
        {
            musicPlayer.Play(); // Restart the music when it finishes
        } 
           
    }
    private void LoadDataFromFiles()
    {
        // Load answer history
        if (Godot.FileAccess.FileExists(answersPath))
        {
            using var file = Godot.FileAccess.Open(answersPath, Godot.FileAccess.ModeFlags.Read);
            if (file != null)
            {
                while (!file.EofReached())
                {
                    string line = file.GetLine();
                    //GD.Print($"Read line: {line}");
                    if (!string.IsNullOrEmpty(line))
                    {
                        string[] parts = line.Split(',');
                        if (parts.Length >= 4)
                        {
                            var answer = new Answer
                            {
                                DateTime = DateTime.Parse(parts[0]),
                                Problem = parts[1],
                                AnswerGiven = int.Parse(parts[2]),
                                TimeToAnswer = double.Parse(parts[3])
                            };
                            answerHistory.Add(answer);
                        }
                    }
                }
            }
        }
        else
        {
                       GD.Print("No answer history file found, starting fresh.");
        }

        // Load win history
        if (Godot.FileAccess.FileExists(winsPath))
        {
            using var file = Godot.FileAccess.Open(winsPath, Godot.FileAccess.ModeFlags.Read);
            if (file != null)
            {
                while (!file.EofReached())
                {
                    string line = file.GetLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        string[] parts = line.Split(',');
                        if (parts.Length >= 3)
                        {
                            var win = new Win
                            {
                                DateTime = DateTime.Parse(parts[0]),
                                Mode = parts[1],
                                Time = int.Parse(parts[2])
                            };
                            winHistory.Add(win);
                        }
                    }
                }
            }
        }
    }

    public override void _ExitTree()
    {
        SaveDataToFiles();
    }

    private void SaveDataToFiles()
    {
        // Save answer history
        using var answersFile = Godot.FileAccess.Open(answersPath, Godot.FileAccess.ModeFlags.Write);
        if (answersFile != null)
        {
            foreach (var answer in answerHistory)
            {
                string line = $"{answer.DateTime},{answer.Problem},{answer.AnswerGiven},{answer.TimeToAnswer}";
                answersFile.StoreLine(line);
            }
        }

        // Save win history
        using var winsFile = Godot.FileAccess.Open(winsPath, Godot.FileAccess.ModeFlags.Write);
        if (winsFile != null)
        {
            foreach (var win in winHistory)
            {
                string line = $"{win.DateTime},{win.Mode},{win.Time}";
                winsFile.StoreLine(line);
            }
        }
    }

    public void ResetPlayerData()
    {
        // Clear in-memory data
        answerHistory.Clear();
        winHistory.Clear();

        // Delete the files
        if (Godot.FileAccess.FileExists(answersPath))
        {
            DirAccess.RemoveAbsolute(answersPath);
        }

        if (Godot.FileAccess.FileExists(winsPath))
        {
            DirAccess.RemoveAbsolute(winsPath);
        }

        GD.Print("Player data has been reset.");
    }
    public void SetCurrentMode(string mode)
    {
        currentMode = mode;
        GD.Print($"Game mode set to: {mode}");
        // You can add mode-specific logic here later
    }
    private void OnGameTimerTimeout()
    {
        if (gameActive)
        {
            timeElapsed += gameTimer.WaitTime;
            AddAnswerBubble();
        }
    }

    private void GenerateNewProblem()
    {
        GD.Print(currentMode);
        int min = minabc;
        int max = maxabc;
        
        int a, b, c;
        switch (currentMode)
        {
            case "a+?=10":
                a = random.Next(min + 1, max + 1);
                currentAnswer = max - a;
                maxAnswer = max;
                problemLabel.Text = $"{a} + __ = 10";

                break;
            case "a+b=?":
                a = random.Next(min + 1, max+1);
                b = random.Next(min + 1, max+1);
                currentAnswer = a + b;
                maxAnswer = max * 2;
                problemLabel.Text = $"{a} + {b} = __";
                break;
            case "a+b+c=?":
                a = random.Next(min, max + 1);
                b = random.Next(min, max + 1);
                c = random.Next(min, max + 1);
                currentAnswer = a + b + c;
                maxAnswer = max * 3;
                problemLabel.Text = $"{a} + {b} + {c} = __";
                break;
            case "10-?=a":
                a = random.Next(min, max + 1);
                currentAnswer = max - a;
                maxAnswer = max;
                problemLabel.Text = $"10 - __ = {a}";
                break;
            case "a-b=?":
                a = random.Next(min, max + 1);
                if (ABCAny)
                {
                    b = random.Next(min, max + 1); // Ensure b <= a to avoid negative
                }
                else
                {
                    b = random.Next(min, a + 1); // Ensure b <= a to avoid negative
                }
                currentAnswer = a - b;
                maxAnswer = max;
                problemLabel.Text = $"{a} - {b} = __";
                break;
            case "a+b-c=?":
                c = random.Next(min, max + 1);
                if (ABCAny)
                {
                    a = random.Next(min, max + 1);
                    b = random.Next(min, max + 1);
                }
                else
                {
                    do
                    {
                        a = random.Next(min, max + 1);
                        b = random.Next(min, max + 1);
                    } while (a + b < c);
                }
                currentAnswer = a + b - c;
                maxAnswer = max * 2;
                problemLabel.Text = $"{a} + {b} - {c} = __";
                break;
            case "a-b+c=?":
                a = random.Next(min, max + 1);
                b = random.Next(min, max + 1);
                if (a - b < 0 && ABCAny)
                {
                    c = random.Next(Math.Abs(a - b), max + 1);
                }
                else
                {
                    c = random.Next(min, max + 1);
                }
                currentAnswer = a - b + c;
                maxAnswer = max * 2;
                problemLabel.Text = $"{a} - {b} + {c} = __";
                break;
            case "a-b-c=?":
                c = random.Next(min, max + 1);
                if(ABCAny)
                {
                    a = random.Next(min, max + 1);
                    b = random.Next(min, max + 1);
                }
                else
                {
                    b = random.Next(min, max - c + 1); // Ensure b <= max - c
                    a = random.Next(b + c, max + 1);
                }
                currentAnswer = a - b - c;
                maxAnswer = max;
                problemLabel.Text = $"{a} - {b} - {c} = __";
                break;
            case "create":
                GD.Print("Creating custom problem...");
                // Custom problem creation mode
                a = random.Next(min, max*2 + 1);
                currentAnswer = a;
                maxAnswer = max * 2;
                problemLabel.Text = $"{createPrefix} _ = {a}";
                break;
            default:
                // Fallback to default mode
                a = random.Next(min, max + 1);
                currentAnswer = 10 - a;
                problemLabel.Text = $"{a} + __ = 10";
                maxAnswer = max;
                break;
        }
        problemStartTime = (int)timeElapsed;
    }

    private void UpdateClockDisplay()
    {
        double remainingTime = maxTime - timeElapsed;
        if (remainingTime <= 0)
        {
            remainingTime = 0;
            if (gameActive)
            {
                gameActive = false;
                EmitSignal("GameTimeUp");
            }
        }

        int minutes = (int)remainingTime / 60;
        int seconds = (int)remainingTime % 60;
        clockLabel.Text = $"{minutes:D2}:{seconds:D2}";
    }

    private void UpdateScoreDisplay()
    {
        scoreLabel.Text = $"Score: {score}/{maxScore}";
    }

    public void AddAnswerBubble()
    {   
        PackedScene answerBubbleScene = (PackedScene)GD.Load("res://answer_bubble.tscn");
        AnswerBubble answerBubbleInstance = (AnswerBubble)answerBubbleScene.Instantiate();

        AddChild(answerBubbleInstance);
        answerBubbles.Add(answerBubbleInstance);

        answerBubbleInstance.ClickedAnswerBubble += OnAnswerBubbleClicked;

        string answerText = "";
        // handle create mode separately and return 
        if (currentMode == "create")
        {
            bool clearExist = false;
            bool negExists = false;
            bool posExists = false;
            bool answerNeededExists = false;
            foreach (var bubble in answerBubbles)
            {
                if (bubble.ansLabel.Text  == "+")
                {
                    posExists = true;
                }
                if(bubble.ansLabel.Text == "-")
                {
                    negExists = true;
                }
                if (bubble.ansLabel.Text == neededAnswer.ToString())
                {
                    answerNeededExists = true;
                }
                if (bubble.ansLabel.Text == "Clear")
                {
                    clearExist = true;
                }

            }
            if (neededAnswer != null && !answerNeededExists && random.Next(0,2) == 0) 
            {
                GD.Print("needed answer " + neededAnswer);
                answerText = neededAnswer.ToString();
            }
            else if (!negExists)
            {
                answerText = "-";
            }
            else if (!posExists)
            {
                answerText = "+";
            }
            else if (!clearExist && random.Next(0,4) == 0)
            {
                answerText = "Clear";
            }
            else
            {
                if (ABCAny)
                {
                    answerText = random.Next(-maxAnswer, maxAnswer).ToString();
                }
                else
                {
                    answerText = random.Next(minanswer, maxAnswer).ToString();
                }
            }

            answerBubbleInstance.SetAnswer(answerText);
            return;
        }
        // Handle other modes
        // get number  for answer label before creating the bubble 
        bool ansExists = false;
        foreach (var bubble in answerBubbles)
        {
            if (bubble.GetAnswerValue() == currentAnswer)
            {
                ansExists = true;
                break;
            }
        }
        if (!ansExists && random.Next(0,2)==0)// 50% chance to set the current answer if one doesn't exist already
        {
            answerText = currentAnswer.ToString();
        }
        else
        {
           
            if(ABCAny)
            {
                answerText = random.Next(-maxAnswer, maxAnswer).ToString();
            }
            else
            {
                answerText = random.Next(0, maxAnswer).ToString();
            }
        }

        


        answerBubbleInstance.SetAnswer(answerText);
    }
    public void OnAnswerBubbleClicked(AnswerBubble clickedBubble)
    {
        if (!gameActive) return;
        recentlyClicked.Add(clickedBubble);
        answerBubbles.Remove(clickedBubble);

    }

    private void ProcessRecentlyClicked()
    {
        // Process recently clicked bubbles
        if (recentlyClicked.Count == 0) return;
        
        if(currentMode == "create")
        {
            AnswerBubble bubble = recentlyClicked[0];
            processCreateModeBubbleClicked(bubble);
            bubble.QueueFree();
            if (score >= maxScore)
            {
                Win wd = new Win
                {
                    DateTime = DateTime.Now,
                    Mode = currentMode,
                    Time = (int)timeElapsed
                };
                winHistory.Add(wd);
                gameActive = false;
                EmitSignal("GameWon", timeElapsed);
                // Remove the old SwapGameScene call and ResetGame call from here
            }
            recentlyClicked.Clear();
            return;
        }

        bool hasCorrect = false;
        Answer ad = null;
        foreach (var bubble in recentlyClicked)
        {
            if (bubble.GetAnswerValue() == currentAnswer)
            {
                hasCorrect = true;
                int timeTaken = (int)timeElapsed - problemStartTime;
                ad = new Answer
                {
                    DateTime = DateTime.Now,
                    Problem = problemLabel.Text,
                    AnswerGiven = currentAnswer,
                    TimeToAnswer = timeTaken
                };
            }

            bubble.QueueFree();
        }

        if (hasCorrect)
        {   
            soundsPlayer.Stream = correctSound;
            soundsPlayer.Play();
            answerHistory.Add(ad);
            score = Math.Min(20, score + 1);
            GenerateNewProblem();

        }
        else
        {   
            soundsPlayer.Stream = incorrectSound;
            soundsPlayer.Play();
            score = Math.Max(0, score - 1);
        }

        UpdateScoreDisplay();

        if (score >= maxScore)
        {
            Win wd = new Win
            {
                DateTime = DateTime.Now,
                Mode = currentMode,
                Time = (int)timeElapsed
            };
            winHistory.Add(wd);
            gameActive = false;
            EmitSignal("GameWon", timeElapsed);
            // Remove the old SwapGameScene call and ResetGame call from here
        }

        recentlyClicked.Clear();

    }

    private void processCreateModeBubbleClicked(AnswerBubble bubble) 
    {         // Handle the logic for create mode when a bubble is clicked
        string bubbleLabel = bubble.ansLabel.Text;
        if (bubbleLabel == "Clear") {//if it's a clear bubble reset the problem label 
            problemLabel.Text = $"{createPrefix} _ = {currentAnswer}";
            return;
        }
        string problemString = problemLabel.Text.Replace(createPrefix, "");
        problemString = problemString.Replace("_", "");
        GD.Print(problemString);
        GD.Print(problemString.Trim());
        string[] tokens = problemString.Split(' ');
        foreach (var token in tokens)
        {
            GD.Print($"Token: {token}");
        }
        string newLabel = "";
        bool isPos = true;
        bool lastWasInt = false;
        int sum = 0;

        int scoreModifier = 1;

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            GD.Print($"{newLabel} : Processing token: {token}");

            
            if (int.TryParse(token, out int value))
            {
                
                if (isPos)
                {
                    sum += value;
                }
                else
                {
                    sum -= value;
                }
                newLabel = $"{newLabel} {token}";
                GD.Print($"Parsed value: {value} is int: sum {sum}");
                lastWasInt = true;
                scoreModifier += 1;
            }
            else
            {
                if (token == "+")
                {
                    isPos = true;
                    lastWasInt = false;
                }
                else if (token == "-")
                {
                    isPos = false;
                    lastWasInt = false;
                }
                else if (token == "=")
                {
                    //add the value of the bubble to the sum if it is a number
                    if (int.TryParse(bubbleLabel, out int bubbleValue))
                    {
                        //don't make a new label or add the sum just skip this bubble if the last token was an int
                        if (lastWasInt) return;
                        if (isPos)
                        {
                            sum += bubbleValue;
                        }
                        else
                        {
                            sum -= bubbleValue;
                        }

                        GD.Print($"Bubble value: {bubbleValue} added to sum: {sum}");
                    }
                    else if (!lastWasInt) 
                    { // previous wasn't a number and current clicked isn't a number
                        return;
                    }
                        // Ether way, insert it before the equals sign and add the last token and break
                        newLabel = $"{newLabel} {bubbleLabel} _ {token} {tokens[i + 1]}";
                    break;
                }
                else
                {
                    // If it's not a number or operator, skip it
                    GD.Print($"Token is not a number or operator: {token}");
                    continue;
                }
                // Append the token to the new label
                newLabel = $"{newLabel} {token}";
            }
            
        }
        problemLabel.Text = createPrefix + " " + newLabel.Trim();
        if (sum == currentAnswer)
        {
            // Correct answer
            score = Math.Min(20, score + scoreModifier);
            soundsPlayer.Stream = correctSound;
            soundsPlayer.Play();
            neededAnswer = null; // Reset needed answer
            GenerateNewProblem();
            UpdateScoreDisplay();
        }
        else
        {
            timeElapsed -= 2;
            if (bubbleLabel == "+") 
            { 
                GD.Print($"Bubble label is +, setting needed answer to {sum - currentAnswer}");
                neededAnswer = ABCAny ? currentAnswer - sum : Math.Abs(currentAnswer - sum);
            }
            else if(bubbleLabel == "-")
            {
                GD.Print($"Bubble label is -, setting needed answer to {sum - currentAnswer}");
                neededAnswer = ABCAny ? sum - currentAnswer : Math.Abs(sum - currentAnswer);
            }
            else
            {
                GD.Print($"Bubble label is {bubbleLabel}, setting needed answer to null"); 
                neededAnswer = null;
            }

        }
        
        
    }
    
    public void PauseGame()
    {
        gameActive = false;
        gameTimer.Paused = true;
        musicPlayer.StreamPaused = true;


    }

    public void ResumeGame()
    {
        gameActive = true;
        gameTimer.Paused = false;
        if (musicPlayer.GetPlaybackPosition() == 0)
        {
            musicPlayer.Play();
        }
        if (!musicPlayer.Playing && AudioEnabled)
        {
            
            musicPlayer.StreamPaused = false;
        }

    }

    public void ResetGame()
    {
        score = 0;
        timeElapsed = 0;
        //gameActive = true; // I don't think we need this.
        gameTimer.Paused = false;

        // Clear all answer bubbles
        foreach (var bubble in answerBubbles)
        {
            if (IsInstanceValid(bubble))
                bubble.QueueFree();
        }
        answerBubbles.Clear();

        GenerateNewProblem();
        UpdateScoreDisplay();
        musicTrackIndex = (musicTrackIndex + 1) % playList.Count; // Cycle through the playlist
        musicPlayer.Stream = GD.Load<AudioStream>(playList[musicTrackIndex]);
        musicPlayer.Play();

    }


    public void SetProblem()
    {
        GenerateNewProblem();
    }
}
