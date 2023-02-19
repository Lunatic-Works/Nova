add_action_before_lazy_block(function(name)
    if current_box() ~= nil then
        current_box().avatar:SetCharacterName(name)
    end
end)

add_action_after_lazy_block(function()
    if current_box() ~= nil then
        current_box().avatar:UpdateImage()
    end
end)

function avatar_show(pose)
    local chara = current_box().avatar:GetCharacterController()
    if chara == nil then
        return
    end
    pose = get_pose(chara, pose)
    current_box().avatar:SetPoseDelayed(pose)
end

function avatar_hide()
    current_box().avatar:ClearImageDelayed()
    schedule_gc()
end
