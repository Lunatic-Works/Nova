add_action_before_lazy_block(function(name)
    avatar:SetCharacterName(name)
end)

add_action_after_lazy_block(function()
    avatar:UpdateImage()
end)

function avatar_show(image_name)
    if type(image_name) == 'table' then
        avatar:SetPoseDelayed(image_name)
    else
        local chara = avatar:GetCharacterController()
        if chara == nil then
            return
        end
        local pose = get_pose(chara, image_name)
        if pose then
            avatar:SetPoseDelayed(pose)
        else
            avatar:SetImageDelayed(image_name)
        end
    end
end

function avatar_hide()
    avatar:ClearImageDelayed()
    schedule_gc()
end
