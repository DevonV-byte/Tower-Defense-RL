extends Path2D
class_name EnemyPath

var map_type := "":
	set(val):
		map_type = val
		for config in Data.maps[val]["spawner_settings"].keys():
			set(config, Data.maps[val]["spawner_settings"][config])

var difficulty := {}
var spawnable_enemies := []
var max_waves := 3
var special_waves := {}
var wave_spawn_count := 10

var current_wave_spawn_count := 0
var current_difficulty := 1.0
var current_wave := 0
var enemies_spawned_this_wave := 0
var killed_this_wave := 0

func spawn_new_enemy():
	var enemyScene := preload("res://Scenes/enemies/enemy_mover.tscn")
	var enemy = enemyScene.instantiate()
	enemy.enemy_type = spawnable_enemies.pick_random()
	
	# Only scale HP from wave 2 onwards
	var hp_scale = 1.0
	if current_wave > 1:
		hp_scale = pow(1.07, current_wave - 1)  # 1.07
	enemy.hp_multiplier = hp_scale
	
	# Reset blueDino base HP to 20 after wave 11
	# if enemy.enemy_type == "blueDino" && current_wave >= 11:
		# enemy.hp = 20.0
	
	add_child(enemy)
	enemies_spawned_this_wave += 1

func get_spawnable_enemies():
	# Last 10 waves are normal dinos
	if current_wave > max_waves - 10:
		return ["normalDino"]
	
	# For other waves, alternate every 5 waves between colored dinos
	match ((current_wave - 1) / 5) % 4:
		0: return ["redDino"]    # Fire
		1: return ["blueDino"]   # Water
		2: return ["yellowDino"] # Wind
		3: return ["greenDino"]  # Earth

func get_current_difficulty() -> float:
	var default_diff = difficulty["initial"]
	var increase = difficulty["increase"]
	var calculated_diff = default_diff * pow(increase, current_wave) if difficulty["multiplies"] else default_diff + increase * current_wave
	return calculated_diff

func _on_spawn_delay_timeout():
	# Spawn multiple enemies if we're running at high speed
	var spawn_count = 1
	if Globals.time_scale > 1.0:
		spawn_count = int(ceil(Globals.time_scale))
	
	for i in range(spawn_count):
		if enemies_spawned_this_wave < current_wave_spawn_count:
			spawn_new_enemy()
	
	if enemies_spawned_this_wave < current_wave_spawn_count:
		$SpawnDelay.start()

func _on_wave_delay_timer_timeout():
	current_wave += 1
	Globals.currentWave = current_wave
	killed_this_wave = 0
	enemies_spawned_this_wave = 0
	current_difficulty = get_current_difficulty()
	current_wave_spawn_count = wave_spawn_count
	spawnable_enemies = get_spawnable_enemies()
	Globals.waveStarted.emit(current_wave, current_wave_spawn_count)
	$SpawnDelay.start()

func enemy_destroyed():
	killed_this_wave += 1
	Globals.enemyDestroyed.emit(current_wave_spawn_count - killed_this_wave)
	check_wave_clear()
	
func check_wave_clear():
	if killed_this_wave == current_wave_spawn_count:
		if not current_wave == max_waves:
			# Skip delay emission if in AI mode
			if Globals.aiMode:
				$WaveDelayTimer.wait_time = 0.1 / Globals.time_scale
			Globals.waveCleared.emit($WaveDelayTimer.wait_time)
			$WaveDelayTimer.start()
			return
		Globals.game_won.emit()
		
		# Show the map completed panel
		var mapCompletedScene := preload("res://Scenes/ui/mapCompleted/mapCompleted.tscn")
		var mapCompleted := mapCompletedScene.instantiate()
		Globals.hud.add_child(mapCompleted)
		
		# Automatically restart the level after a short delay
		Globals.restart_current_level()

func _ready():
	Globals.currentWave = 0
	# Use a fixed spawn delay that isn't affected by time scale
	$SpawnDelay.wait_time = 0.05
	# Set different delay for AI mode
	if Globals.aiMode:
		$WaveDelayTimer.wait_time = 0.01  # Minimal delay for AI mode
		# Also set the initial wait time since the timer autostarts
		$WaveDelayTimer.stop()  # Stop the autostarted timer
		$WaveDelayTimer.wait_time = 0.01  # Set minimal delay
		$WaveDelayTimer.start()  # Restart with new delay
	else:
		$WaveDelayTimer.wait_time = 5.0 / Globals.time_scale
