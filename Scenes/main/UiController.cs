using Godot;

public partial class UiController : Node2D
{
    public override void _Ready()
    {
        var globals = GetNode("/root/Globals");
        globals.Set("mainNode", this);
        globals.Set("hud", GetNode<Control>("UI/HUD"));
        var container = GetNode<VBoxContainer>("UI/HUD/VBoxContainer");

        var currentAiMode = (bool)globals.Get("aiMode");

        if (currentAiMode)
        {
            // AI mode: fast physics, speed slider visible
            Engine.PhysicsTicksPerSecond = 600;
            Engine.MaxFps = 0; // uncapped

            var aiToggle = new CheckButton();
            aiToggle.Text = "AI Mode";
            aiToggle.ButtonPressed = true;
            aiToggle.SetPressedNoSignal(true);
            aiToggle.Toggled += OnAiToggle;
            container.AddChild(aiToggle);

            var speedControl = new HBoxContainer();
            var speedLabel = new Label { Text = "Game Speed: " };
            speedControl.AddChild(speedLabel);
            var speedSlider = new HSlider
            {
                MinValue = 0.1f,
                MaxValue = 10.0f,
                Value = 10.0f,
                CustomMinimumSize = new Vector2(100, 0),
                Step = 0.1f
            };
            speedSlider.ValueChanged += OnSpeedChanged;
            speedControl.AddChild(speedSlider);
            container.AddChild(speedControl);
        }
        else
        {
            // Human mode: normal physics at 60hz, capped at 60fps, normal time scale
            Engine.PhysicsTicksPerSecond = 60;
            Engine.MaxFps = 60;
            globals.Set("time_scale", 1.0f);
        }
        
        // Load and set up initial map
        LoadMap("map1");
    }

    private void LoadMap(string mapType)
    {
        var globals = GetNode("/root/Globals");
        globals.Set("selected_map", mapType);
        
        var dataCache = GD.Load<GDScript>("res://Scenes/main/Data.gd").New().As<GodotObject>();
        var mapData = dataCache.Get("maps").As<Godot.Collections.Dictionary>();
        var mapScene = GD.Load<PackedScene>(mapData[mapType].As<Godot.Collections.Dictionary>()["scene"].AsString());
        
        var map = mapScene.Instantiate<Node2D>();
        map.Set("map_type", mapType);
        AddChild(map);
    }

    private void OnAiToggle(bool buttonPressed)
    {
        var globals = GetNode("/root/Globals");
        globals.Set("aiMode", buttonPressed);
    }

    private void OnSpeedChanged(double value)
    {
        var globals = GetNode("/root/Globals");
        globals.Set("time_scale", value);
    }
} 