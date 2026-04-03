extends Control

var mapSelectContainer : PanelContainer

func _on_quit_button_pressed():
	get_tree().quit()

func _on_start_button_pressed():
	Globals.aiMode = false
	_show_map_select()

func _on_train_ai_button_pressed():
	Globals.aiMode = true
	_show_map_select()

func _show_map_select():
	if not mapSelectContainer:
		var mscScene := preload("res://Scenes/ui/mainMenu/select_map_container.tscn")
		var msc := mscScene.instantiate()
		mapSelectContainer = msc
		add_child(msc)
