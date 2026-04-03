extends Node
@warning_ignore("unused_signal")
signal mineralsChanged(newMinerals)
@warning_ignore("unused_signal")
signal baseHpChanged(newHp, maxHp)
@warning_ignore("unused_signal")
signal waveStarted(wave_count, enemy_count)
@warning_ignore("unused_signal")
signal waveCleared(wait_time)
@warning_ignore("unused_signal")
signal enemyDestroyed(remain)
@warning_ignore("unused_signal")
signal enemyLeak
@warning_ignore("unused_signal")
signal game_won
@warning_ignore("unused_signal")
signal game_lost

var currentWave := 0
var baseHP := 0
var maxHP := 0
var minerals := 0
var selected_map := ""
var mainNode : Node2D
var turretsNode : Node2D
var projectilesNode : Node2D
var currentMap : Node2D
var hud : Control
var aiMode = false  # False = player mode, True = AI mode
var time_scale := 0.5:
	set(value):
		time_scale = value
		Engine.time_scale = value
var total_games_target := 25 # Default number of games to run
var games_completed := 0
var game_start_time := 0.0
var total_start_time := 0.0

func _ready():
	RenderingServer.render_loop_enabled = not aiMode

# Add this function to announce the current game
func announce_game_number():
	if aiMode:
		print("Now playing Game ", games_completed + 1, " of ", total_games_target)
		game_start_time = Time.get_ticks_msec()

func restart_current_level():
	# Defer the map deletion to the next frame
	currentMap.call_deferred("queue_free")
	
	if aiMode:  # Only track games in AI mode
		var game_duration = (Time.get_ticks_msec() - game_start_time) / 1000.0  # Convert to seconds
		print("Game ", games_completed + 1, " completed in ", "%.2f" % game_duration, " seconds")
		
		games_completed += 1
		
		# Calculate and print timing information for all games completed so far
		var current_total_duration = (Time.get_ticks_msec() - total_start_time) / 1000.0
		var current_avg_time = current_total_duration / games_completed
		print("Total time so far: ", "%.2f" % current_total_duration, " seconds")
		print("Average time per game so far: ", "%.2f" % current_avg_time, " seconds")
		
		if games_completed >= total_games_target:
			print("Completed all ", total_games_target, " games!")
			# Write all logs before quitting
			var logger = get_node("/root/GameLoggerSingleton")
			logger.WriteAllLogs()
			# Save Q-table before quitting
			var q_agent = get_node("/root/PersistentQAgent")
			q_agent.save_q_table()
			
			# Let's go back to the original method, which should work
			get_tree().quit()
			return
		else:
			# Announce the next game that's about to start
			announce_game_number()
	
	# Defer the scene change to the next frame
	get_tree().call_deferred("change_scene_to_file", "res://Scenes/main/main.tscn")

func set_game_speed(speed: float):
	time_scale = speed

func set_total_games(count: int) -> void:
	if count > 0:
		total_games_target = count
		games_completed = 0
		total_start_time = Time.get_ticks_msec()
