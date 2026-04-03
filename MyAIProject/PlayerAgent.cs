using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MyAIProject
{
    public class PlayerAgent
    {
        private int currentTurretPositionIndex = 0;
        private Random random = new Random();
        private readonly DateTime gameStartTime;
        private Dictionary<string, int> totalTurrets = new Dictionary<string, int>();
        private int currentWave = 0;
        private int gameNumber;

        // Expose turret counts to other classes.
        public Dictionary<string, int> TotalTurrets => totalTurrets;
        
        // QAgent instance for handling Q-learning
        public QAgent QAgent => PersistentQAgent.Instance.QAgent;

        public PlayerAgent(int gameNumber)
        {
            this.gameNumber = gameNumber;
            gameStartTime = DateTime.Now;
            GameLogger.Instance.EnqueueLog(gameNumber, $"Game started at: {gameStartTime}\n");
        }

        public string GetGreeting()
        {
            return "Hello from PlayerAgent!";
        }

        // Select turret placement position sequentially.
        public (string action, Vector2 position, string turretType) DecideAction(GameState state)
        {
            // Choose the turret type using the QAgent's epsilon-greedy policy.
            string turretType = QAgent.ChooseAction(state, random);

            // Select turret placement position sequentially.
            Vector2 selectedPosition = Vector2.Zero;
            if (state.ValidTurretPositions.Length > 0)
            {
                selectedPosition = state.ValidTurretPositions[currentTurretPositionIndex];
                currentTurretPositionIndex = (currentTurretPositionIndex + 1) % state.ValidTurretPositions.Length;
            }

            // Return the action, position, and the turret type decided by the QAgent.
            return ("PlaceTower", selectedPosition, turretType);
        }

        public void LogAction(string action, Dictionary<string, object> details)
        {
            if (details == null || details["type"] == null)
            {
                GD.PrintErr("LogAction: turret type is null in details.");
                return;
            }

            string turretType = details["type"]?.ToString();
            if (string.IsNullOrEmpty(turretType))
            {
                GD.PrintErr("LogAction: turret type is null or empty after conversion.");
                return;
            }
            
            // GD.Print("LogAction turret type: " + turretType);

            var gameTime = (DateTime.Now - gameStartTime).TotalSeconds;
            var logEntry = $"[{gameTime:F2}s] ";

            if (action == "PlaceTower")
            {
                logEntry += $"Built {turretType} turret at position {details["position"]}\n";
                GameLogger.Instance.EnqueueLog(gameNumber, logEntry);

                if (!totalTurrets.ContainsKey(turretType))
                    totalTurrets[turretType] = 0;
                totalTurrets[turretType]++;
            }
        }

        public void OnWaveStart(int waveNumber)
        {
            currentWave = waveNumber;
        }

        private void LogWaveSummary()
        {
            try
            {
                var summary = $"\nWave {currentWave} Summary:\n";
                foreach (var kvp in totalTurrets.OrderBy(x => x.Key))
                {
                    summary += $"- {kvp.Key} turrets: {kvp.Value}\n";
                }
                summary += "\n";
                GameLogger.Instance.EnqueueLog(gameNumber, summary);
            }
            catch (Exception e)
            {
                GD.PrintErr($"Failed to queue log entry: {e.Message}");
            }
        }

        public void OnWaveCleared()
        {
            LogWaveSummary();
        }

        public void OnGameEnd(bool isWin)
        {
            var summary = $"\nGame {gameNumber} {(isWin ? "Won" : "Lost")} at Wave {currentWave}\n";
            summary += "Final Turret Summary:\n";
            foreach (var kvp in totalTurrets.OrderBy(x => x.Key))
            {
                summary += $"- {kvp.Key} turrets: {kvp.Value}\n";
            }
            summary += "\n";
            GameLogger.Instance.EnqueueLog(gameNumber, summary);
        }
    }

    public enum GameEvent
    {
        WinGame,
        LoseGame,
        EnemyLeak,
        KillEnemy,
        BuildTower
    }
}
