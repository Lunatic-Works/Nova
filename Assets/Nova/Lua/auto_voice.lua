function auto_voice_on(name, index)
    __Nova.autoVoice:SetEnabled(name, true)
    __Nova.autoVoice:SetIndex(name, index)
end

function auto_voice_off(name)
    __Nova.autoVoice:SetEnabled(name, false)
end

function auto_voice_off_all()
    __Nova.autoVoice:DisableAll()
end

local auto_voice_delay = 0

function set_auto_voice_delay(value)
    auto_voice_delay = value
end

local auto_voice_overridden = false

function auto_voice_skip()
    auto_voice_overridden = true
end

add_action_after_lazy_block(function(name)
    if auto_voice_overridden then
        auto_voice_overridden = false
        return
    end
    if name == nil or name == '' then
        return
    end
    if not __Nova.autoVoice:GetEnabled(name) then
        return
    end
    local chara = __Nova.autoVoice:GetCharacterController(name)
    local audio_name = __Nova.autoVoice:GetAudioName(name)
    say(chara, audio_name, auto_voice_delay, false)
    __Nova.autoVoice:IncrementIndex(name)
    auto_voice_delay = 0
end)
