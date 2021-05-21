function video(video_name)
    __Nova.videoController:SetVideo(video_name)
end

function video_hide()
    __Nova.videoController:ClearVideo()
    schedule_gc()
end

make_anim_method('video_play', function(self, duration)
    local videoPlayer = __Nova.videoController.videoPlayer
    duration = duration or videoPlayer.clip.length
    return self:action(function() videoPlayer:Play() end):wait(duration)
end)
