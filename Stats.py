import os
import re
import matplotlib.pyplot as plt

def atoi(text):
    return int(text) if text.isdigit() else text

def natural_keys(text):
    """
    Splits the text into a list of strings and integers.
    This will allow for natural sort ordering.
    """
    return [atoi(c) for c in re.split(r'(\d+)', text)]


def parse_log_file_turret_counts(filepath):
    """
    Parses a single log file to extract total turret build counts.
    Returns a dictionary of total turret counts like:
      { 'water': N, 'earth': N, 'fire': N, 'wind': N, 'basic': N }
    """
    turret_counts = {'water': 0, 'earth': 0, 'fire': 0, 'wind': 0, 'basic': 0}
    turret_count_pattern = re.compile(r"-\s+Buy\s+(\w+)\s+turrets:\s+(\d+)", re.IGNORECASE)

    with open(filepath, "r", encoding="utf-8") as f:
        for line in f:
            match = turret_count_pattern.search(line)
            if match:
                turret_type = match.group(1).lower()
                count = int(match.group(2))
                if turret_type in turret_counts:
                    turret_counts[turret_type] += count

    return turret_counts

def parse_log_file_furthest_wave(filepath):
    """
    Parses a single log file to find the furthest (highest) wave reached.
    It searches lines like 'Wave 3 Summary:'. Returns the max wave number found
    or 0 if none found.
    """
    wave_header_pattern = re.compile(r"Wave\s+(\d+)\s+Summary", re.IGNORECASE)
    furthest_wave = 0

    with open(filepath, "r", encoding="utf-8") as f:
        for line in f:
            match = wave_header_pattern.search(line)
            if match:
                wave_num = int(match.group(1))
                if wave_num > furthest_wave:
                    furthest_wave = wave_num

    return furthest_wave

def overall_turret_distribution(log_dir):
    """
    Aggregates turret builds from all .txt files in log_dir and produces:
      - A bar chart of the overall probability distribution of turret types.
      - A dictionary with overall turret build totals.
    """
    turret_types = ['water', 'earth', 'fire', 'wind', 'basic']
    total_counts = {t: 0 for t in turret_types}

    # Gather log files
    log_files = [f for f in os.listdir(log_dir) if f.endswith('.txt')]
    if not log_files:
        print(f"No .txt files found in {log_dir}")
        return None

    # Parse each file for turret counts
    for logfile in log_files:
        filepath = os.path.join(log_dir, logfile)
        file_counts = parse_log_file_turret_counts(filepath)
        for t in turret_types:
            total_counts[t] += file_counts[t]

    # Calculate probabilities
    grand_total = sum(total_counts.values())
    if grand_total == 0:
        print("No turrets found across all logs.")
        return None

    probabilities = {t: total_counts[t] / grand_total for t in turret_types}
    # Plot bar chart of probabilities
    plt.figure(figsize=(6, 4))
    plt.bar(probabilities.keys(), probabilities.values(),
            color=['blue','brown','red','green','gray'])
    plt.title("Overall Probability of Each Turret Type (All Logs Combined)")
    plt.xlabel("Turret Type")
    plt.ylabel("Probability")
    plt.ylim(0, 1)
    plt.grid(axis='y')
    plt.tight_layout()
    plt.show()

    # Also print them out
    print("Overall Turret Build Distribution:")
    for t in turret_types:
        print(f"{t.capitalize()}: {probabilities[t]*100:.1f}%")

    return total_counts

def plot_furthest_wave_per_game(log_dir):
    """
    Reads all .txt files in log_dir, determines the furthest wave reached
    in each file, and produces a bar chart showing wave # vs. filename.
    """
    log_files = [f for f in os.listdir(log_dir) if f.endswith('.txt')]
    log_files.sort(key=natural_keys)

    if not log_files:
        print(f"No .txt files found in {log_dir}")
        return

    furthest_waves = []
    labels = []
    for logfile in log_files:
        filepath = os.path.join(log_dir, logfile)
        max_wave = parse_log_file_furthest_wave(filepath)
        furthest_waves.append(max_wave)
        labels.append(logfile)

    # Plot bar chart
    plt.figure(figsize=(8, 4))
    x_positions = range(len(log_files))
    plt.bar(x_positions, furthest_waves, color='orange')
    plt.xticks(x_positions, labels, rotation=45, ha='right')
    plt.ylabel("Furthest Wave Reached")
    plt.title("Furthest Wave Reached Per Log File")
    plt.tight_layout()
    plt.show()

def main():
    log_dir = "MyTowerDefenseGame/GameLogs"

    # 1) Overall turret distribution
    overall_turret_distribution(log_dir)
    # 2) Furthest wave per game
    plot_furthest_wave_per_game(log_dir)

if __name__ == "__main__":
    main()
