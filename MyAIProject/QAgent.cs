using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyAIProject
{
    public class QAgent
    {
        // Q-learning parameters and Q-table
        public Dictionary<string, Dictionary<string, float>> QTable { get; private set; } = new Dictionary<string, Dictionary<string, float>>();
        public float Alpha { get; set; } = 0.1f;    // Learning rate
        public float Gamma { get; set; } = 0.9f;    // Discount factor
        public float Epsilon { get; set; } = 0.2f;  // Exploration rate
        public int Wave1ExplorationThreshold { get; set; } = 5;  // This basically means that the agent has explored every possible state in Wave 1

        // Discrete actions representing turret types
        public List<string> Actions { get; private set; } = new List<string> { "Buy Water", "Buy Fire", "Buy Earth", "Buy Wind", "Buy Basic", "Wait" };

        // Discretize the state into a key. In this example, we use wave buckets and a base HP category.
        public string GetStateKey(GameState state)
        {
            // Keep track of wave number
            int wave = state.CurrentWave;
            
            // Keep HP as a percentage
            int hpPercentage = (int)((float)state.BaseHP / state.MaxHP * 100);
            
            // Get turret counts
            int waterCount = state.TurretTypeCounts.ContainsKey("Buy Water") ? state.TurretTypeCounts["Buy Water"] : 0;
            int fireCount = state.TurretTypeCounts.ContainsKey("Buy Fire") ? state.TurretTypeCounts["Buy Fire"] : 0;
            int earthCount = state.TurretTypeCounts.ContainsKey("Buy Earth") ? state.TurretTypeCounts["Buy Earth"] : 0;
            int windCount = state.TurretTypeCounts.ContainsKey("Buy Wind") ? state.TurretTypeCounts["Buy Wind"] : 0;
            int basicCount = state.TurretTypeCounts.ContainsKey("Buy Basic") ? state.TurretTypeCounts["Buy Basic"] : 0;
            
            return $"Wave{wave}_HP{hpPercentage}_W{waterCount}F{fireCount}E{earthCount}Wi{windCount}B{basicCount}";
        }


        // Choose an action (turret type) using an epsilon-greedy strategy.
        public string ChooseAction(GameState state, Random random)
        {
            string stateKey = GetStateKey(state);
            
            // Check if we already have 20 turrets or we don't have 70 gold, force "Wait" action
            int totalTurrets = state.TurretTypeCounts.Values.Sum();
            if (totalTurrets >= 40 || state.CurrentMinerals < 70)
            {
                return "Wait";
            }

            // Special handling for Wave 1
            if (state.CurrentWave == 1)
            {
                return ChooseWave1Action(state, random);
            }

            // Only initialize Q-values if this is a new wave state
            if (!QTable.ContainsKey(stateKey))
            {
                // Don't initialize here anymore
                return Actions[random.Next(Actions.Count)];
            }

            if (random.NextDouble() < Epsilon)
            {
                // Explore: return a random action
                return Actions[random.Next(Actions.Count)];
            }
            else
            {
                // Exploit: choose action with highest Q-value
                var stateActions = QTable[stateKey];
                float maxQ = stateActions.Values.Max();
                var bestActions = stateActions
                                    .Where(pair => pair.Value == maxQ)
                                    .Select(pair => pair.Key)
                                    .ToList();

                return bestActions.Count > 0 
                    ? bestActions[random.Next(bestActions.Count)]
                    : Actions[random.Next(Actions.Count)];
            }
        }

        // Special action selection for Wave 1 to ensure proper exploration/exploitation balance
        private string ChooseWave1Action(GameState state, Random random)
        {
            string stateKey = GetStateKey(state);
            
            // Initialize QTable for this state if it doesn't exist
            if (!QTable.ContainsKey(stateKey))
            {
                QTable[stateKey] = new Dictionary<string, float>();
                foreach (string action in Actions)
                {
                    QTable[stateKey][action] = 0.0f;
                }
                GD.Print($"Initialized new Wave 1 state in QTable: {stateKey}");
            }
            
            // Count how many Wave 1 states we've seen
            int wave1States = QTable.Count(kvp => kvp.Key.StartsWith("Wave1_"));
            
            //GD.Print("Wave 1 states: ", wave1States);

            // If we haven't explored Wave 1 enough, explore more
            if (wave1States < Wave1ExplorationThreshold)
            {
                // Pure exploration when we have very few samples
                return Actions[random.Next(Actions.Count)];
            }
            
            

            // Analyze all Wave 1 states to find the best action overall
            Dictionary<string, float> actionValues = new Dictionary<string, float>();
            
            // Initialize all possible actions with zero values
            foreach (string action in Actions)
            {
                actionValues[action] = 0f;
            }
            
            // Count how many times each action appears in Wave 1 states
            Dictionary<string, int> actionCounts = new Dictionary<string, int>();
            foreach (string action in Actions)
            {
                actionCounts[action] = 0;
            }
            
            // Aggregate Q-values across all Wave 1 states
            foreach (var kvp in QTable)
            {
                if (kvp.Key.StartsWith("Wave1_"))
                {
                    foreach (var actionValue in kvp.Value)
                    {
                        actionValues[actionValue.Key] += actionValue.Value;
                        actionCounts[actionValue.Key]++;
                    }
                }
            }
            
            // Calculate average Q-value for each action
            Dictionary<string, float> avgActionValues = new Dictionary<string, float>();
            foreach (string action in Actions)
            {
                avgActionValues[action] = actionCounts[action] > 0 
                    ? actionValues[action] / actionCounts[action] 
                    : 0f;
            }
            
            // Determine if we should explore or exploit
            if (random.NextDouble() < Math.Max(0.1f, Epsilon - (wave1States / 100f)))
            {
                // Still some exploration, but decreasing as we see more Wave 1 states
                return Actions[random.Next(Actions.Count)];
            }
            else
            {
                // Find the best action based on average Q-values
                float maxAvgQ = avgActionValues.Values.Max();
                var bestActions = avgActionValues
                    .Where(pair => pair.Value == maxAvgQ)
                    .Select(pair => pair.Key)
                    .ToList();
                
                // If we have valid best actions with positive values, choose one
                if (bestActions.Count > 0 && maxAvgQ > 0)
                {
                    return bestActions[random.Next(bestActions.Count)];
                }
                else
                {
                    // If no good actions found, explore
                    return Actions[random.Next(Actions.Count)];
                }
            }
        }

        // Update the Q-table based on the transition from oldState to newState.
        public void UpdateQValue(GameState oldState, string action, float reward, GameState newState)
        {
            if (oldState == null || newState == null || action == null)
            {
                GD.PrintErr("UpdateQValue called with null parameters. Skipping Q update.");
                return;
            }
            
            string oldKey = GetStateKey(oldState);
            string newKey = GetStateKey(newState);

            // Find the existing entry for this wave
            var existingEntry = QTable.FirstOrDefault(kvp => kvp.Key.StartsWith($"Wave{oldState.CurrentWave}_"));
            if (existingEntry.Key == null)
            {
                GD.PrintErr($"No existing Q-table entry found for wave {oldState.CurrentWave}");
                return;
            }

            // Get the existing Q-values and apply the discount factor
            var qValues = existingEntry.Value.ToDictionary(kvp => kvp.Key, kvp => kvp.Value * Gamma);
            
            // Update the Q-value for the action taken
            float oldQ = qValues[action];
            float maxNextQ = qValues.Values.Max();  // Using current state's max Q as there is no "next" state
            float updatedQ = oldQ + Alpha * (reward + Gamma * maxNextQ - oldQ);
            qValues[action] = updatedQ;

            // Remove the old entry and add the updated one with the new key
            QTable.Remove(existingEntry.Key);
            QTable[newKey] = qValues;
        }

    }
}
