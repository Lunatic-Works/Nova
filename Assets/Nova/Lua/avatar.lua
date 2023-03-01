add_action_before_lazy_block(function(name)
    local box = current_box()
    if box and box.avatar then
        box.avatar:SetCharacterName(name)
    end
end)

add_action_after_lazy_block(function()
    local box = current_box()
    if box and box.avatar then
        box.avatar:UpdateImage()
    end
end)

function avatar_show(pose)
    local box = current_box()
    if box == nil then
        warn('Cannot call avatar_show when the dialogue box is hidden')
        return
    end

    local avatar = box.avatar
    if avatar == nil then
        warn('No AvatarController on the dialogue box')
        return
    end

    local chara = avatar:GetCharacterController()
    if chara == nil then
        return
    end

    pose = get_pose(chara, pose)
    avatar:SetPoseDelayed(pose)
end

function avatar_hide()
    local box = current_box()
    if box and box.avatar then
        box.avatar:ClearImageDelayed()
    end
    schedule_gc()
end
