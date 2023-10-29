-- disable auto fade in trans()
local auto_fade_off_count = 0

function auto_fade_on()
    auto_fade_off_count = auto_fade_off_count - 1
    if auto_fade_off_count < 0 then
        warn('auto_fade_off_count < 0')
        auto_fade_off_count = 0
    end
end

function auto_fade_off()
    auto_fade_off_count = auto_fade_off_count + 1
end

--- usage:
---     show(obj, 'image_name', [{x, y, [scale, z, angle]}, {r, g, b, [a]}, fade])
function show(obj, image_name, coord, color, fade)
    local duration
    if fade == nil then
        fade = (auto_fade_off_count == 0)
    elseif type(fade) == 'number' then
        duration = fade
        fade = true
    end

    if coord then
        move(obj, coord)
    end
    if color then
        tint(obj, color)
    end

    local _type = obj:GetType()
    if _type == typeof(Nova.PrefabLoader) then
        obj:SetPrefab(image_name)
    elseif _type == typeof(Nova.SpriteController) then
        obj:SetImage(image_name)
        __Nova.imageUnlockHelper:Unlock(obj.imageFolder .. '/' .. image_name)
    else
        local pose = get_pose(obj, image_name)
        if duration == nil then
            obj:SetPose(pose, fade)
        else
            obj:SetPose(pose, fade, duration)
        end
        __Nova.imageUnlockHelper:Unlock(obj.imageFolder .. '/' .. pose)
    end
end
add_preload_pattern('show')

function show_no_fade(obj, image_name, coord, color)
    show(obj, image_name, coord, color, false)
end
add_preload_pattern('show_no_fade')

function hide(obj, fade)
    local duration
    if fade == nil then
        fade = (auto_fade_off_count == 0)
    elseif type(fade) == 'number' then
        duration = fade
        fade = true
    end

    local _type = obj:GetType()
    if _type == typeof(Nova.PrefabLoader) then
        obj:ClearPrefab()
    elseif _type == typeof(Nova.SpriteController) then
        obj:ClearImage()
    else
        if duration == nil then
            obj:ClearImage(fade)
        else
            obj:ClearImage(fade, duration)
        end
    end
    schedule_gc()
end

function hide_no_fade(obj)
    hide(obj, false)
end
