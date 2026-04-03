extends Control

var next_wait_time := 0
var waited := 0
var open_details_pane : PanelContainer
var fps_update_interval := 0.5  # Update FPS every 0.5 seconds
var fps_update_timer := 0.0

func	 _ready():
	Globals.hud = self
	Globals.baseHpChanged.connect(update_hp)
	Globals.mineralsChanged.connect(update_minerals)
	Globals.waveStarted.connect(show_wave_count)
	Globals.waveCleared.connect(show_wave_timer)
	Globals.enemyDestroyed.connect(update_enemy_count)
	process_mode = Node.PROCESS_MODE_ALWAYS  # Ensure FPS updates even when paused

func update_hp(newHp, maxHp):
	%HPLabel.text = "HP: "+str(round(newHp))+"/"+str(round(maxHp))

func update_minerals(newMinerals):
	%GoldLabel.text = "Minerals: "+str(round(newMinerals))

func show_wave_count(current_wave, enemies):
	$WaveWaitTimer.stop()
	waited = 0
	%WaveLabel.text = "Wave " + str(current_wave) + "/" + str(Globals.currentMap.get_node("PathSpawner").max_waves)
	%RemainLabel.text = "Enemies: " + str(enemies)
	%RemainLabel.visible = true

func show_wave_timer(wait_time):
	%RemainLabel.visible = false
	next_wait_time = wait_time-1
	$WaveWaitTimer.start()

func _on_wave_wait_timer_timeout():
	%WaveLabel.text = "Wave " + str(Globals.currentMap.get_node("PathSpawner").current_wave) + "/" + str(Globals.currentMap.get_node("PathSpawner").max_waves) + " (Next in " + str(next_wait_time-waited) + ")"
	waited += 1

func update_enemy_count(remain):
	%RemainLabel.text = "Enemies: "+str(remain)

func reset():
	if is_instance_valid(open_details_pane):
		open_details_pane.turret.close_details_pane()

func _on_wave_started(wave_count, enemy_count):
	%WaveLabel.text = "Wave " + str(wave_count) + "/" + str(Data.TOTAL_WAVES)
	%RemainLabel.text = str(enemy_count) + " Enemies"

func _process(delta):
	fps_update_timer += delta
	if fps_update_timer >= fps_update_interval:
		fps_update_timer = 0
		%FPSLabel.text = "FPS: " + str(Engine.get_frames_per_second())
