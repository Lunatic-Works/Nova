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
    if fade == nil then
        fade = (auto_fade_off_count == 0)
    end

    if coord then
        move(obj, coord)
    end
    if color then
        tint(obj, color)
    end

    if type(image_name) == 'table' then
        obj:SetPose(image_name, fade)
    else
        local pose = get_pose(obj, image_name)
        if pose then
            obj:SetPose(pose, fade)
        else
            obj:SetImage(image_name, fade)
        end
    end

    if tostring(obj:GetType()) == 'Nova.SpriteController' then
        __Nova.imageUnlockHelper:Unlock(obj.imageFolder .. '/' .. image_name)
    end
end
add_preload_pattern('show')

function show_no_fade(obj, image_name, coord, color)
    show(obj, image_name, coord, color, false)
end
add_preload_pattern('show_no_fade')

function hide(obj, fade)
    if fade == nil then
        fade = (auto_fade_off_count == 0)
    end

    obj:ClearImage(fade)
    schedule_gc()
end

function hide_no_fade(obj)
    hide(obj, false)
end

function set_render_queue(obj, to)
    Nova.RenderQueueOverrider.Ensure(get_go(obj)).renderQueue = to
end
