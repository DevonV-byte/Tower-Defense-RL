using Godot;
using MyAIProject;

public partial class TestAgent : Node
{
    public override void _Ready()
    {
        base._Ready();

        var player = new PlayerAgent(1); // Pass game number 1 for testing
        GD.Print(player.GetGreeting());
        // This should print: "Hello from PlayerAgent!"
    }
}
