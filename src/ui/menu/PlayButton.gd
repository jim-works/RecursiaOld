extends Button

@export var scene = preload("res://World.tscn")

func _on_Button_pressed():
	var _err = get_tree().change_scene_to_packed(scene)
