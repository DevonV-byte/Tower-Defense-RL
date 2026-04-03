using System;
using System.Collections.Generic;
using System.IO;
using Godot;

public class GameLogger
{
    private static GameLogger _instance;
    private Dictionary<int, Queue<string>> _gameLogQueues;
    private readonly object _lockObject = new object();

    public static GameLogger Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameLogger();
            }
            return _instance;
        }
    }

    private GameLogger()
    {
        _gameLogQueues = new Dictionary<int, Queue<string>>();
    }

    public void EnqueueLog(int gameNumber, string logEntry)
    {
        lock (_lockObject)
        {
            if (!_gameLogQueues.ContainsKey(gameNumber))
            {
                _gameLogQueues[gameNumber] = new Queue<string>();
            }
            _gameLogQueues[gameNumber].Enqueue(logEntry);
        }
    }

    public void WriteAllLogs()
    {
        lock (_lockObject)
        {
            try
            {
                string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "GameLogs");
                Directory.CreateDirectory(logDirectory);

                foreach (var gameKvp in _gameLogQueues)
                {
                    string logFilePath = Path.Combine(logDirectory, $"ai_log_game_{gameKvp.Key}.txt");
                    File.WriteAllLines(logFilePath, gameKvp.Value);
                }
                _gameLogQueues.Clear();
            }
            catch (Exception e)
            {
                GD.PrintErr($"Failed to write logs: {e.Message}");
            }
        }
    }
} 