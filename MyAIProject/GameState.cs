using Godot;
using System.Collections.Generic;
using System.Linq;

namespace MyAIProject
{
    public class TurretInfo
    {
        public string Type { get; set; } = string.Empty;
        public Vector2 Position { get; set; }
        public int Level { get; set; }
        public float Damage { get; set; }
        public float AttackSpeed { get; set; }
        public float AttackRange { get; set; }
        public int UpgradeCost { get; set; }
        public int MaxLevel { get; set; }
    }

    public class GameState
    {
        public bool EnoughMoney { get; set; }
        public int CurrentMinerals { get; set; }
        public Vector2[] ValidTurretPositions { get; set; } = Array.Empty<Vector2>();
        public List<Vector2> OurTurretPositions { get; set; } = new List<Vector2>();
        public List<TurretInfo> Turrets { get; set; } = new List<TurretInfo>();
        public bool HasUpgradeableTurrets => Turrets.Any(t => t.Level < t.MaxLevel && CurrentMinerals >= t.UpgradeCost);
        
        // Add these new properties
        public int CurrentWave { get; set; }
        public int BaseHP { get; set; }
        public int MaxHP { get; set; }
        public Dictionary<string, int> TurretTypeCounts { get; set; } = new Dictionary<string, int>();
    }
}
