extends Node2D

var bullet_type := "":
	set(value):
		bullet_type = value
		$AnimatedSprite2D.sprite_frames = load(Data.bullets[value]["frames"])
		type = Data.bullets[value]["type"]
		scale = Vector2.ONE * Data.bullets[value]["scale"]
		# Set base speed based on bullet type
		match type:
			"fire": speed = 800.0  # Fast bullets
			"water": speed = 400.0  # Slower bullets
			"wind": speed = 600.0  # Medium speed
			"earth": speed = 500.0  # Medium-slow speed
			"normal": speed = 700.0  # Fast-medium speed

var type := "normal"
var target = null
var direction: Vector2

var speed: float = 400.0
var damage: float = 10
var pierce: int = 1
var time: float = 1.0

func _process(delta):
	if target:
		if not direction: 
			direction = (target - position).normalized()
		# Use fixed timestep for consistent bullet speed
		var fixed_delta = 1.0/60.0
		position += direction * speed * fixed_delta * Globals.time_scale * 10.0

func _on_area_2d_area_entered(area):
	var obj = area.get_parent()
	if obj.is_in_group("enemy"):
		pierce -= 1
		obj.get_damage(damage, type)
		
		# Apply status effects based on bullet type
		match type:
			"fire":
				obj.apply_burn(damage * 0.5, 3.0)  # Changed from 5.8 to 0.2 (20% of damage per second)
			"water":
				obj.apply_slow(0.325, 1.75)  # Slightly increased from 0.2, 1.5
			"wind":
				obj.apply_wind_stack()
		
		if pierce == 0:
			queue_free()

func _on_disappear_timer_timeout():
	queue_free()
