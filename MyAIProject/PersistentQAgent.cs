using Godot;
using System;
using MyAIProject;

public partial class PersistentQAgent : Node
{
    // Static instance for easy access
    public static PersistentQAgent Instance { get; private set; }
    
    // The persistent QAgent
    public QAgent QAgent { get; private set; }

    public override void _Ready()
    {        
        // Ensure only one instance exists
        if (Instance != null)
        {
            GD.Print("PersistentQAgent already exists, preserving Q-table");
            // Instead of destroying this instance, transfer the existing Q-table
            QAgent = Instance.QAgent;
            Instance = this;
            return;
        }
        
        Instance = this;
        // Only create a new QAgent if one doesn't exist
        QAgent = new QAgent();
        GD.Print("PersistentQAgent initialized with new Q-table");
    }

    // Optional: Add method to save Q-table state when the game exits
    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    public void save_q_table()
    {
        // Use project directory path
        string dirPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "QTables");
        
        // Create directory if it doesn't exist
        if (!System.IO.Directory.Exists(dirPath))
        {
            System.IO.Directory.CreateDirectory(dirPath);
        }

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filePath = System.IO.Path.Combine(dirPath, $"qtable_{timestamp}.json");
        
        GD.Print($"Attempting to save Q-table to: {filePath}");

        using var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Write);
        if (file != null)
        {
            var godotDict = new Godot.Collections.Dictionary();
            foreach (var kvp in QAgent.QTable)
            {
                var innerDict = new Godot.Collections.Dictionary();
                foreach (var innerKvp in kvp.Value)
                {
                    innerDict[innerKvp.Key] = innerKvp.Value;
                }
                godotDict[kvp.Key] = innerDict;
            }
            
            string json = Json.Stringify(godotDict, "  ");  // "  " adds pretty printing
            file.StoreString(json);
            GD.Print($"Q-table successfully saved to: {filePath}");
            GD.Print($"Q-table size: {QAgent.QTable.Count} states");
        }
        else
        {
            GD.PrintErr($"Failed to save Q-table to: {filePath}");
        }
    }
}

