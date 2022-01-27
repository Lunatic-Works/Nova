function timeline(timelime_name, time)
    time = time or 0
    __Nova.timelineController:SetPrefab(timelime_name)
    local playableDirector = __Nova.timelineController.playableDirector
    if playableDirector then
        playableDirector.time = time
        playableDirector:Evaluate()
    end
end
add_preload_pattern_with_obj('timeline', '__Nova.timelineController')

function timeline_hide()
    __Nova.timelineController:ClearPrefab()
    schedule_gc()
end

function timeline_seek(time)
    local playableDirector = __Nova.timelineController.playableDirector
    if playableDirector then
        playableDirector.time = time
        playableDirector:Evaluate()
    else
        warn('playableDirector not found')
    end
end

make_anim_method('timeline_play', function(self, to, duration, slope)
    local obj = __Nova.timelineController
    local playableDirector = obj.playableDirector
    to = to or playableDirector.duration
    duration = duration or to - playableDirector.time
    slope = slope or {1, 1}
    local easing = parse_easing(slope)
    return self:_then(Nova.TimeAnimationProperty(obj, to)):_with(easing):_for(duration)
end)
