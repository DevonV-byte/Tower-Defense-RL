using Godot;
using System;

public partial class StartButton : Button
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Optionally: Connect the signal programmatically
        this.Pressed += OnStartButtonPressed;
    }

    // Method connected to the button's pressed signal
    private void OnStartButtonPressed()
    {
        GD.Print("Start button pressed!");
    }
}
