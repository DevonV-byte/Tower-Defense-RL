extends PathFollow2D

var hp_multiplier := 1.0  # Default to no scaling
var enemy_type := "":
	set(val):
		enemy_type = val
		$Sprite2D.texture = load(Data.enemies[val]["sprite"])
		# First set base stats
		for stat in Data.enemies[val]["stats"].keys():
			set(stat, Data.enemies[val]["stats"][stat])
		type = Data.enemies[val]["type"]
		# Apply scale if defined
		if "scale" in Data.enemies[val]:
			$Sprite2D.scale = Vector2(Data.enemies[val]["scale"], Data.enemies[val]["scale"])

var type := ""  # Store the enemy's elemental type
enum State {walking, damaged}
var state = State.walking
var mineralsYield := 10.0
var hp := 10.0
var baseDamage := 5.0
var speed := 1.0
var is_destroyed := false

# Define type resistances and weaknesses
const type_resistances = {
	"fire": "wind",    # Fire resists wind
	"wind": "earth",   # Wind resists earth
	"earth": "water",  # Earth resists water
	"water": "fire"    # Water resists fire
}

const type_weaknesses = {
	"fire": "water",   # Fire is weak to water
	"water": "earth",  # Water is weak to earth
	"earth": "wind",   # Earth is weak to wind
	"wind": "fire"    # Wind is weak to fire
}

# Add after the existing variables
var burning := false
var burn_damage := 0.0
var burn_duration := 0.0
var burn_tick := 0.0

var slowed := false
var slow_amount := 0.0
var slow_duration := 0.0

var wind_stacks := 0  # Each stack increases damage taken by 2%
var base_speed: float
var wave_number: int

@onready var spawner := get_parent() as EnemyPath
func _ready():
	add_to_group("enemy")
	base_speed = speed  # Store the initial speed
	wave_number = get_parent().current_wave # Get current wave from spawner
	
	# Enemy types are introduced in different waves, but speed remains constant
	match type:
		"fire": pass  # Red dino starts at wave 1
		"water": pass # Blue dino starts at wave 6
		"wind": pass  # Yellow dino starts at wave 11
		"earth": pass # Green dino starts at wave 16
		"normal": pass # Normal dino starts at wave 41
	
	# No speed scaling with waves anymore
	speed = base_speed

	if Globals.time_scale > 2.0:
		$AnimationPlayer.stop()

	# Apply HP scaling after all initialization is done
	if hp_multiplier != 1.0:
		var old_hp = hp
		hp *= hp_multiplier
		# print("Enemy HP scaled from ", old_hp, " to ", hp, " (multiplier: ", hp_multiplier, ")")

func _process(delta):
	if state == State.walking:
		# Use fixed timestep (60 FPS) for consistent movement
		var fixed_delta = 1.0/60.0
		progress_ratio += speed * fixed_delta * Globals.time_scale
		if progress_ratio >= 1.0:
			Globals.enemyLeak.emit()
			finished_path()
			return
			
		# Flip
		var angle = int(rotation_degrees) % 360
		if angle > 180:
			angle -= 360
		$Sprite2D.flip_v = abs(angle) > 90

func finished_path():
	if is_destroyed:
		return
	is_destroyed = true
	spawner.enemy_destroyed()
	Globals.currentMap.get_base_damage(baseDamage)
	queue_free()

func get_damage(damage, bullet_type := "normal"):
	if is_destroyed:
		return
	var final_damage = damage * (1.0 + (wind_stacks * 0.05))  # Increased from 2% to 5% per wind stack
	
	# Handle normal type's universal resistance
	if type == "normal" and bullet_type != "normal":
		final_damage *= 0.9  # 10% resistance to all elements
	# Handle same type resistance (turret and enemy of same type)
	elif type == bullet_type and type != "normal":
		final_damage *= 0.50  # 50% resistance when hit by same element type
	# Handle elemental resistances
	elif type_resistances.get(type) == bullet_type:
		final_damage *= 0.10  # 90% damage reduction against the type we resist
	# Handle elemental weaknesses
	elif type_weaknesses.get(type) == bullet_type:
		final_damage *= 2.0  # double more damage from types we're weak to
		
	hp -= final_damage
	damage_animation()
	if hp <= 0:
		is_destroyed = true
		spawner.enemy_destroyed()
		Globals.currentMap.minerals += mineralsYield
		queue_free()

func damage_animation():
	var tween := create_tween()
	tween.tween_property(self, "v_offset", 0, 0.05)
	tween.tween_property(self, "modulate", Color.ORANGE_RED, 0.1)
	tween.tween_property(self, "modulate", Color.WHITE, 0.3)
	tween.set_parallel()
	tween.tween_property(self, "v_offset", -5, 0.2)
	tween.set_parallel(false)
	tween.tween_property(self, "v_offset", 0, 0.2)

func _physics_process(delta):
	if burning and burn_duration > 0:
		var burn_tick_damage = burn_damage * delta
		
		# Apply elemental resistances/weaknesses to burn (fire) damage
		if type == "normal":
			burn_tick_damage *= 0.9  # Normal type's universal resistance
		elif type_resistances.get(type) == "fire":
			burn_tick_damage *= 0.75  # 25% less damage if resistant to fire
		elif type_weaknesses.get(type) == "fire":
			burn_tick_damage *= 1.25  # 25% more damage if weak to fire
			
		hp -= burn_tick_damage
		burn_duration -= delta
		burn_tick += delta
		if burn_tick >= 0.5:  # Every 0.5 seconds
			burn_tick = 0.0
			damage_animation()
		if burn_duration <= 0:
			burning = false
			burn_tick = 0.0
	
	if slowed and slow_duration > 0:
		slow_duration -= delta
		if slow_duration <= 0:
			slowed = false
			
			speed *= (1.0 / (1.0 - slow_amount))  # Restore original speed

func apply_burn(damage: float, duration: float):
	# Simply reset the burn effect, don't stack
	burning = true
	burn_damage = damage
	burn_duration = duration

func apply_slow(amount: float, duration: float):
	if not slowed:  # Only slow if not already slowed
		slowed = true
		speed *= (1.0 - amount)
	slow_amount = amount
	slow_duration = duration

func apply_wind_stack():
	wind_stacks += 3  # Now permanent, no duration
