using Godot;

public partial class GameLoggerSingleton : Node
{
    private static GameLoggerSingleton _instance;

    public static GameLoggerSingleton Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameLoggerSingleton();
            }
            return _instance;
        }
    }

    public override void _Ready()
    {
        if (_instance != null)
        {
            QueueFree();
            return;
        }
        _instance = this;
    }

    public void WriteAllLogs()
    {
        GameLogger.Instance.WriteAllLogs();
    }
} 