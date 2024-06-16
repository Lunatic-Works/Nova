function stop_auto_ff()
    __Nova.dialogueState.state = Nova.DialogueState.State.Normal
end

function stop_ff()
    if __Nova.dialogueState.isFastForward then
        __Nova.dialogueState.state = Nova.DialogueState.State.Normal
    end
end

function force_step()
    __Nova.gameViewController:ForceStep()
end

function current_box()
    return __Nova.gameViewController.currentDialogueBox
end

--- offset: {left, right, top, bottom}
function box_offset(offset)
    local box = current_box()
    if box == nil then
        warn('Cannot call box_offset when the dialogue box is hidden')
        return
    end

    box.rect.offsetMin = Vector2(offset[1], offset[4])
    box.rect.offsetMax = Vector2(-offset[2], -offset[3])
end

make_anim_method('box_offset', function(self, offset, duration, easing)
    local box = current_box()
    if box == nil then
        warn('Cannot call box_offset when the dialogue box is hidden')
        return
    end

    duration = duration or 1
    easing = parse_easing(easing)
    local property = Nova.OffsetAnimationProperty(box.rect, Vector4(unpack(offset)))
    return self:_then(property):_with(easing):_for(duration)
end)

--- anchor: {left, right, bottom, top}
function box_anchor(anchor)
    local box = current_box()
    if box == nil then
        warn('Cannot call box_anchor when the dialogue box is hidden')
        return
    end

    box.rect.anchorMin = Vector2(anchor[1], anchor[3])
    box.rect.anchorMax = Vector2(anchor[2], anchor[4])
end

make_anim_method('box_anchor', function(self, anchor, duration, easing)
    local box = current_box()
    if box == nil then
        warn('Cannot call box_anchor when the dialogue box is hidden')
        return
    end

    duration = duration or 1
    easing = parse_easing(easing)
    local property = Nova.AnchorAnimationProperty(box.rect, Vector4(unpack(anchor)))
    return self:_then(property):_with(easing):_for(duration)
end)

function box_set_current(box)
    if type(box) == 'string' then
        box = _G[box]
    end
    __Nova.gameViewController:SwitchDialogueBox(box)
end

function box_tint(color)
    local box = current_box()
    if box == nil then
        warn('Cannot call box_tint when the dialogue box is hidden')
        return
    end

    box.backgroundColor = parse_color(color)
end

make_anim_method('box_tint', function(self, color, duration, easing)
    local box = current_box()
    if box == nil then
        warn('Cannot call box_tint when the dialogue box is hidden')
        return
    end

    duration = duration or 1
    easing = parse_easing(easing)
    local property = Nova.ColorAnimationProperty(Nova.DialogueBoxColor(box, Nova.DialogueBoxColor.Type.Background), parse_color(color))
    return self:_then(property):_with(easing):_for(duration)
end)

function box_alignment(mode)
    local box = current_box()
    if box == nil then
        warn('Cannot call box_alignment when the dialogue box is hidden')
        return
    end

    if mode == 'left' then
        mode = TMPro.TextAlignmentOptions.TopLeft
    elseif mode == 'center' then
        mode = TMPro.TextAlignmentOptions.Top
    elseif mode == 'right' then
        mode = TMPro.TextAlignmentOptions.TopRight
    else
        warn('Unknown text alignment: ' .. dump(mode))
        return
    end
    box.textAlignment = mode
end

function text_color(color)
    local box = current_box()
    if box == nil then
        warn('Cannot call text_color when the dialogue box is hidden')
        return
    end

    if color then
        box.textColorHasSet = true
        box.textColor = parse_color(color)
    else
        box.textColorHasSet = false
    end
end

make_anim_method('text_color', function(self, color, duration, easing)
    local box = current_box()
    if box == nil then
        warn('Cannot call text_color when the dialogue box is hidden')
        return
    end

    duration = duration or 1
    easing = parse_easing(easing)
    local property = Nova.ColorAnimationProperty(Nova.DialogueBoxColor(box, Nova.DialogueBoxColor.Type.Text), parse_color(color))
    return self:_then(property):_with(easing):_for(duration)
end)

function text_material(material_name)
    local box = current_box()
    if box == nil then
        warn('Cannot call text_material when the dialogue box is hidden')
        return
    end

    box.materialName = material_name
end

local box_pos_presets = {
    bottom = {
        box = 'default_box',
        offset = {0, 0, 0, 0},
        anchor = {0.1, 0.9, 0.05, 0.35},
    },
    top = {
        box = 'default_box',
        offset = {0, 0, 0, 0},
        anchor = {0.1, 0.9, 0.65, 0.95},
    },
    center = {
        box = 'default_box',
        offset = {0, 0, 0, 0},
        anchor = {0.1, 0.9, 0.35, 0.65},
    },
    left = {
        box = 'basic_box',
        offset = {0, 0, 0, 0},
        anchor = {0, 0.5, 0, 1},
    },
    right = {
        box = 'basic_box',
        offset = {0, 0, 0, 0},
        anchor = {0.5, 1, 0, 1},
    },
    full = {
        box = 'basic_box',
        offset = {0, 0, 0, 0},
        anchor = {0.05, 0.95, 0, 1},
    },
    hide = {
        box = nil,
    },
}

local box_style_presets = {
    light = {
        tint = 1,
        alignment = 'left',
        text_color = 0,
        text_material = '',
    },
    center = {
        tint = 1,
        alignment = 'center',
        text_color = 0,
        text_material = '',
    },
    dark = {
        tint = {0, 0.5},
        alignment = 'left',
        text_color = 1,
        text_material = 'outline',
    },
    dark_center = {
        tint = {0, 0.5},
        alignment = 'center',
        text_color = 1,
        text_material = 'outline',
    },
    transparent = {
        tint = {0, 0},
        alignment = 'left',
        text_color = 1,
        text_material = 'outline',
    },
    subtitle = {
        tint = {0, 0},
        alignment = 'center',
        text_color = 1,
        text_material = 'outline',
    },
}

function set_box(pos_name, style_name, auto_new_page)
    pos_name = pos_name or 'bottom'
    style_name = style_name or 'light'
    if auto_new_page == nil then
        auto_new_page = true
    end

    local pos = box_pos_presets[pos_name]
    if pos == nil then
        warn('Unknown box pos ' .. dump(pos_name))
        return
    end

    local style = nil
    if pos['box'] then
        style = box_style_presets[style_name]
        if style == nil then
            warn('Unknown box style ' .. dump(style_name))
            return
        end
    end

    if auto_new_page then
        new_page()
    end

    box_set_current(pos['box'])
    if pos['box'] then
        box_offset(pos['offset'])
        box_anchor(pos['anchor'])

        box_tint(style['tint'])
        box_alignment(style['alignment'])
        text_color(style['text_color'])
        text_material(style['text_material'])

        current_box():SetTextScroll(0)
    end
end

function new_page()
    local box = current_box()
    if box then
        box:NewPage()
    end
end

function text_delay(time)
    local box = current_box()
    if box == nil then
        warn('Cannot call text_delay when the dialogue box is hidden')
        return
    end

    box:SetTextAnimationDelay(time)
end

function text_duration(time)
    local box = current_box()
    if box == nil then
        warn('Cannot call text_duration when the dialogue box is hidden')
        return
    end

    box:OverrideTextDuration(time)
end

function box_hide_show(duration, pos_name, style_name)
    duration = duration or 1
    set_box(pos_name, style_name)
    local box = current_box()
    if box == nil then
        warn('Cannot call box_hide_show when the dialogue box is hidden')
        return
    end

    box:Hide(false, nil)
    text_delay(duration)
    anim:wait(duration):action(function()
        box:Show(false, nil)
    end)
end

function text_scroll(value)
    local box = current_box()
    if box == nil then
        warn('Cannot call text_scroll when the dialogue box is hidden')
        return
    end

    box:OverrideTextScroll()
    box:SetTextScroll(value)
end

make_anim_method('text_scroll', function(self, start, target, duration, easing)
    local box = current_box()
    if box == nil then
        warn('Cannot call text_scroll when the dialogue box is hidden')
        return
    end

    duration = duration or 1
    easing = parse_easing(easing)
    box:OverrideTextScroll()
    local property = box:GetTextScrollAnimationProperty(start, target)
    return self:_then(property):_with(easing):_for(duration)
end)

function auto_time(time)
    __Nova.gameViewController:OverrideAutoTime(time)
end
