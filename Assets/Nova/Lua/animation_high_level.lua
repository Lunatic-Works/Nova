make_anim_method('wait', function(self, duration)
    return self:_then({ duration = duration })
end)

make_anim_method('wait_all', function(self, entry)
    return self:_then({ duration = entry.entry.totalTimeRemaining })
end)

--- usage:
---     action(func, arg1, arg2, arg3)
---     action(function() blahblah end)
make_anim_method('action', function(self, func, ...)
    local args = {...}
    if args then
        return self:_then(Nova.ActionAnimationProperty(function() func(unpack(args)) end))
    else
        return self:_then(Nova.ActionAnimationProperty(func))
    end
end)

--- infinite loop at the end of an animation chain
--- func: AnimationEntry -> AnimationEntry
--- if func returns nil, the loop will end
make_anim_method('loop', function(self, func)
    local loop_action, tail

    loop_action = function()
        local res = func(tail)
        if res == nil then
            return
        end
        tail = res:action(loop_action)
        tail.entry.evaluateOnStop = false
    end

    tail = func(self):action(loop_action)
    tail.entry.evaluateOnStop = false
    tail.head = self
    return tail
end)

function get_coord(obj)
    local transform = obj.transform
    local _pos = transform.localPosition
    local _angle = transform.localEulerAngles
    local _scale = transform.localScale
    local x = _pos.x
    local y = _pos.y
    local scale
    local camera = obj:GetComponent(typeof(Nova.CameraController))
    if camera then
        scale = camera.size
    else
        scale = {_scale.x, _scale.y, _scale.z}
    end
    local z = _pos.z
    local angle = {_angle.x, _angle.y, _angle.z}
    return x, y, scale, z, angle
end

--- use if you already know that obj is CameraController
local function get_coord_cam(camera)
    local transform = camera.transform
    local _pos = transform.localPosition
    local _angle = transform.localEulerAngles
    local x = _pos.x
    local y = _pos.y
    local scale = camera.size
    local z = _pos.z
    local angle = {_angle.x, _angle.y, _angle.z}
    return x, y, scale, z, angle
end

local function parse_coord(obj, camera, coord)
    local pos, angle, scale, relative

    if type(coord) == 'function' then
        coord = coord(obj)
    end

    -- When there is nil in coord, #coord may be wrong
    for i = 6, 3, -1 do
        if coord[i] == RELATIVE then
            relative = RELATIVE
            coord[i] = nil
            break
        end
    end

    if relative then
        if coord[1] or coord[2] or coord[4] then
            local x = coord[1] or 0
            local y = coord[2] or 0
            local z = coord[4] or 0
            pos = Vector3(x, y, z)
        end

        if coord[5] then
            angle = coord[5]
            if type(angle) == 'number' then
                angle = Vector3(0, 0, angle)
            else -- type(angle) == 'table'
                angle = Vector3(angle[1], angle[2], angle[3])
            end
        end

        if coord[3] then
            scale = coord[3]
            if camera == nil then
                if type(scale) == 'number' then
                    scale = Vector3(scale, scale, 1)
                else -- type(angle) == 'table'
                    scale = Vector3(scale[1], scale[2], scale[3])
                end
            end
        end
    else
        if coord[1] or coord[2] or coord[4] then
            local x0, y0, z0
            if camera then
                x0, y0, _, z0, _ = get_coord_cam(camera)
            else
                x0, y0, _, z0, _ = get_coord(obj)
            end

            local x = coord[1] or x0
            local y = coord[2] or y0
            local z = coord[4] or z0
            pos = Vector3(x, y, z)
        end

        if coord[5] then
            angle = coord[5]
            if type(angle) == 'number' then
                angle = Vector3(0, 0, angle)
            else -- type(angle) == 'table'
                angle = Vector3(angle[1], angle[2], angle[3])
            end
        end

        if coord[3] then
            scale = coord[3]
            if camera == nil then
                if type(scale) == 'number' then
                    scale = Vector3(scale, scale, 1)
                else -- type(angle) == 'table'
                    scale = Vector3(scale[1], scale[2], scale[3])
                end
            end
        end
    end

    return pos, angle, scale, relative
end

local easing_func_name_map = {
    linear = Nova.AnimationEntry.LinearEasing,
    cubic = Nova.AnimationEntry.CubicEasing,
    shake = Nova.AnimationEntry.ShakeEasing,
    shake_sqr = Nova.AnimationEntry.ShakeSquaredEasing,
    bezier = Nova.AnimationEntry.BezierEasing,
}

function parse_easing(t)
    local start_slope, target_slope, easing

    if t == nil then
        easing = Nova.AnimationEntry.CubicEasing(0, 0)
    elseif type(t) == 'number' then
        start_slope = t
        target_slope = t
    elseif type(t[1]) == 'number' then
        start_slope = t[1]
        target_slope = t[2]
    else -- t == {'func_name', ...}
        local func_name = t[1]
        table.remove(t, 1)
        easing = easing_func_name_map[func_name](unpack(t))
    end

    if easing == nil then
        if start_slope == 1 and target_slope == 1 then
            easing = Nova.AnimationEntry.LinearEasing()
        else
            easing = Nova.AnimationEntry.CubicEasing(start_slope, target_slope)
        end
    end

    return easing
end

--- usage:
---     move(obj, {x, y, [scale, z, angle]})
function move(obj, coord)
    local camera = obj:GetComponent(typeof(Nova.CameraController))
    local pos, angle, scale, relative = parse_coord(obj, camera, coord)

    local transform = obj.transform
    if relative then
        if pos then
            transform.localPosition = transform.localPosition + pos
        end
        if angle then
            transform.localRotation = transform.localRotation * Quaternion.Euler(angle)
        end
        if scale then
            if camera then
                camera.size = camera.size:Scale(scale)
            else
                transform.localScale = transform.localScale:Scale(scale)
            end
        end
    else
        if pos then
            transform.localPosition = pos
        end
        if angle then
            transform.localEulerAngles = angle
        end
        if scale then
            if camera then
                camera.size = scale
            else
                transform.localScale = scale
            end
        end
    end
end

--- usage:
---     move(obj, {x, y, [scale, z, angle], [RELATIVE]}, [duration, easing])
make_anim_method('move', function(self, obj, coord, duration, easing)
    local camera = obj:GetComponent(typeof(Nova.CameraController))
    local pos, angle, scale, relative = parse_coord(obj, camera, coord)
    duration = duration or 1
    easing = parse_easing(easing)

    local transform = obj.transform
    local head = self
    if relative then
        if pos then
            head = self:_then(Nova.PositionAnimationProperty(transform, pos, relative)):_with(easing):_for(duration)
        end
        if angle then
            head = self:_then(Nova.RotationAnimationProperty(transform, angle, relative)):_with(easing):_for(duration)
        end
        if scale then
            local property
            if camera then
                property = Nova.CameraSizeAnimationProperty(camera, scale, relative)
            else
                property = Nova.ScaleAnimationProperty(transform, scale, relative)
            end
            head = self:_then(property):_with(easing):_for(duration)
        end
    else
        if pos then
            head = self:_then(Nova.PositionAnimationProperty(transform, pos)):_with(easing):_for(duration)
        end
        if angle then
            head = self:_then(Nova.RotationAnimationProperty(transform, angle)):_with(easing):_for(duration)
        end
        if scale then
            local property
            if camera then
                property = Nova.CameraSizeAnimationProperty(camera, scale)
            else
                property = Nova.ScaleAnimationProperty(transform, scale)
            end
            head = self:_then(property):_with(easing):_for(duration)
        end
    end
    return head
end)

function get_renderer(obj)
    local renderer
    if obj:GetType() == typeof(Nova.SpriteController) then
        local resizer = obj.resizer
        renderer = resizer:GetComponent(typeof(UnityEngine.SpriteRenderer)) or resizer:GetComponent(typeof(UnityEngine.UI.Image))
    else
        renderer = obj:GetComponent(typeof(Nova.FadeController)) or obj:GetComponent(typeof(UnityEngine.UI.RawImage))
    end

    if renderer == nil then
        warn('Cannot find renderer for ' .. dump(obj))
    end
    return renderer
end

function get_color(obj)
    local renderer = get_renderer(obj)
    if renderer == nil then
        return nil
    end

    local color = renderer.color
    return color.r, color.g, color.b, color.a
end

function parse_color(color, is_vector)
    local Type, default, default_alpha
    if is_vector then
        Type = Vector4
        default = Vector4.zero
        default_alpha = 0
    else
        Type = Color
        default = Color.white
        default_alpha = 1
    end

    if type(color) == 'number' then
        return Type(color, color, color, default_alpha)
    else -- type(color) == 'table'
        if #color == 1 then
            warn('Table is not needed: {' .. color[1] .. '}')
            return Type(color[1], color[1], color[1], default_alpha)
        elseif #color == 2 then
            return Type(color[1], color[1], color[1], color[2])
        elseif #color == 3 then
            return Type(color[1], color[2], color[3], default_alpha)
        elseif #color == 4 then
            return Type(color[1], color[2], color[3], color[4])
        else
            warn('Parse color failed: ' .. dump(color))
            return default
        end
    end
end

--- usage:
---     tint(obj, {r, g, b, [a]})
function tint(obj, color)
    local renderer = get_renderer(obj)
    if renderer == nil then
        return
    end
    renderer.color = parse_color(color)
end

--- usage:
---     tint(obj, {r, g, b, [a]}, [duration, easing])
make_anim_method('tint', function(self, obj, color, duration, easing)
    local chara = obj:GetComponent(typeof(Nova.GameCharacterController))
    local renderer = get_renderer(obj)
    if chara == nil and renderer == nil then
        warn('Cannot find GameCharacterController or renderer for ' .. dump(obj))
        return self
    end

    duration = duration or 1
    easing = parse_easing(easing)
    local property
    if chara then
        property = Nova.ColorAnimationProperty(Nova.CharacterColor(chara, Nova.CharacterColor.Type.Base), parse_color(color))
    else
        property = Nova.ColorAnimationProperty(renderer, parse_color(color))
    end
    return self:_then(property):_with(easing):_for(duration)
end)

--- usage:
---     env_tint(obj, {r, g, b, [a]})
function env_tint(obj, color)
    local chara = obj:GetComponent(typeof(Nova.GameCharacterController))
    if chara then
        chara.environmentColor = parse_color(color)
        return
    end

    warn('Cannot find GameCharacterController for ' .. dump(obj))
end

--- usage:
---     tint(obj, {r, g, b, [a]}, [duration, easing])
make_anim_method('env_tint', function(self, obj, color, duration, easing)
    local chara = obj:GetComponent(typeof(Nova.GameCharacterController))
    if chara == nil then
        warn('Cannot find GameCharacterController for ' .. dump(obj))
        return self
    end

    duration = duration or 1
    easing = parse_easing(easing)
    local property = Nova.ColorAnimationProperty(Nova.CharacterColor(chara, Nova.CharacterColor.Type.Environment), parse_color(color))
    return self:_then(property):_with(easing):_for(duration)
end)
