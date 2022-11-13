local shader_alias_map = {
    broken_tv = 'Broken TV',
}

local cam_trans_layer_id = 1

--- convert Lua-style name to Unity-style name, e.g., 'foo_bar' -> 'Foo Bar'
local function get_base_shader_name(s)
    return string.upper(string.sub(s, 1, 1)) .. string.gsub(string.sub(s, 2), '_(.)', function(x) return ' ' .. string.upper(x) end)
end

local function get_full_shader_name(shader_name, pp)
    local raw_shader_name = shader_name

    local variant
    variant, shader_name = pop_prefix(shader_name, 'multiply', 1)
    if not variant then
        variant, shader_name = pop_prefix(shader_name, 'screen', 1)
    end

    if shader_name == '' then
        shader_name = 'default'
    end

    local base_shader_name = shader_alias_map[shader_name] or get_base_shader_name(shader_name)

    local full_shader_name
    if pp then
        if variant then
            warn('Post processing does not support multiply or screen shader, raw_shader_name: ' .. raw_shader_name)
        end
        full_shader_name = 'Nova/Post Processing/' .. base_shader_name
    else
        if variant == 'multiply' then
            full_shader_name = 'Nova/VFX Multiply/' .. base_shader_name
        elseif variant == 'screen' then
            full_shader_name = 'Nova/VFX Screen/' .. base_shader_name
        else
            full_shader_name = 'Nova/VFX/' .. base_shader_name
            variant = 'default'
        end
    end

    return full_shader_name, base_shader_name, variant
end

local function get_renderer_pp(obj)
    local go = obj.gameObject
    local renderer, pp
    if obj:GetType() == typeof(Nova.CameraController) or obj:GetType() == typeof(Nova.GameCharacterController) then
        pp = go:GetComponent(typeof(Nova.PostProcessing))
    else
        renderer = go:GetComponent(typeof(UnityEngine.SpriteRenderer)) or go:GetComponent(typeof(UnityEngine.UI.Image)) or go:GetComponent(typeof(UnityEngine.UI.RawImage))
    end
    return go, renderer, pp
end

--- get material from the MaterialPool attached to the GameObject
local function get_mat(obj, shader_name, restorable)
    if shader_name == nil then
        return nil
    end

    if restorable == nil then
        restorable = true
    end

    local go, renderer, pp = get_renderer_pp(obj)
    if renderer == nil and pp == nil then
        warn('Cannot find SpriteRenderer or Image or RawImage or PostProcessing for ' .. dump(obj))
        return nil
    end

    local full_shader_name, base_shader_name, variant = get_full_shader_name(shader_name, pp)

    -- Overriding cameras do not have MaterialPool, otherwise their
    -- materials may be disposed before some animations finish
    local pool = Nova.MaterialPool.Ensure(go)
    local mat
    if restorable then
        mat = pool:GetRestorableMaterial(full_shader_name)
    else
        mat = pool:Get(full_shader_name)
    end

    if mat == nil then
        warn('Cannot find material: ' .. shader_name)
        return nil
    end

    return mat, base_shader_name, variant
end

local function get_default_mat(obj)
    return Nova.MaterialPool.Ensure(obj.gameObject).defaultMaterial
end

local function set_mat(obj, mat, layer_id, token)
    layer_id = layer_id or 0
    token = token or -1

    local go, renderer, pp = get_renderer_pp(obj)

    if renderer then
        if layer_id ~= 0 then
            warn('layer_id should be 0 for SpriteRenderer or Image or RawImage')
        end
        renderer.material = mat
        return -1
    end

    if pp then
        if mat then
            return pp:SetLayer(layer_id, mat)
        else
            pp:ClearLayer(layer_id, token)
            return -1
        end
    end

    local fade = go:GetComponent(typeof(Nova.FadeController))
    if fade then
        warn('Cannot set material for FadeController ' .. dump(obj))
        return -1
    end

    warn('Cannot find SpriteRenderer or Image or PostProcessing for ' .. dump(obj))
    return -1
end

local function set_mat_default_properties(mat, base_shader_name, skip_properties)
    local _float_data = shader_float_data[base_shader_name]
    if _float_data then
        for name, value in pairs(_float_data) do
            if skip_properties[name] == nil then
                mat:SetFloat(name, value)
            end
        end
    end

    local _color_data = shader_color_data[base_shader_name]
    if _color_data then
        for name, value in pairs(_color_data) do
            if skip_properties[name] == nil then
                mat:SetColor(name, value)
            end
        end
    end

    local _vector_data = shader_vector_data[base_shader_name]
    if _vector_data then
        for name, value in pairs(_vector_data) do
            if skip_properties[name] == nil then
                mat:SetVector(name, value)
            end
        end
    end
end

-- render targets can be used in this function
local function set_texture_by_path(mat, name, path)
    if mat:GetType() == typeof(Nova.RestorableMaterial) then
        mat:SetTexturePath(name, path)
    else
        local prefix, rt_name = pop_prefix(path, Nova.AssetLoader.RenderTargetPrefix)
        if prefix then
            local rt = Nova.AssetLoader.LoadRenderTarget(rt_name)
            rt:Bind(mat, name)
        else
            local tex
            -- we cannot set value = nil in table, so we use ''
            if path and path ~= '' then
                tex = Nova.AssetLoader.LoadTexture(path)
            end
            mat:SetTexture(name, tex)
        end
    end
end

local function set_mat_properties(mat, base_shader_name, properties)
    local _type_data = shader_type_data[base_shader_name]
    if _type_data == nil then
        return
    end

    for name, value in pairs(properties) do
        local dtype = _type_data[name]
        if dtype == 'Float' then
            mat:SetFloat(name, value)
        elseif dtype == 'Color' then
            mat:SetColor(name, parse_color(value))
        elseif dtype == 'Vector' then
            mat:SetVector(name, parse_color(value, true))
        elseif dtype == '2D' then
            set_texture_by_path(mat, name, value)
        else
            warn('Unknown dtype ' .. dump(dtype) .. ' for property ' .. dump(name))
        end
    end
end

local function parse_shader_layer(shader_layer, default_layer_id)
    default_layer_id = default_layer_id or 0
    if shader_layer == nil then
        return nil, default_layer_id
    elseif type(shader_layer) == 'string' then
        return shader_layer, default_layer_id
    else -- type(shader_layer) == 'table'
        return shader_layer[1], shader_layer[2]
    end
end

--- cubic easing is already in FadeWithMask.shader,
--- so the default easing here is linear
local function parse_times(times)
    if times == nil then
        return 1, Nova.AnimationEntry.LinearEasing()
    elseif type(times) == 'number' then
        return times, Nova.AnimationEntry.LinearEasing()
    else -- type(times) == 'table'
        return times[1], parse_easing(times[2])
    end
end

--- sprite or camera transition using a shader with two textures
--- the shader should implement _MainTex, _SubTex, _SubColor and _T
--- range of _T is (0, 1), _T = 0 shows _MainTex, _T = 1 shows _SubTex
--- usage:
---     trans(obj, 'image_name', 'shader_name', [duration, { name = value }, {r, g, b, [a]}])
---     trans(cam, func, 'shader_name', [duration, { name = value }, {r, g, b, [a]}])
make_anim_method('trans', function(self, obj, image_name, shader_layer, times, properties, color2)
    local shader_name, layer_id = parse_shader_layer(shader_layer, cam_trans_layer_id)
    -- mat is not RestorableMaterial
    local mat, base_shader_name, _ = get_mat(obj, shader_name, false)
    local duration, easing = parse_times(times)
    properties = properties or {}

    local action_begin, action_end, token
    if obj:GetType() == typeof(Nova.CameraController) then
        action_begin = function()
            __Nova.screenCapturer:CaptureGameTexture()

            auto_fade_off()
            local func = image_name
            func()
            auto_fade_on()

            -- set the material for `cam` after `func`, because it may change
            -- the material for `cam`
            mat:SetTexture('_SubTex', __Nova.screenCapturer.capturedGameTexture)
            set_mat_default_properties(mat, base_shader_name, properties)
            set_mat_properties(mat, base_shader_name, properties)
            mat:SetFloat('_T', 1)
            token = set_mat(obj, mat, layer_id)
        end

        action_end = function()
            set_mat(obj, get_default_mat(obj), layer_id, token)
        end
    else
        action_begin = function()
            if obj.currentImageName and obj.currentImageName ~= '' then
                local tex = Nova.AssetLoader.LoadTexture(obj.imageFolder .. '/' .. obj.currentImageName)
                mat:SetTexture('_SubTex', tex)
            else
                mat:SetTexture('_SubTex', nil)
            end
            local renderer = obj:GetComponent(typeof(UnityEngine.SpriteRenderer)) or obj:GetComponent(typeof(UnityEngine.UI.Image)) or obj:GetComponent(typeof(UnityEngine.UI.RawImage))
            set_mat_default_properties(mat, base_shader_name, properties)
            set_mat_properties(mat, base_shader_name, properties)
            mat:SetFloat('_T', 1)
            mat:SetColor('_SubColor', renderer.color)
            token = set_mat(obj, mat)

            show_no_fade(obj, image_name, nil, color2)
        end

        action_end = function()
            set_mat(obj, get_default_mat(obj), nil, token)
        end
    end

    -- parallel animation entries should start after `action_begin`, because it
    -- may contain operations conflicting with other animation entries
    local entry0 = self:action(action_begin)
    local entry = entry0:_then(Nova.MaterialFloatAnimationProperty(mat, '_T', 0)):_with(easing):_for(duration
        ):action(action_end)
    entry.head = entry0
    return entry
end, add_preload_pattern)

--- sprite transition using a shader that hides the texture
--- the shader should implement _MainTex and _T
--- range of _T is (0, 1), _T = 0 shows _MainTex, _T = 1 hides _MainTex
--- usage:
---     trans2(obj, 'image_name', 'shader_name', [duration, { name = value }, duration2, { name = value }, {r, g, b, [a]}])
make_anim_method('trans2', function(self, obj, image_name, shader_layer, times, properties, times2, properties2, color2)
    local shader_name, layer_id = parse_shader_layer(shader_layer, cam_trans_layer_id)
    -- mat is not RestorableMaterial
    local mat, base_shader_name, _ = get_mat(obj, shader_name, false)
    local duration, easing = parse_times(times)
    properties = properties or {}
    local duration2, easing2 = parse_times(times2)
    properties2 = properties2 or {}

    local action_begin, action_middle, action_end, token
    if obj:GetType() == typeof(Nova.CameraController) then
        action_begin = function()
            set_mat_default_properties(mat, base_shader_name, properties)
            set_mat_properties(mat, base_shader_name, properties)
            mat:SetFloat('_T', 0)
            token = set_mat(obj, mat, layer_id)
        end

        action_middle = function()
            if image_name then
                auto_fade_off()
                local func = image_name
                func()
                auto_fade_on()
            end

            set_mat_properties(mat, properties2)
        end

        action_end = function()
            set_mat(obj, get_default_mat(obj), layer_id, token)
        end
    else
        action_begin = function()
            set_mat_default_properties(mat, base_shader_name, properties)
            set_mat_properties(mat, base_shader_name, properties)
            mat:SetFloat('_T', 0)
            token = set_mat(obj, mat)
        end

        action_middle = function()
            if image_name then
                show_no_fade(obj, image_name, nil, color2)
            end

            set_mat_properties(mat, properties2)
        end

        action_end = function()
            set_mat(obj, get_default_mat(obj), nil, token)
        end
    end

    local entry = self:action(action_begin
        ):_then(Nova.MaterialFloatAnimationProperty(mat, '_T', 1)):_with(easing):_for(duration
        ):action(action_middle
        ):_then(Nova.MaterialFloatAnimationProperty(mat, '_T', 0)):_with(easing2):_for(duration2
        ):action(action_end)
    entry.head = self
    return entry
end, add_preload_pattern)

--- usage:
---     vfx(obj, 'shader_name', [t, { name = value }])
---     vfx(obj, {'shader_name', layer_id}, [t, { name = value }])
function vfx(obj, shader_layer, t, properties)
    local shader_name, layer_id = parse_shader_layer(shader_layer)
    if shader_name then
        local mat, base_shader_name, _ = get_mat(obj, shader_name)
        t = t or 1
        properties = properties or {}
        set_mat_default_properties(mat, base_shader_name, properties)
        set_mat_properties(mat, base_shader_name, properties)
        mat:SetFloat('_T', t)
        set_mat(obj, mat, layer_id)
    else
        set_mat(obj, get_default_mat(obj), layer_id)
    end
end

--- visual effect on sprite or camera using shader
--- the shader should implement _MainTex and _T
--- range of _T is (0, 1), _T = 0 means no effect, _T = 1 means maximum effect
--- if target_t = 0, the material will be set to default after the animation
--- usage:
---     vfx(obj, 'shader_name', {start_t, target_t}, duration, [{ name = value }])
make_anim_method('vfx', function(self, obj, shader_layer, start_target_t, times, properties)
    local shader_name, layer_id = parse_shader_layer(shader_layer)
    local mat, base_shader_name, variant = get_mat(obj, shader_name)
    local start_t, target_t = unpack(start_target_t)
    local duration, easing = parse_times(times)
    properties = properties or {}

    local action_begin = function()
        set_mat_default_properties(mat, base_shader_name, properties)
        set_mat_properties(mat, base_shader_name, properties)
        mat:SetFloat('_T', start_t)
        set_mat(obj, mat, layer_id)
    end

    local entry = self:action(action_begin
        ):_then(Nova.MaterialFloatAnimationProperty(mat, '_T', target_t)):_with(easing):_for(duration)

    if target_t == 0 then
        local action_end = function()
            if variant then
                set_mat(obj, get_mat(obj, variant), layer_id)
            else
                set_mat(obj, get_default_mat(obj), layer_id)
            end
        end
        entry = entry:action(action_end)
    end

    entry.head = self
    return entry
end)

--- usage:
---     vfx_multi(obj, 'shader_name', duration, { name = {start_value, target_value} }, [{ name = value }])
make_anim_method('vfx_multi', function(self, obj, shader_layer, times, anim_properties, properties)
    local shader_name, layer_id = parse_shader_layer(shader_layer)
    local mat, base_shader_name, _ = get_mat(obj, shader_name)
    local duration, easing = parse_times(times)
    properties = properties or {}

    local action_begin = function()
        set_mat_default_properties(mat, base_shader_name, properties)
        set_mat_properties(mat, base_shader_name, properties)
        for name, value in pairs(anim_properties) do
            mat:SetFloat(name, value[1])
        end
        mat:SetFloat('_T', 1)
        set_mat(obj, mat, layer_id)
    end

    local entry0 = self:action(action_begin)
    local entry
    for name, value in pairs(anim_properties) do
        entry = entry0:_then(Nova.MaterialFloatAnimationProperty(mat, name, value[2])):_with(easing):_for(duration)
    end
    entry.head = self
    return entry
end)
