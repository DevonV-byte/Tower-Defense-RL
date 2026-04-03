using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using MyAIProject;

public partial class AiManager : Node2D
{
    private PlayerAgent _agent;
    private List<Vector2> placedTurretPositions = new List<Vector2>();
    private Node2D turretsNode;
    private GodotObject dataCache;
    private Godot.Collections.Dictionary turretData;
    private List<string> turretTypes = new List<string>();
    private Random random = new Random();
    private double actionTimer = 0;
    private const double ACTION_DELAY = 0.5;
    private GameState waveStartState;
    private List<string> actionsThisWave = new List<string>();

    // Add these new fields to track bonus counts
    private Dictionary<string, int> turretTypeBonusCount = new Dictionary<string, int>
    {
        { "water", 0 },
        { "earth", 0 },
        { "wind", 0 },
        { "fire", 0 }
    };

    // Add this new field with the hardcoded positions
    private readonly Vector2[] hardcodedPositions = new Vector2[] {
        // Original positions (first 15)
        new Vector2(-33, -100), new Vector2(-33, 5), new Vector2(-33, 40),
        new Vector2(-33, 75), new Vector2(-33, 110), new Vector2(2, -100),
        new Vector2(2, 5), new Vector2(2, 40), new Vector2(2, 75), new Vector2(2, 110),
        new Vector2(-68, -275), new Vector2(-68, -240), new Vector2(-68, -205),
        new Vector2(-68, -170), new Vector2(-68, -135),
        
        // Original positions (next 5)
        new Vector2(37, -100), new Vector2(37, 5), new Vector2(37, 40), 
        new Vector2(37, 75), new Vector2(37, 110),
        
        // New positions (first 10 unique positions)
        new Vector2(72, -100), new Vector2(72, 5), new Vector2(72, 40),
        new Vector2(72, 75), new Vector2(72, 110), new Vector2(-103, -275),
        new Vector2(-103, -240), new Vector2(-103, -205), new Vector2(-103, -170), new Vector2(-103, -135),
        
        // Additional 10 unique positions
        new Vector2(107, -100), new Vector2(107, 5), new Vector2(107, 40),
        new Vector2(107, 75), new Vector2(107, 110), new Vector2(-103, -100),
        new Vector2(-103, 5), new Vector2(-103, 40), new Vector2(-103, 75), new Vector2(-103, 110),
        
        // Final 10 unique positions
        new Vector2(142, -100), new Vector2(142, 5), new Vector2(142, 40),
        new Vector2(142, 75), new Vector2(142, 110), new Vector2(-138, -275),
        new Vector2(-138, -240), new Vector2(-138, -205), new Vector2(-138, -170), new Vector2(-138, -135)
    };

    // Add this new field to track the next position index
    private int nextPositionIndex = 0;

    public override void _Ready()
    {
        var globals = GetNode("/root/Globals");
        int currentGame = (int)globals.Get("games_completed") + 1;
        _agent = new PlayerAgent(currentGame);
        dataCache = GD.Load<GDScript>("res://Scenes/main/Data.gd").New().As<GodotObject>();
        turretData = dataCache.Get("turrets").As<Godot.Collections.Dictionary>();
        
        var globalsNode = GetNode("/root/Globals");
        globalsNode.Call("set_game_speed", 0.5);  // Start at 50% speed
        
        foreach (var key in turretData.Keys)
        {
            turretTypes.Add(key.AsString());
        }
        
        turretsNode = new Node2D();
        turretsNode.Name = "Turrets";
        GetParent().CallDeferred("add_child", turretsNode);

        // Connect to global signals
        globalsNode.Connect("baseHpChanged", new Callable(this, nameof(OnBaseHpChanged)));
        globalsNode.Connect("waveStarted", new Callable(this, nameof(OnWaveStarted)));
        globalsNode.Connect("waveCleared", new Callable(this, nameof(OnWaveCleared)));
        globalsNode.Connect("enemyDestroyed", new Callable(this, nameof(OnEnemyDestroyed)));
        globalsNode.Connect("enemyLeak", new Callable(this, nameof(OnEnemyLeak)));
        globalsNode.Connect("game_won", Callable.From((Action)(() => OnGameEnd(true))));
        globalsNode.Connect("game_lost", Callable.From((Action)(() => OnGameEnd(false))));
    }

    public override void _Process(double delta)
    {        
        var globals = GetNode("/root/Globals");
        if (globals == null)
        {
            GD.Print("Early exit: globals is null");
            return;
        }
        
        if (!(bool)globals.Get("aiMode"))
        {
            return;
        }
        
        if (turretTypes.Count == 0)
        {
            GD.Print("Early exit: turretTypes.Count == 0");
            return;
        }
        
        var turretManager = GetNode<Node2D>("../TurretPlacementManager");
        if (turretManager == null)
        {
            GD.Print("Early exit: turretManager is null");
            return;
        }
        
        // Get valid positions (here, we ignore these in favor of hardcoded ones)
        var validPositions = turretManager.Call("get_valid_positions").AsGodotArray<Vector2>();
        
        int currentWave = (int)globals.Get("currentWave");
        int baseHP = (int)globals.Get("baseHP");
        int maxHP = (int)globals.Get("maxHP");
        
        var currentMapVariant = globals.Get("currentMap");
        if (currentMapVariant.Equals(default(Variant)))
        {
            GD.Print("Early exit: currentMapVariant equals default Variant");
            return;
        }
        
        var currentMap = currentMapVariant.As<Node>();
        if (currentMap == null)
        {
            GD.Print("Early exit: currentMap is null");
            return;
        }
        
        int currentMinerals = (int)currentMap.Get("minerals");

        // Build the current state.
        var state = new GameState {
            CurrentMinerals = currentMinerals,
            ValidTurretPositions = hardcodedPositions,
            OurTurretPositions = placedTurretPositions,
            CurrentWave = currentWave,
            BaseHP = baseHP,
            MaxHP = maxHP,
            EnoughMoney = true,  // Will be updated below.
            TurretTypeCounts = new Dictionary<string, int>(_agent.TotalTurrets) // Copy current turret counts.
        };
        
        // Choose turret type using QAgent.
        var selectedTurretType = _agent.QAgent.ChooseAction(state, random);
        
        // Handle "Wait" action specially
        if (selectedTurretType == "Wait")
        {
            // Don't add "Wait" to actionsThisWave
            // Just return and do nothing this frame
            return;
        }
        
        // Compute cost for the selected turret.
        string selectedKey = selectedTurretType.Replace("Buy ", "").Trim().ToLower();
        object foundKeyObject = null;
        foreach (var keyObj in turretData.Keys)
        {
            if (keyObj.ToString().ToLower() == selectedKey)
            {
                foundKeyObject = keyObj;
                break;
            }
        }

        if (foundKeyObject == null)
        {
            GD.PrintErr($"Turret key '{selectedKey}' not found in turretData!");
            return;
        }

        string foundKeyStr = foundKeyObject.ToString();
        if (foundKeyStr == null)
        {
            GD.PrintErr($"Turret key '{foundKeyObject}' is not a string!");
            return;
        }

        var turretDictForSelected = turretData[(Godot.Variant)foundKeyStr].As<Godot.Collections.Dictionary>();
        int selectedTurretCost = turretDictForSelected["cost"].As<int>();
        state.EnoughMoney = (currentMinerals >= selectedTurretCost);
        
        if (!state.EnoughMoney)
        {
            return;
        }        
        
        if (hardcodedPositions.Length > 0)
        {
            var (action, position, turretType) = _agent.DecideAction(state);
            if (action == "PlaceTower")
            {
                bool isValid = (bool)turretManager.Call("is_valid_position", position);
                if (!isValid)
                {
                    return;
                }
                
                // Execute the turret placement.
                ExecuteAction(action, position, state, selectedTurretType);
                actionTimer = 0;
                
                int newMinerals = (int)currentMap.Get("minerals");
                // Build the new state after turret placement.
                GameState newState = new GameState {
                    CurrentMinerals = newMinerals,
                    ValidTurretPositions = hardcodedPositions,
                    OurTurretPositions = new List<Vector2>(placedTurretPositions),
                    CurrentWave = currentWave,
                    BaseHP = baseHP,
                    MaxHP = maxHP,
                    EnoughMoney = newMinerals >= selectedTurretCost,
                    TurretTypeCounts = new Dictionary<string, int>(_agent.TotalTurrets)
                };
            }
        }
    }

    private void ExecuteAction(string action, Vector2 position, GameState state, string turretType)
    {
        // Skip adding "Wait" actions to actionsThisWave
        if (turretType == "Wait")
        {
            return;
        }
        
        // Store the action for wave-end evaluation
        actionsThisWave.Add(turretType);
        
        // GD.Print("[ExecuteAction] turretType: " + turretType);
        
        if (action != "PlaceTower" || turretsNode == null)
        {
            GD.Print("ExecuteAction early exit: action not PlaceTower or turretsNode null");
            return;
        }

        // Process turret key.
        string turretKey = turretType.Replace("Buy ", "").Trim().ToLower();
        // GD.Print($"[ExecuteAction] Looking for turret key: {turretKey}");

        object foundKeyObject = null;
        foreach (var keyObj in turretData.Keys)
        {
            string keyStr = keyObj.ToString().ToLower();
            // GD.Print($"[ExecuteAction] Available turretData key: {keyStr}");
            if (keyStr == turretKey)
            {
                foundKeyObject = keyObj;
                break;
            }
        }

        if (foundKeyObject == null)
        {
            GD.PrintErr($"[ExecuteAction] Turret key '{turretKey}' not found in turretData!");
            return;
        }

        string foundKeyStr = foundKeyObject.ToString();
        if (foundKeyStr == null)
        {
            GD.PrintErr($"[ExecuteAction] Turret key '{foundKeyObject}' is not a string!");
            return;
        }

        // Retrieve the turret dictionary using the found key.
        var turretDict = turretData[(Godot.Variant)foundKeyStr].As<Godot.Collections.Dictionary>();
        int turretCost = turretDict["cost"].As<int>();

        // Debug prints for minerals and turret cost.
        // GD.Print($"[ExecuteAction] CurrentMinerals: {state.CurrentMinerals}, TurretCost: {turretCost}");

        // Instead of using the position from the agent, use the next available hardcoded position
        if (nextPositionIndex >= hardcodedPositions.Length)
        {
            GD.Print("[ExecuteAction] No more hardcoded positions available");
            return;
        }
        
        // Get the next position from our hardcoded list
        Vector2 nextPosition = hardcodedPositions[nextPositionIndex];

        // Check if the placement position is valid.
        bool isValid = (bool)GetNode<Node2D>("../TurretPlacementManager").Call("is_valid_position", nextPosition);
        // GD.Print($"[ExecuteAction] Position {nextPosition} valid? {isValid}");
        if (!isValid)
        {
            // If this position is invalid, try the next one
            nextPositionIndex++;
            GD.Print($"[ExecuteAction] Position {nextPosition} is not valid. Trying next position.");
            return;
        }

        if (state.CurrentMinerals < turretCost)
        {
            //GD.Print($"[ExecuteAction] Not enough minerals to build turret '{turretType}'.");
            return;
        }

        // At this point, we expect to build the turret.
        var placementDetails = new Dictionary<string, object>
        {
            { "type", turretType },
            { "position", nextPosition },
            { "cost", turretCost }
        };
        _agent.LogAction("PlaceTower", placementDetails);

        string scenePath = turretDict["scene"].As<string>();
        // GD.Print($"[ExecuteAction] Loading turret scene from: {scenePath}");
        var turretScene = GD.Load<PackedScene>(scenePath);
        if (turretScene == null)
        {
            GD.PrintErr("[ExecuteAction] Failed to load turret scene!");
            return;
        }

        // Update minerals on currentMap.
        var currentMap = GetNode("/root/Globals").Get("currentMap").As<Node>();
        int newMinerals = state.CurrentMinerals - turretCost;
        currentMap.Set("minerals", newMinerals);
        // GD.Print($"[ExecuteAction] Deducted minerals: new value = {newMinerals}");

        // Instantiate and add the turret.
        var turret = turretScene.Instantiate<Node2D>();
        turret.Position = nextPosition;
        // Pass the cleaned turret key.
        turret.Call("set", "turret_type", turretKey);

        turretsNode.AddChild(turret);
        turret.Call("build");
        placedTurretPositions.Add(nextPosition);
        
        // Increment the position index for next time
        nextPositionIndex++;

        // GD.Print($"[ExecuteAction] Turret '{turretType}' built at {nextPosition}");
    }


    private void OnBaseHpChanged(int newHp, int maxHp)
    {
        var currentState = BuildCurrentState();
        if (currentState == null)
        {
            GD.PrintErr("OnBaseHpChanged: Failed to build current state");
            return;
        }

        // If HP drops to 0, we'll handle this in the wave completion reward
        // No Q-table updates here anymore
    }

    // Helper method to build current state
    private GameState BuildCurrentState()
    {
        var globals = GetNode("/root/Globals");
        if (globals == null)
        {
            GD.PrintErr("BuildCurrentState: globals is null");
            return null;
        }

        // Get minerals directly from Globals instead of currentMap
        return new GameState
        {
            CurrentMinerals = (int)globals.Get("minerals"),
            ValidTurretPositions = hardcodedPositions,
            OurTurretPositions = new List<Vector2>(placedTurretPositions),
            CurrentWave = (int)globals.Get("currentWave"),
            BaseHP = (int)globals.Get("baseHP"),
            MaxHP = (int)globals.Get("maxHP"),
            TurretTypeCounts = new Dictionary<string, int>(_agent.TotalTurrets)
        };
    }

    private void OnWaveStarted(int waveCount, int enemyCount)
    {
        waveStartState = BuildCurrentState();
        actionsThisWave.Clear();
        
        string stateKey = _agent.QAgent.GetStateKey(waveStartState);
        
        // If this is wave 1, initialize with zeros
        if (waveCount == 1)
        {
            if (!_agent.QAgent.QTable.ContainsKey(stateKey))
            {
                _agent.QAgent.QTable[stateKey] = new Dictionary<string, float>();
                foreach (var action in _agent.QAgent.Actions.Where(a => a != "Wait"))
                {
                    _agent.QAgent.QTable[stateKey][action] = 0.0f;
                }
            }
        }
        else
        {
            // Find the previous wave's entry
            var previousEntry = _agent.QAgent.QTable.FirstOrDefault(kvp => kvp.Key.StartsWith($"Wave{waveCount-1}_"));
            if (previousEntry.Key != null)
            {
                // Copy Q-values from previous wave, excluding "Wait"
                _agent.QAgent.QTable[stateKey] = new Dictionary<string, float>();
                foreach (var pair in previousEntry.Value.Where(p => p.Key != "Wait"))
                {
                    _agent.QAgent.QTable[stateKey][pair.Key] = pair.Value;
                }
            }
            else
            {
                GD.PrintErr($"No Q-values found for previous wave {waveCount-1}");
            }
        }
        
        _agent.OnWaveStart(waveCount);
    }


    private void OnWaveCleared(double waitTime)
    {
        if (waveStartState != null)
        {
            var currentState = BuildCurrentState();
            float waveReward = ComputeWaveReward(currentState);
            
            string stateKey = _agent.QAgent.GetStateKey(waveStartState);
            
            // If we have actions this wave, update their Q-values
            if (actionsThisWave.Count > 0)
            {
                // Update Q-values for all actions taken during this wave
                foreach (var action in actionsThisWave)
                {
                    _agent.QAgent.UpdateQValue(waveStartState, action, waveReward, currentState);
                }
            }
            else if (waveStartState.OurTurretPositions.Count >= 40 && _agent.QAgent.QTable.ContainsKey(stateKey))
            {
                // We had 40 turrets and couldn't build more
                // Get the turret types from the agent's total turrets
                var turretTypes = _agent.TotalTurrets.Keys.Where(k => _agent.TotalTurrets[k] > 0)
                                                        .ToList();
                // Print out how many turrets we have per type
                foreach (var turretType in turretTypes)
                {
                    //GD.Print($"[OnWaveCleared] Turret type: {turretType}, count: {_agent.TotalTurrets[turretType]}");
                }
                
                // Only update Q-values for turret types we actually have
                foreach (var turretType in turretTypes)
                {
                    // Make sure both the state key and the action exist in the Q-table
                    if (_agent.QAgent.QTable.ContainsKey(stateKey) && _agent.QAgent.QTable[stateKey].ContainsKey(turretType))
                    {
                        _agent.QAgent.UpdateQValue(waveStartState, turretType, waveReward, currentState);
                    }
                }
            }
            
            // Set the current state as the start state for the next wave
            waveStartState = currentState;
        }
        
        _agent.OnWaveCleared();
    }

    private float ComputeWaveReward(GameState state)
    {
        // Base reward for surviving the wave
        float reward = 10.0f;
        
        // Add a negative reward for HP loss since the wave started
        if (waveStartState != null)
        {
            int hpLost = waveStartState.BaseHP - state.BaseHP;
            if (hpLost > 0)
            {
                // Penalize HP loss - adjust the multiplier as needed
                reward -= hpLost * 2.0f;
            }
        }
        
        // Wave progression bonus
        reward += state.CurrentWave * 10.0f;  // Increased weight on wave progression
        
        // Reward for having a diverse set of turrets
        int uniqueTurretTypes = state.TurretTypeCounts.Count(kvp => kvp.Value > 0);
        reward += uniqueTurretTypes * 2.0f;
        
        // Debug: Print all actions taken this wave with wave number, if wave number is higher than 5
        // GD.Print($"Wave {state.CurrentWave}: Actions this wave: {string.Join(", ", actionsThisWave)}");

        // Special rewards for building specific turret types during specific wave ranges
        if (actionsThisWave.Count > 0)
        {
            // First 5 waves: reward for building water turrets (max 2 times)
            if (state.CurrentWave <= 5 && 
                actionsThisWave.Any(a => a.ToLower().Contains("water")) && 
                turretTypeBonusCount["water"] < 2)
            {
                reward += 25.0f;
                turretTypeBonusCount["water"]++;
                GD.Print($"Added bonus reward for building Water turret in first 5 waves! (Bonus count: {turretTypeBonusCount["water"]}/2)");
            }
            
            // Second 5 waves: reward for building earth turrets (max 2 times)
            if (state.CurrentWave > 5 && state.CurrentWave <= 10 && 
                actionsThisWave.Any(a => a.ToLower().Contains("earth")) && 
                turretTypeBonusCount["earth"] < 2)
            {
                reward += 25.0f;
                turretTypeBonusCount["earth"]++;
                GD.Print($"Added bonus reward for building Earth turret in second 5 waves! (Bonus count: {turretTypeBonusCount["earth"]}/2)");
            }
            
            // Third 5 waves: reward for building fire turrets (max 2 times)
            if (state.CurrentWave > 10 && state.CurrentWave <= 15 && 
                actionsThisWave.Any(a => a.ToLower().Contains("fire")) && 
                turretTypeBonusCount["fire"] < 2)
            {
                reward += 25.0f;
                turretTypeBonusCount["fire"]++;
                GD.Print($"Added bonus reward for building Fire turret in third 5 waves! (Bonus count: {turretTypeBonusCount["fire"]}/2)");
            }
            
            // Fourth 5 waves: reward for building wind turrets (max 2 times)
            if (state.CurrentWave > 15 && state.CurrentWave <= 20 && 
                actionsThisWave.Any(a => a.ToLower().Contains("wind")) && 
                turretTypeBonusCount["wind"] < 2)
            {
                reward += 25.0f;
                turretTypeBonusCount["wind"]++;
                GD.Print($"Added bonus reward for building Wind turret in fourth 5 waves! (Bonus count: {turretTypeBonusCount["wind"]}/2)");
            }
        }
        
        return reward;
    }

    private void OnEnemyDestroyed(int remainingEnemies)
    {
        // No Q-table updates here
    }

    public void OnEnemyLeak()
    {
        // No Q-table updates here
    }

    public void OnGameWin()
    {
        // No Q-table updates here
    }

    public void OnGameEnd(bool isWin)
    {
        if (waveStartState != null)
        {
            var currentState = BuildCurrentState();
            float finalReward;

            if (isWin)
            {
                // Big reward for winning
                finalReward = 100.0f + (currentState.BaseHP / currentState.MaxHP * 50.0f);
                GD.Print($"Game won! Applying positive reward: {finalReward}");
            }
            else
            {
                // Stronger negative reward for losing
                finalReward = -100.0f + (currentState.CurrentWave * 5.0f);
                GD.Print($"Game lost! Applying negative reward: {finalReward}");
            }

            // Special rewards for building specific turret types during specific wave ranges
            if (actionsThisWave.Count > 0)
            {
                // First 5 waves: reward for building water turrets (max 2 times)
                if (currentState.CurrentWave <= 5 && 
                    actionsThisWave.Any(a => a.ToLower().Contains("water")) && 
                    turretTypeBonusCount["water"] < 2)
                {
                    finalReward += 25.0f;
                    turretTypeBonusCount["water"]++;
                    GD.Print($"Added final bonus reward for building Water turret in first 5 waves! (Bonus count: {turretTypeBonusCount["water"]}/2)");
                }
                
                // Second 5 waves: reward for building earth turrets (max 2 times)
                if (currentState.CurrentWave > 5 && currentState.CurrentWave <= 10 && 
                    actionsThisWave.Any(a => a.ToLower().Contains("earth")) && 
                    turretTypeBonusCount["earth"] < 2)
                {
                    finalReward += 25.0f;
                    turretTypeBonusCount["earth"]++;
                    GD.Print($"Added final bonus reward for building Earth turret in second 5 waves! (Bonus count: {turretTypeBonusCount["earth"]}/2)");
                }
                
                // Third 5 waves: reward for building wind turrets (max 2 times)
                if (currentState.CurrentWave > 10 && currentState.CurrentWave <= 15 && 
                    actionsThisWave.Any(a => a.ToLower().Contains("wind")) && 
                    turretTypeBonusCount["wind"] < 2)
                {
                    finalReward += 25.0f;
                    turretTypeBonusCount["wind"]++;
                    GD.Print($"Added final bonus reward for building Wind turret in third 5 waves! (Bonus count: {turretTypeBonusCount["wind"]}/2)");
                }
                
                // Fourth 5 waves: reward for building fire turrets (max 2 times)
                if (currentState.CurrentWave > 15 && currentState.CurrentWave <= 20 && 
                    actionsThisWave.Any(a => a.ToLower().Contains("fire")) && 
                    turretTypeBonusCount["fire"] < 2)
                {
                    finalReward += 25.0f;
                    turretTypeBonusCount["fire"]++;
                    GD.Print($"Added final bonus reward for building Fire turret in fourth 5 waves! (Bonus count: {turretTypeBonusCount["fire"]}/2)");
                }
            }

            string stateKey = _agent.QAgent.GetStateKey(waveStartState);
            GD.Print($"Final state key: {stateKey}");
            
            // If we have actions this wave, update their Q-values
            if (actionsThisWave.Count > 0)
            {
                // Update Q-values for all actions taken during the final wave
                foreach (var action in actionsThisWave)
                {
                    _agent.QAgent.UpdateQValue(waveStartState, action, finalReward, currentState);
                }
            }
            else if (waveStartState.OurTurretPositions.Count >= 40 && _agent.QAgent.QTable.ContainsKey(stateKey))
            {
                // We had 40 turrets and couldn't build more
                // Get the turret types from the agent's total turrets
                var turretTypes = _agent.TotalTurrets.Keys.Where(k => _agent.TotalTurrets[k] > 0)
                                                        .ToList();
                
                // Only update Q-values for turret types we actually have
                foreach (var turretType in turretTypes)
                {
                    // Make sure both the state key and the action exist in the Q-table
                    if (_agent.QAgent.QTable.ContainsKey(stateKey) && _agent.QAgent.QTable[stateKey].ContainsKey(turretType))
                    {
                        _agent.QAgent.UpdateQValue(waveStartState, turretType, finalReward, currentState);
                    }
                }
            }
        }

        _agent.OnGameEnd(isWin);
        
        // Reset the bonus counters for the next game
        turretTypeBonusCount["water"] = 0;
        turretTypeBonusCount["earth"] = 0;
        turretTypeBonusCount["wind"] = 0;
        turretTypeBonusCount["fire"] = 0;
        
        // Reset the position index for the next game
        nextPositionIndex = 0;
    }

}
