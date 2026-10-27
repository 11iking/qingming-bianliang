extends ColorRect
## The rods reveal the existing artwork; the painting's pixels stay unchanged.
var amount=1.0
var reveal_tween: Tween

func _ready() -> void:
	mouse_filter=Control.MOUSE_FILTER_IGNORE
	size=Vector2(1440,900)
	material=ShaderMaterial.new()
	material.shader=preload("res://scripts/scroll_frame.gdshader")

func reveal(duration: float=1.6) -> void:
	if reveal_tween and reveal_tween.is_valid(): reveal_tween.kill()
	set_amount(.01)
	reveal_tween=create_tween()
	reveal_tween.tween_method(set_amount,.01,1.0,duration).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)

func set_amount(value: float) -> void:
	amount=value
	material.set_shader_parameter("unfold",value)
