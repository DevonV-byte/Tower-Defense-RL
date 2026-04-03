extends Turret

var attack_timer := 0.0

func _process(delta):
	# Call parent _process to maintain input handling
	super._process(delta)
	
	if not current_target:
		try_get_closest_target()
		
	# Use fixed timestep for consistent attack timing
	if is_instance_valid(current_target):
		var fixed_delta = 1.0/60.0
		attack_timer += fixed_delta * Globals.time_scale
		if attack_timer >= 1.0 / attack_speed:
			attack_timer = 0
			attack()

func attack():
	if is_instance_valid(current_target):
		$AnimatedSprite2D.play("default")
		for a in $DetectionArea.get_overlapping_areas():
			var collider = a.get_parent()
			if collider.is_in_group("enemy"):
				collider.get_damage(damage, "earth")
	else:
		try_get_closest_target()
