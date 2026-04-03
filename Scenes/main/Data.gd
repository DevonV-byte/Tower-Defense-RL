extends Node

const turrets := {
	"basic": {
		"stats": {
			"damage": 3.0,
			"attack_speed": 1.0,
			"attack_range": 150.0,
			"bulletSpeed": 300.0,
			"bulletPierce": 1,
		},
		"upgrades": {
			"damage": {"amount": 2.0, "multiplies": false},
			"attack_speed": {"amount": 1.2, "multiplies": true},
		},
		"name": "Basic Turret",
		"cost": 25,
		"upgrade_cost": 25,
		"max_level": 10,
		"scene": "res://Scenes/turrets/projectileTurret/projectileTurret.tscn",
		"sprite": "res://Assets/turrets/BasicTurret.png",
		"scale": 2.0,
		"rotates": true,
		"bullet": "basic",
	},
	"water": {
		"stats": {
			"damage": 4.0,
			"attack_speed": 1.5,
			"attack_range": 175.0,
			"bulletSpeed": 200.0,
			"bulletPierce": 1,
		},
		"upgrades": {
			"damage": {"amount": 2.0, "multiplies": false},
			"attack_speed": {"amount": 1.2, "multiplies": true},
		},
		"name": "Water Turret",
		"cost": 30,
		"upgrade_cost": 75,
		"max_level": 3,
		"scene": "res://Scenes/turrets/projectileTurret/projectileTurret.tscn",
		"sprite": "res://Assets/turrets/WaterTurret.png",
		"scale": 2.0,
		"rotates": true,
		"bullet": "water",
	},
	"fire": {
		"stats": {
			"damage": 2.2,
			"attack_speed": 10.0,
			"attack_range": 250.0,
			"bulletSpeed": 400.0,
			"bulletPierce": 4,
		},
		"upgrades": {
			"damage": {"amount": 2.5, "multiplies": false},
			"attack_speed": {"amount": 1.5, "multiplies": true},
		},
		"name": "Fire Turret",
		"cost": 70,
		"upgrade_cost": 50,
		"max_level": 3,
		"scene": "res://Scenes/turrets/projectileTurret/projectileTurret.tscn",
		"sprite": "res://Assets/turrets/FireTurret.png",
		"scale": 0.6,
		"rotates": false,
		"bullet": "fire",
		"bulletSprite": "res://Assets/bullets/bullet2.tres",
	},
	"wind": {
		"stats": {
			"damage": 10.0,
			"attack_speed": 0.5,
			"attack_range": 4000.0,
			"ray_duration": 2.5,
			"ray_length": 3500.0,
		},
		"upgrades": {
			"damage": {"amount": 1.0, "multiplies": false},
			"attack_speed": {"amount": 1.5, "multiplies": true},
			"ray_length": {"amount": 1.5, "multiplies": true},
			"ray_duration": {"amount": 1.5, "multiplies": true},
		},
		"name": "Wind Turret",
		"cost": 30,
		"upgrade_cost": 50,
		"max_level": 3,
		"scene": "res://Scenes/turrets/windTurret/windTurret.tscn",
		"sprite": "res://Assets/turrets/WindTurret.png",
		"scale": 0.5,
		"rotates": true,
		"bullet": "wind",
	},
	"earth": {
		"stats": {
			"damage": 12.0,
			"attack_speed": 0.5,
			"attack_range": 250.0,
		},
		"upgrades": {
			"damage": {"amount": 2.5, "multiplies": false},
			"attack_speed": {"amount": 1.5, "multiplies": true},
		},
		"name": "Earth Turret",
		"cost": 70,
		"upgrade_cost": 50,
		"max_level": 3,
		"scene": "res://Scenes/turrets/earthTurret/earthTurret.tscn",
		"sprite": "res://Assets/turrets/EarthTurret.png",
		"scale": 0.5,
		"rotates": false,
		"bullet": "earth"
	},
}

const stats := {
	"damage": {"name": "Damage"},
	"attack_speed": {"name": "Speed"},
	"attack_range": {"name": "Range"},
	"bulletSpeed": {"name": "Bullet Speed"},
	"bulletPierce": {"name": "Bullet Pierce"},
	"ray_length": {"name": "Ray Length"},
	"ray_duration": {"name": "Ray Duration"},
}

const bullets := {
	"basic": {
		"frames": "res://Assets/bullets/bullet1.tres",
		"type": "normal",
		"scale": 1.0
	},
	"water": {
		"frames": "res://Assets/bullets/WaterBullet.tres",
		"type": "water",
		"scale": 3.0
	},
	"fire": {
		"frames": "res://Assets/bullets/bullet2.tres",
		"type": "fire",
		"scale": 1.8
	},
	"wind": {
		"frames": "res://Assets/bullets/bullet1.tres",
		"type": "wind",
		"scale": 1.0
	},
	"earth": {
		"frames": "res://Assets/bullets/bullet1.tres",
		"type": "earth",
		"scale": 1.5
	}
}

const enemies := {
	"redDino": {
		"stats": {
			"hp": 25.0,
			"speed": 0.05,
			"baseDamage": 1.0,
			"mineralsYield": 3.0,
		},
		"type": "fire",
		"sprite": "res://Assets/enemies/dino1.png",
	},
	"blueDino": {
		"stats": {
			"hp": 40.0,
			"speed": 0.05,
			"baseDamage": 1.0,
			"mineralsYield": 3.0,
		},
		"type": "water",
		"sprite": "res://Assets/enemies/dino2.png",
	},
	"yellowDino": {
		"stats": {
			"hp": 33.0,
			"speed": 0.05,
			"baseDamage": 1.0,
			"mineralsYield": 3.0,
		},
		"type": "wind",
		"sprite": "res://Assets/enemies/dino3.png",
	},
	"greenDino": {
		"stats": {
			"hp": 25.0,
			"speed": 0.05,
			"baseDamage": 1.0,
			"mineralsYield": 3.0,
		},
		"type": "earth",
		"sprite": "res://Assets/enemies/dino4.png",
	},
	"normalDino": {
		"stats": {
			"hp": 20.0,
			"speed": 0.05,
			"baseDamage": 1.0,
			"mineralsYield": 10.0,
		},
		"type": "normal",
		"sprite": "res://Assets/enemies/dino5.png",
		"scale": 4.0
	}
}

const maps := {
	"map1": {
		"name": "Grass Map",
		"bg": "res://Assets/maps/Map1_Path.png",
		"scene": "res://Scenes/maps/map1.tscn",
		"baseHp": 50,
		"startingMinerals": 100,
		"spawner_settings":
			{
			"difficulty": {"initial": 2.0, "increase": 1.5, "multiplies": true},
			"max_waves": 50,
			"wave_spawn_count": 20,
			"special_waves": {},
			},
	}
}

const TOTAL_WAVES = 50
