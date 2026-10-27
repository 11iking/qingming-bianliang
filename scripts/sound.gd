extends Node
## Procedurally synthesized ambience and interaction tones, original CC0 audio.
var music: AudioStreamPlayer
var fx: AudioStreamPlayer
var muted := false
var rng := RandomNumberGenerator.new()

func _ready() -> void:
	rng.seed=2026
	music=AudioStreamPlayer.new()
	add_child(music)
	music.volume_db=-18
	music.stream=make_ambience()
	muted="--self-test" in OS.get_cmdline_user_args()
	if not muted: music.play()
	fx=AudioStreamPlayer.new()
	add_child(fx)
	fx.volume_db=-13

func make_ambience() -> AudioStreamWAV:
	var rate=22050
	var duration=24.0
	var data=PackedByteArray()
	data.resize(int(duration*rate)*2)
	var notes=[220.0,293.66,329.63,440.0,493.88,440.0,329.63,293.66]
	var previous=0.0
	for i in range(int(duration*rate)):
		var t=float(i)/rate
		var segment=int(t/3)
		var nt=fmod(t,3)
		var freq=notes[segment%notes.size()]
		var envelope=exp(-nt*2.2)*minf(nt*30,1)
		var pluck=(sin(TAU*freq*t)+.35*sin(TAU*freq*2.01*t)+.14*sin(TAU*freq*3*t))*envelope*.13
		previous=lerpf(previous,rng.randf_range(-1,1),.025)
		var water=previous*.17
		var bird=sin(TAU*(1450+sin(t*18)*230)*t)*maxf(0,sin(t*.8-1.5)-.93)*.3
		var value=clampf(pluck+water+bird,-1,1)
		data.encode_s16(i*2,int(value*32767))
	var stream=AudioStreamWAV.new()
	stream.format=AudioStreamWAV.FORMAT_16_BITS
	stream.mix_rate=rate
	stream.data=data
	stream.loop_mode=AudioStreamWAV.LOOP_FORWARD
	stream.loop_end=int(duration*rate)
	return stream

func tone(kind: String = "click") -> void:
	if muted: return
	var data=PackedByteArray()
	var rate=22050
	var seconds=.35
	var freq=660.0
	if kind=="coin": freq=1108.0
	elif kind=="water": freq=180.0
	elif kind=="toast": freq=1568.0
	elif kind=="chapter": freq=440.0; seconds=.7
	data.resize(int(seconds*rate)*2)
	for i in range(int(seconds*rate)):
		var t=float(i)/rate
		var v=sin(TAU*freq*t)*exp(-t*12)*.35
		if kind=="water": v=(sin(TAU*(freq-t*150)*t)+rng.randf_range(-.25,.25))*exp(-t*9)*.25
		if kind=="toast": v+=sin(TAU*freq*2.7*t)*exp(-t*22)*.12
		data.encode_s16(i*2,int(v*32767))
	var stream=AudioStreamWAV.new()
	stream.format=AudioStreamWAV.FORMAT_16_BITS
	stream.mix_rate=rate
	stream.data=data
	fx.stream=stream
	fx.play()

func toggle() -> void:
	muted=not muted
	music.volume_db=-80 if muted else -18

func stop_all() -> void:
	music.stop()
	fx.stop()
	music.stream=null
	fx.stream=null
