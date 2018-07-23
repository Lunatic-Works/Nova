---
--- Created by L.
--- DateTime: 2018/7/23 12:48 PM
---

--- play bgm
--- this function will stop the previous bgm and start the new one
--- this function should have bgmController binded
function play_bgm(audio_name)
    __Nova.bgmController:PlayAudio(audio_name)
end

--- stop current playing bgm
--- if no bgm is playing, this function will do nothing
--- this function should have bgmController binded
function stop_bgm()
    __Nova.bgmController:StopAudio()
end

--- play sound
--- this function should have soundController binded
function play_sound(audio_name)
    __Nova.soundController:PlayAudio(audio_name)
end 