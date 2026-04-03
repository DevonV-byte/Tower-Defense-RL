import os
import glob
import re
import matplotlib.pyplot as plt

# Directory containing game log files (adjust as needed)
log_dir = './MyTowerDefenseGame/GameLogs/'
log_files = sorted(glob.glob(os.path.join(log_dir, '*.txt')))

# Split log files into first half and second half
n = len(log_files)
first_half_files = log_files[: n // 2]
second_half_files = log_files[n // 2 :]

def aggregate_logs(file_list):
    """
    Given a list of log files, aggregates turret counts by grouping waves into blocks of 5.
    Returns a dictionary mapping group index (0 for waves 1-5, 1 for waves 6-10, etc.)
    to a dictionary of turret counts.
    """
    agg_groups = {}
    wave_header_regex = re.compile(r'Wave\s+(\d+)\s+Summary:', re.IGNORECASE)
    turret_regex = re.compile(r'-\s+(.*?)\s+turrets:\s+(\d+)', re.IGNORECASE)

    for file_path in file_list:
        with open(file_path, 'r') as f:
            lines = f.readlines()
        
        current_wave = None
        current_wave_turrets = {}  # turret counts for the current wave
        
        for line in lines:
            line = line.strip()
            if not line:
                continue
            
            # Check for a wave header
            wave_match = wave_header_regex.match(line)
            if wave_match:
                # Commit previous wave's data if available
                if current_wave is not None:
                    group_index = (current_wave - 1) // 5
                    if group_index not in agg_groups:
                        agg_groups[group_index] = {}
                    for turret, count in current_wave_turrets.items():
                        agg_groups[group_index][turret] = agg_groups[group_index].get(turret, 0) + count
                # Start new wave
                current_wave = int(wave_match.group(1))
                current_wave_turrets = {}
                continue
            
            # Check for turret count lines within a wave summary
            turret_match = turret_regex.match(line)
            if turret_match and current_wave is not None:
                turret_type_raw = turret_match.group(1).strip().lower()
                # Remove the "buy " prefix if present
                if turret_type_raw.startswith("buy "):
                    turret_type = turret_type_raw[4:]
                else:
                    turret_type = turret_type_raw
                count = int(turret_match.group(2))
                current_wave_turrets[turret_type] = current_wave_turrets.get(turret_type, 0) + count
        
        # Commit the final wave's data from the file
        if current_wave is not None:
            group_index = (current_wave - 1) // 5
            if group_index not in agg_groups:
                agg_groups[group_index] = {}
            for turret, count in current_wave_turrets.items():
                agg_groups[group_index][turret] = agg_groups[group_index].get(turret, 0) + count

    return agg_groups

# Aggregate turret counts for each half.
agg_first = aggregate_logs(first_half_files)
agg_second = aggregate_logs(second_half_files)

# Determine the full set of turret types encountered across both halves (for consistent plotting)
all_turrets = set()
for group in list(agg_first.values()) + list(agg_second.values()):
    all_turrets.update(group.keys())
all_turrets = sorted(all_turrets)

# Define a fixed color mapping for turret types.
# Keys like "water", "fire", "earth", etc. will match after removing the "buy " prefix.
turret_color_map = {
    "water": "blue",
    "fire": "red",
    "earth": "green",
    "wind": "purple",
    "basic": "gray"
}

def plot_agg(agg_groups, title):
    """
    Plots turret distribution per 5-wave group using a 2x2 grid of subplots.
    """
    fig, axes = plt.subplots(2, 2, figsize=(12, 8))
    axes = axes.flatten()
    
    # We expect groups 0, 1, 2, and 3 (waves 1-5, 6-10, 11-15, 16-20)
    for group_index in range(4):
        ax = axes[group_index]
        if group_index in agg_groups:
            turret_counts = agg_groups[group_index]
            # Get count for each turret type (or 0 if missing), in consistent order
            counts = [turret_counts.get(t, 0) for t in all_turrets]
            colors = [turret_color_map.get(t, "skyblue") for t in all_turrets]
            ax.bar(all_turrets, counts, color=colors)
            ax.set_title(f'Waves {group_index*5+1} - {group_index*5+5}')
            ax.set_xlabel('Turret Type')
            ax.set_ylabel('Count')
        else:
            ax.set_visible(False)
    
    fig.suptitle(title)
    plt.tight_layout(rect=[0, 0.03, 1, 0.95])
    plt.show()

# Plot for the first half of log files.
plot_agg(agg_first, "First Half of Log Files: Turret Distribution per 5-Wave Group")

# Plot for the second half of log files.
plot_agg(agg_second, "Second Half of Log Files: Turret Distribution per 5-Wave Group")
