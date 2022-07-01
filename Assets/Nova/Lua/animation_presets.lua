pos_l = {-4, -0.3, 0.53, 0, 0}
pos_c = { 0, -0.3, 0.53, 0, 0}
pos_r = { 4, -0.3, 0.53, 0, 0}

color_sunset = {1, 240/255, 220/255}

cam1_layer = 0
cam1_overlay = 15
cam2_layer = 17
cam2_overlay = 18

make_anim_method('nod', function(self, obj, distance, duration)
    distance = distance or 0.3
    duration = duration or 0.15
    local entry = self:move(obj, {0, -distance, RELATIVE}, duration, {0, 0.5}
        ):move(obj, {0, distance, RELATIVE}, duration, {0.5, 0})
    entry.head = self
    return entry
end)

make_anim_method('shake', function(self, obj, distance, duration)
    distance = distance or 0.2
    duration = duration or 0.07
    local entry = self:move(obj, {-distance, 0, RELATIVE}, duration, {0, 0.5}
        ):move(obj, {distance * 2, 0, RELATIVE}, duration * 2, 0.5
        ):move(obj, {-distance, 0, RELATIVE}, duration, {0.5, 0})
    entry.head = self
    return entry
end)

make_anim_method('cam_punch', function(self)
    local entry = self:_then(Nova.PositionAnimationProperty(cam.transform, Vector3(0, -0.2, 0), RELATIVE)):_with(parse_easing({'shake', 20, 0.5})):_for(0.4
        ):_and(Nova.CameraSizeAnimationProperty(cam, 0.9, RELATIVE)):_with(parse_easing({1, 0})):_for(0.05
        ):_then(Nova.CameraSizeAnimationProperty(cam, 1 / 0.9, RELATIVE)):_with(parse_easing()):_for(0.35)
    entry.head = self
    return entry
end)

make_anim_method('trans_fade', function(self, obj, image_name, duration)
    duration = duration or 1
    return self:trans(obj, image_name, 'fade', duration, { _Mask = 'Masks/gray', _Vague = 0.5 })
end, add_preload_pattern)

make_anim_method('trans_left', function(self, obj, image_name, duration)
    duration = duration or 1
    return self:trans(obj, image_name, 'fade', duration, { _Mask = 'Masks/wipe_left' })
end, add_preload_pattern)

make_anim_method('trans_right', function(self, obj, image_name, duration)
    duration = duration or 1
    return self:trans(obj, image_name, 'fade', duration, { _Mask = 'Masks/wipe_left', _InvertMask = 1 })
end, add_preload_pattern)

make_anim_method('trans_up', function(self, obj, image_name, duration)
    duration = duration or 1
    return self:trans(obj, image_name, 'fade', duration, { _Mask = 'Masks/wipe_up' })
end, add_preload_pattern)

make_anim_method('trans_down', function(self, obj, image_name, duration)
    duration = duration or 1
    return self:trans(obj, image_name, 'fade', duration, { _Mask = 'Masks/wipe_up', _InvertMask = 1 })
end, add_preload_pattern)

make_anim_method('trans_fade_in', function(self, obj, image_name, coord, color, duration, mask)
    duration = duration or 0.5
    mask = mask or 'Masks/wipe_up'
    local entry = self:action(function()
            vfx(obj, 'fade', 1, { _SubTex = '' })
            show_no_fade(obj, image_name, coord, color)
        end
        ):vfx(obj, 'fade', {1, 0}, duration, { _SubTex = '', _Mask = mask })
    entry.head = self
    return entry
end, add_preload_pattern)

make_anim_method('trans_fade_out', function(self, obj, duration, mask)
    duration = duration or 0.5
    mask = mask or 'Masks/wipe_up'
    local entry = self:vfx(obj, 'fade', {0, 1}, duration, { _SubTex = '', _Mask = mask, _InvertMask = 1 }
        ):action(function()
            hide_no_fade(obj)
            vfx(obj, nil)
        end)
    entry.head = self
    return entry
end)
