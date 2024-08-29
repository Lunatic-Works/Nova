--- require order is important
--- built_in.lua cannot be hot-reloaded, so we use require rather than dofile
require 'built_in'

dofile 'hook'
dofile 'preload'

dofile 'advanced_dialogue_helper'
dofile 'alert'
dofile 'animation'
dofile 'animation_high_level'
dofile 'audio'
dofile 'auto_voice'
dofile 'avatar'
dofile 'checkpoint_helper'
dofile 'dialogue_box'
dofile 'garbage_collection'
dofile 'graphics'
dofile 'input'
dofile 'minigame'
dofile 'pose'
dofile 'script_loader'
dofile 'shader_info'
dofile 'timeline'
dofile 'transition'
dofile 'variables'
dofile 'video'

dofile 'animation_presets'
