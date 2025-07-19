
// Chicken.cs
using Godot;
using System;

public partial class Chicken : Sprite2D
{
    private Timer _bounceTimer;
    private bool _isBouncing = false;
    private int _bouncesRemaining = 0;
    private float _bounceHeight = 0f;
    private float _horizontalDistance = 0f;
    private bool _bouncingRight = true;
    private Vector2 _startPosition;
    private Vector2 _targetPosition;
    private float _bounceProgress = 0f;
    private float _bounceSpeed = 8f; // Speed of individual bounce
    private Random _random = new Random();

    public override void _Ready()
    {

        // Create and setup timer for random bounce intervals
        _bounceTimer = new Timer();
        AddChild(_bounceTimer);
        _bounceTimer.WaitTime = _random.Next(2, 6); // 2-5 seconds between bounce sets
        _bounceTimer.Timeout += StartBounceSet;
        _bounceTimer.Start();

        _startPosition = Position;
    }

    public override void _Process(double delta)
    {
        if (_isBouncing && _bouncesRemaining > 0)
        {
            // Update bounce animation
            _bounceProgress += (float)delta * _bounceSpeed;

            if (_bounceProgress >= 1f)
            {
                // Complete current bounce
                Position = _targetPosition;
                _bouncesRemaining--;

                if (_bouncesRemaining > 0)
                {
                    // Start next bounce in the set
                    StartNextBounce();
                }
                else
                {
                    // End bounce set
                    _isBouncing = false;
                    _bounceTimer.WaitTime = _random.Next(2, 6);
                    _bounceTimer.Start();
                }
            }
            else
            {
                // Animate current bounce with parabolic arc
                Vector2 currentPos = _startPosition.Lerp(_targetPosition, _bounceProgress);
                float bounceArc = Mathf.Sin(_bounceProgress * Mathf.Pi) * _bounceHeight;
                Position = new Vector2(currentPos.X, currentPos.Y - bounceArc);
            }
        }
    }

    private void StartBounceSet()
    {
        _bounceTimer.Stop();

        // Random number of bounces (1-5)
        int totalBounces = _random.Next(1, 6);
        _bouncesRemaining = totalBounces;

        // Height inversely proportional to number of bounces
        // Max height is 1/4 sprite height
        float spriteHeight = Texture.GetHeight() * Scale.Y;
        _bounceHeight = (spriteHeight * 0.25f) / totalBounces;

        // Horizontal distance proportional to bounce height
        _horizontalDistance = _bounceHeight * 0.8f;

        // Random direction
        _bouncingRight = _random.Next(0, 2) == 1;

        // Flip sprite to face direction (PNG faces left, so flip when going right)
        FlipH = _bouncingRight;

        _isBouncing = true;
        StartNextBounce();
    }

    private void StartNextBounce()
    {
        _startPosition = Position;
        float direction = _bouncingRight ? 1f : -1f;
        _targetPosition = new Vector2(Position.X + (_horizontalDistance * direction), Position.Y);
        _bounceProgress = 0f;
    }
}