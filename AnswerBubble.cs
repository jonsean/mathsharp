
// AnswerBubble.cs
// version: 1.9
using Godot;
using System;

public partial class AnswerBubble : Sprite2D
{
    private Polygon2D polygon;
    private Area2D area2D;
    private CollisionPolygon2D collisionPolygon;
    public Label ansLabel;

    public float fallSpeed;
    public float horizontalSpeed; // Horizontal speed for future use
    public string answerValue;
    private Random random = new Random();

    [Signal]
    public delegate void ClickedAnswerBubbleEventHandler(AnswerBubble clickedBubble);

    public override void _Ready()
    {
        InitializeComponents();
        SetRandomStartPosition();
        SetRandomSpeed();
        if (area2D == null)
        {
            GD.PrintErr("Area2D node not found! Please ensure it exists in the scene.");
            return;
        }
        area2D.InputEvent += OnArea2dInputEvent;
        area2D.InputPickable = true;
    }
    private void SetRandomSpeed()
    {
        // Vary fall speed between 50-150 pixels per second
        fallSpeed = (float)(random.NextDouble() * 100 + 100);
        // Set horizontal speed to 0 for now, can be used later
        horizontalSpeed = (float)(random.NextDouble() * 100 - 50);
    }

    private void OnArea2dInputEvent(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.Pressed)
        {
            if (mouseButtonEvent.ButtonIndex == MouseButton.Left)
            {
                // Emit the custom signal
                EmitSignal("ClickedAnswerBubble", this);
            }
        }
    }
    public override void _Process(double delta)
    {
    }

    private void InitializeComponents()
    {
        polygon = GetNodeOrNull<Polygon2D>("Polygon2D");
        area2D = GetNodeOrNull<Area2D>("Area2D");
        collisionPolygon = GetNodeOrNull<CollisionPolygon2D>("Area2D/CollisionPolygon2D");
        ansLabel = GetNodeOrNull<Label>("AnsLabel");

    }

    private void SetRandomStartPosition()
    {
        // Start at random X position at top of screen
        Vector2 viewportSize = GetViewportRect().Size;
        float randomX = (float)random.NextDouble() * (viewportSize.X - 100) + 50;
        Position = new Vector2(randomX, -50); // Start above screen
    }

    public void AnimateMovement(double delta)
    {
        // Move bubble downward
        Position += new Vector2(horizontalSpeed * (float)delta, fallSpeed * (float)delta);
    }
    public void AdjustFallSpeed(double delta)
    {
        // slows fall speed after the bubble reaches half the screen height
        double screenY = GetViewport().GetVisibleRect().Size.Y;
        double bubbleY = Position.Y;
        if (bubbleY > 0 && screenY / bubbleY > 2)
        {
            if (fallSpeed > 30)
                fallSpeed -= 20 * (float)delta;
        }
    }
    public void CheckIfOffScreen()
    {
        Vector2 viewportSize = GetViewportRect().Size;
        if (Position.Y > viewportSize.Y + 50) // Off bottom of screen
        {
            QueueFree();
        }
    }

    public void SetAnswer(String answer)
    {
       
        answerValue = answer;
       
        SetupPolygon();
        SetupLabel();
        SetRandomColor();
    }


    private void SetupLabel()
    {
        if (ansLabel != null)
        {
            ansLabel.Text = answerValue.ToString();
            ansLabel.Modulate = new Color(1, 1, 1, 1); // White text
        }
    }
    private void SetupPolygon()
    {
        if (polygon == null) return;
        int ansValue = 100;
        if(int.TryParse(answerValue, out int value))
        {
            ansValue = value;
        }
        int pointCount = Math.Max(3, Math.Abs(ansValue)); // Minimum 3 points (triangle)
        Vector2[] points = GeneratePolygonPoints(pointCount, 60.0f); // 30 pixel radius
        polygon.Polygon = points;
        polygon.Position = new Vector2(0, 0); // Center polygon at sprite origin
        // Match collision polygon to poly 
        collisionPolygon.Polygon = points; 
        collisionPolygon.Position = polygon.Position;   
    }

    private Vector2[] GeneratePolygonPoints(int pointCount, float radius)
    {
        Vector2[] points = new Vector2[pointCount];
        float angleStep = 2 * Mathf.Pi / pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float angle = i * angleStep - Mathf.Pi / 2; // Start from top
            points[i] = new Vector2(
                radius * Mathf.Cos(angle),
                radius * Mathf.Sin(angle)
            );
        }

        return points;
    }

    private void SetRandomColor()
    {
        if (polygon != null)
        {
            // Generate random bright colors
            Color randomColor = new Color(
                (float)random.NextDouble() * 0.5f + 0.5f, // 0.5-1.0 range for brightness
                (float)random.NextDouble() * 0.5f + 0.5f,
                (float)random.NextDouble() * 0.5f + 0.5f,
                0.8f // Slight transparency
            );
            polygon.Color = randomColor;
        }
    }

    public int GetAnswerValue()
    {
        if (int.TryParse(answerValue, out int answerValueInt))
        {
            return answerValueInt;
        }
        else
        {
            GD.PrintErr("Invalid answer value: " + answerValue);
            return 0; // Default to 0 if parsing fails
        }
    }

}
