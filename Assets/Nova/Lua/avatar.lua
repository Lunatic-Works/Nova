add_action_before_lazy_block(function(name)
    avatar:SetCharacterName(name)
end)

add_action_after_lazy_block(function()
    avatar:UpdateImage()
end)

function avatar_show(pose)
    local chara = avatar:GetCharacterController()
    if chara == nil then
        return
    end
    pose = get_pose(chara, pose)
    avatar:SetPoseDelayed(pose)
end

function avatar_hide()
    avatar:ClearImageDelayed()
    schedule_gc()
end
