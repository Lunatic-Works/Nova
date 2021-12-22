function sound(audio_name, volume, pos, use_3d)
    volume = volume or 1
    pos = pos or {0, 0, 0}
    if use_3d then
        __Nova.soundController:PlayClipAtPoint(audio_name, Vector3(unpack(pos)), volume)
    else
        __Nova.soundController:PlayClipNo3D(audio_name, Vector3(unpack(pos)), volume)
    end
end

function say(obj, audio_name, delay, override_auto_voice)
    delay = delay or 0
    if override_auto_voice == nil then
        override_auto_voice = true
    end
    obj:Say(audio_name, delay)
    if override_auto_voice then
        auto_voice_skip()
    end
end

function play(obj, audio_name, volume)
    volume = volume or 0.5
    obj.scriptVolume = volume
    obj:Play(audio_name)

    if obj:GetType() == typeof(Nova.AudioController) then
        __Nova.musicUnlockHelper:Unlock(obj.audioFolder .. '/' .. audio_name)
    end
end
add_preload_pattern('play')

function stop(obj)
    obj:Stop()
    schedule_gc()
end

function volume(obj, value)
    obj.scriptVolume = value
end

make_anim_method('volume', function(self, obj, value, duration)
    duration = duration or 1
    return self:_then(Nova.VolumeAnimationProperty(obj, value)):_for(duration)
end)

make_anim_method('fade_in', function(self, obj, audio_name, volume, duration)
    volume = volume or 0.5
    local entry = self:action(play, obj, audio_name, 0):volume(obj, volume, duration)
    entry.head = self
    return entry
end)

make_anim_method('fade_out', function(self, obj, duration)
    local entry = self:volume(obj, 0, duration):action(stop, obj)
    entry.head = self
    return entry
end)
