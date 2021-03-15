function video(video_name)
    __Nova.videoController:SetVideo(video_name)
end

function video_hide()
    __Nova.videoController:ClearVideo()
    schedule_gc()
end

make_anim_method('video_play', function(self)
    local videoPlayer = __Nova.videoController.videoPlayer
    return self:action(function() videoPlayer:Play() end):wait(videoPlayer.clip.length)
end)
