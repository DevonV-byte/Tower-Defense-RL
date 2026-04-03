extends Node2D

const GRID_SIZE = 35
const TURRET_RADIUS = 22.8
const COLL_RADIUS = 1.0
const MIN_TURRET_SPACING = 45

var valid_positions: Array[Vector2] = []
var occupied_positions: Array[Vector2] = []
const MAX_SNAP_DISTANCE = 100

func _ready():
	await get_tree().create_timer(0.1).timeout
	initialize_positions()

func initialize_positions():
	var map_bounds = get_map_bounds()
	generate_valid_positions(map_bounds)

func get_map_bounds() -> Rect2:
	var currentMap = get_node("/root/Globals").get("currentMap")
	if currentMap:
		var background = currentMap.get_node("Background")
		if background:
			var sprite_size = background.texture.get_size() * background.scale
			var top_left = background.position - sprite_size/2
			var size = sprite_size
			return Rect2(top_left, size)
	return Rect2(-200, -200, 400, 400)

func generate_valid_positions(bounds: Rect2):
	valid_positions.clear()
	
	# Generate grid within map bounds
	for x in range(bounds.position.x + GRID_SIZE, bounds.end.x - GRID_SIZE, GRID_SIZE):
		for y in range(bounds.position.y + GRID_SIZE, bounds.end.y - GRID_SIZE, GRID_SIZE):
			var pos = Vector2(x, y)
			
			# Skip if position overlaps with obstacles or paths
			if is_position_valid(pos):
				valid_positions.append(pos)

func is_position_valid(pos: Vector2) -> bool:
	var space_state = get_world_2d().direct_space_state

	var query = PhysicsShapeQueryParameters2D.new()
	
	# Check for collisions with obstacles
	query.collision_mask = 2
	
	var shape = CircleShape2D.new()
	shape.radius = TURRET_RADIUS
	query.shape = shape
	query.transform = Transform2D(0, pos)
	
	var result = space_state.intersect_shape(query)
	if not result.is_empty():
		return false
		
	# Check distance to other turrets
	for occupied_pos in occupied_positions:
		if pos.distance_to(occupied_pos) < MIN_TURRET_SPACING:
			return false
			
	# Check path collision
	var currentMap = get_node("/root/Globals").get("currentMap")
	if currentMap:
		var path = currentMap.get_node("PathSpawner")
		if path and path.curve:
			var curve: Curve2D = path.curve
			var min_distance = 35
			var path_length = curve.get_baked_length()
			var step = 10.0
			
			for i in range(0, path_length, step):
				var path_point = curve.sample_baked(i)
				if pos.distance_to(path_point) < min_distance:
					return false
	
	# Check if position is already occupied
	if occupied_positions.has(pos):
		return false
	
	return true

# Find the closest valid position to the given position
func get_closest_valid_position(target_pos: Vector2) -> Vector2:
	var closest_pos = null
	var closest_dist = MAX_SNAP_DISTANCE
	
	for pos in valid_positions:
		if pos in occupied_positions:
			continue
			
		var dist = target_pos.distance_to(pos)
		if dist < closest_dist:
			closest_dist = dist
			closest_pos = pos
	
	return closest_pos if closest_pos else target_pos

# Mark a position as occupied when a turret is placed
func occupy_position(pos: Vector2):
	if pos in valid_positions and not pos in occupied_positions:
		occupied_positions.append(pos)

# Free up a position when a turret is removed
func free_position(pos: Vector2):
	var idx = occupied_positions.find(pos)
	if idx != -1:
		occupied_positions.remove_at(idx)

# Check if a position is valid and unoccupied
func is_valid_position(pos: Vector2) -> bool:
	return pos in valid_positions and not pos in occupied_positions

# Get all valid positions (for AI use)
func get_valid_positions() -> Array[Vector2]:
	var available = valid_positions.duplicate()
	for pos in occupied_positions:
		var idx = available.find(pos)
		if idx != -1:
			available.remove_at(idx)
	return available 
