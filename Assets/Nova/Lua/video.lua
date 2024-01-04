function video(video_name)
    __Nova.videoController:SetVideo(video_name)
end

function video_hide()
    __Nova.videoController:ClearVideo()
    schedule_gc()
end

function video_play()
    __Nova.videoController:Play()
end

function video_duration()
    return __Nova.videoController.duration
end
