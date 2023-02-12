function stop_auto_ff()
    __Nova.dialogueState.state = Nova.DialogueState.State.Normal
end

function stop_ff()
    if __Nova.dialogueState.isFastForward then
        __Nova.dialogueState.state = Nova.DialogueState.State.Normal
    end
end

function current_box()
    return __Nova.gameViewController.currentDialogueBox
end

--- offset: {left, right, top, bottom}
function box_offset(offset)
    current_box().rect.offsetMin = Vector2(offset[1], offset[4])
    current_box().rect.offsetMax = Vector2(-offset[2], -offset[3])
end

make_anim_method('box_offset', function(self, offset, duration, easing)
    duration = duration or 1
    easing = parse_easing(easing)
    local property = Nova.OffsetAnimationProperty(current_box().rect, Vector4(unpack(offset)))
    return self:_then(property):_with(easing):_for(duration)
end)

--- anchor: {left, right, bottom, top}
function box_anchor(anchor)
    current_box().rect.anchorMin = Vector2(anchor[1], anchor[3])
    current_box().rect.anchorMax = Vector2(anchor[2], anchor[4])
end

make_anim_method('box_anchor', function(self, anchor, duration, easing)
    duration = duration or 1
    easing = parse_easing(easing)
    local property = Nova.AnchorAnimationProperty(current_box().rect, Vector4(unpack(anchor)))
    return self:_then(property):_with(easing):_for(duration)
end)

function box_update_mode(mode)
    -- current_box().dialogueUpdateMode = mode
end

function box_theme(theme)
    -- current_box().theme = theme
end

function box_tint(color)
    current_box().backgroundColor = parse_color(color)
end

make_anim_method('box_tint', function(self, color, duration, easing)
    duration = duration or 1
    easing = parse_easing(easing)
    local property = Nova.ColorAnimationProperty(Nova.DialogueBoxColor(current_box(), Nova.DialogueBoxColor.Type.Background), parse_color(color))
    return self:_then(property):_with(easing):_for(duration)
end)

function box_alignment(mode)
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
    current_box().textAlignment = mode
end

function text_color(color)
    if color then
        current_box().textColorHasSet = true
        current_box().textColor = parse_color(color)
    else
        current_box().textColorHasSet = false
    end
end

make_anim_method('text_color', function(self, color, duration, easing)
    duration = duration or 1
    easing = parse_easing(easing)
    local property = Nova.ColorAnimationProperty(Nova.DialogueBoxColor(current_box(), Nova.DialogueBoxColor.Type.Text), parse_color(color))
    return self:_then(property):_with(easing):_for(duration)
end)

function text_material(material_name)
    current_box().materialName = material_name
end

box_pos_presets = {
    bottom = {
        offset = {0, 0, 0, 0},
        anchor = {0.1, 0.9, 0.05, 0.35},
        update_mode = Nova.DialogueBoxController.DialogueUpdateMode.Overwrite,
        theme = Nova.DialogueBoxController.Theme.Default,
    },
    top = {
        offset = {0, 0, 0, 0},
        anchor = {0.1, 0.9, 0.65, 0.95},
        update_mode = Nova.DialogueBoxController.DialogueUpdateMode.Overwrite,
        theme = Nova.DialogueBoxController.Theme.Default,
    },
    center = {
        offset = {0, 0, 0, 0},
        anchor = {0.1, 0.9, 0.35, 0.65},
        update_mode = Nova.DialogueBoxController.DialogueUpdateMode.Overwrite,
        theme = Nova.DialogueBoxController.Theme.Default,
    },
    left = {
        offset = {0, 0, 0, 0},
        anchor = {0, 0.5, 0, 1},
        update_mode = Nova.DialogueBoxController.DialogueUpdateMode.Append,
        theme = Nova.DialogueBoxController.Theme.Basic,
    },
    right = {
        offset = {0, 0, 0, 0},
        anchor = {0.5, 1, 0, 1},
        update_mode = Nova.DialogueBoxController.DialogueUpdateMode.Append,
        theme = Nova.DialogueBoxController.Theme.Basic,
    },
    full = {
        offset = {0, 0, 0, 0},
        anchor = {0.05, 0.95, 0, 1},
        update_mode = Nova.DialogueBoxController.DialogueUpdateMode.Append,
        theme = Nova.DialogueBoxController.Theme.Basic,
    },
    hide = {
        offset = {0, 0, 0, 0},
        anchor = {0.1, 0.9, 2.05, 2.35},
        update_mode = Nova.DialogueBoxController.DialogueUpdateMode.Append,
        theme = Nova.DialogueBoxController.Theme.Basic,
    },
}

box_style_presets = {
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

    local style = box_style_presets[style_name]
    if style == nil then
        warn('Unknown box style ' .. dump(style_name))
        return
    end

    if auto_new_page and pos['update_mode'] == Nova.DialogueBoxController.DialogueUpdateMode.Append then
        new_page()
    end

    box_offset(pos['offset'])
    box_anchor(pos['anchor'])
    box_update_mode(pos['update_mode'])
    box_theme(pos['theme'])

    box_tint(style['tint'])
    box_alignment(style['alignment'])
    text_color(style['text_color'])
    text_material(style['text_material'])

    current_box():SetTextScroll(0)
end

function new_page()
    current_box():NewPage()
end

function text_delay(time)
    current_box():SetTextAnimationDelay(time)
end

function text_duration(time)
    current_box():OverrideTextDuration(time)
end

function box_hide_show(duration, pos_name, style_name)
    duration = duration or 1
    -- set style and new page before animation
    set_box('hide', style_name, false)
    new_page()
    anim:wait(duration):action(set_box, pos_name, style_name, false)
    text_delay(duration)
end

function text_scroll(value)
    current_box():OverrideTextScroll()
    current_box():SetTextScroll(value)
end

make_anim_method('text_scroll', function(self, start, target, duration, easing)
    duration = duration or 1
    easing = parse_easing(easing)
    current_box():OverrideTextScroll()
    local property = current_box():GetTextScrollAnimationProperty(start, target)
    return self:_then(property):_with(easing):_for(duration)
end)
