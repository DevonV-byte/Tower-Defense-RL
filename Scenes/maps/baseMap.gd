extends Node2D

var map_type := "":
	set(val):
		map_type = val
		baseHP = Data.maps[val]["baseHp"]
		baseMaxHp = Data.maps[val]["baseHp"]
		Globals.baseHP = baseHP
		Globals.maxHP = baseMaxHp
		Globals.minerals = Data.maps[val]["startingMinerals"]
		$PathSpawner.map_type = val

var gameOver := false
var baseMaxHp := 50.0:
	set(value):
		baseMaxHp = value
		Globals.maxHP = value

var baseHP := baseMaxHp:
	set(value):
		baseHP = value
		Globals.baseHP = value
		Globals.baseHpChanged.emit(value, baseMaxHp)

var minerals := 100:
	set(value):
		minerals = value
		Globals.minerals = value
		Globals.mineralsChanged.emit(value)

func _ready():
	Globals.turretsNode = $Turrets
	Globals.projectilesNode = $Projectiles
	Globals.currentMap = self
	Globals.baseHpChanged.emit(baseHP, baseMaxHp)

func get_base_damage(damage):
	if gameOver:
		return
	baseHP -= damage
	Globals.baseHP = baseHP
	Globals.baseHpChanged.emit(baseHP, baseMaxHp)
	if baseHP <= 0:
		gameOver = true
		Globals.game_lost.emit()
		Globals.restart_current_level()
