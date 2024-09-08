-- No need to restore because it's always assigned before lazy block
local last_avatar_name = ''

function swap_last_avatar_name()
    local box = current_box()
    if box and box.avatar then
        local name = box.avatar.characterName
        box.avatar.characterName = last_avatar_name
        last_avatar_name = name
    end
end

add_action_before_lazy_block(function(name)
    local box = current_box()
    if box and box.avatar then
        last_avatar_name = box.avatar.characterName
        box.avatar.characterName = name
    else
        last_avatar_name = name
    end
end)

add_action_after_lazy_block(function()
    local box = current_box()
    if box and box.avatar then
        box.avatar:UpdateImage()
    end
end)

local function current_avatar()
    local box = current_box()
    if box == nil then
        warn('Cannot call current_avatar when the dialogue box is hidden')
        return nil
    end

    local avatar = box.avatar
    if avatar == nil then
        warn('No AvatarController on the dialogue box')
        return nil
    end

    return avatar
end

function avatar(pose, color)
    local avatar = current_avatar()
    if avatar == nil then
        return
    end

    local chara = avatar:GetCharacterController()
    if chara == nil then
        return
    end

    if pose == '.' then
        pose = avatar.characterName
    end

    pose = get_pose(chara, pose)
    avatar:SetPoseDelayed(pose)

    if color and color ~= '' then
        tint(avatar, color)
    end
end

function avatar_hide(name)
    local avatar = current_avatar()
    if avatar == nil then
        return
    end

    if name == nil then
        avatar:ClearImageDelayed()
    else
        avatar:ClearImageDelayed(name)
    end

    schedule_gc()
end

-- TODO: Clear avatar for all dialogue boxes
function avatar_clear()
    local avatar = current_avatar()
    if avatar == nil then
        return
    end

    avatar:ResetAll()
    schedule_gc()
    tint(avatar, 1)
end
