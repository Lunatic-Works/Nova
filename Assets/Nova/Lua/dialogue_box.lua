function stop_auto_ff()
    __Nova.dialogueBoxController.state = Nova.DialogueBoxState.Normal
end

function stop_ff()
    if __Nova.dialogueBoxController.state == Nova.DialogueBoxState.FastForward then
        __Nova.dialogueBoxController.state = Nova.DialogueBoxState.Normal
    end
end

function force_step()
    __Nova.dialogueBoxController:ForceStep()
end

function box_update_mode(mode)
    if mode == 'append' then
        mode = Nova.DialogueBoxController.DialogueUpdateMode.Append
    elseif mode == 'overwrite' then
        mode = Nova.DialogueBoxController.DialogueUpdateMode.Overwrite
    else
        warn('Unknown dialogue update mode: ' .. tostring(mode))
        return
    end
    __Nova.dialogueBoxController.dialogueUpdateMode = mode
end

function box_offset(offset_left, offset_right, offset_top, offset_bottom)
    __Nova.dialogueBoxController.rect.offsetMin = Vector2(offset_left, offset_bottom)
    __Nova.dialogueBoxController.rect.offsetMax = Vector2(-offset_right, -offset_top)
end

function box_anchor(anchor_left, anchor_right, anchor_top, anchor_bottom)
    __Nova.dialogueBoxController.rect.anchorMin = Vector2(anchor_left, anchor_top)
    __Nova.dialogueBoxController.rect.anchorMax = Vector2(anchor_right, anchor_bottom)
end

function box_tint(color)
    __Nova.dialogueBoxController.backgroundColor = parse_color(color)
end

function box_themed(themed)
    __Nova.dialogueBoxController.useThemedBox = themed
end

function box_alignment(mode)
    if mode == 'left' then
        mode = TMPro.TextAlignmentOptions.TopLeft
    elseif mode == 'center' then
        mode = TMPro.TextAlignmentOptions.Top
    elseif mode == 'right' then
        mode = TMPro.TextAlignmentOptions.TopRight
    else
        warn('Unknown text alignment: ' .. tostring(mode))
        return
    end
    __Nova.dialogueBoxController.textAlignment = mode
end

function box_text_color(color)
    if color then
        __Nova.dialogueBoxController.textColorHasSet = true
        __Nova.dialogueBoxController.textColor = parse_color(color)
    else
        __Nova.dialogueBoxController.textColorHasSet = false
    end
end

make_anim_method('box_text_color', function(self, color, duration)
    local property = Nova.ColorAnimationProperty(Nova.DialogueBoxColor(__Nova.dialogueBoxController, Nova.DialogueBoxColor.Type.Text), parse_color(color))
    return self:_then(property):_for(duration)
end)

function box_text_material(material_name)
    __Nova.dialogueBoxController.materialName = material_name
end

box_pos_presets = {
    bottom = {
        update_mode = 'overwrite',
        offset = {0, 0, 0, 0},
        anchor = {0.1, 0.9, 0.05, 0.35},
    },
    top = {
        update_mode = 'overwrite',
        offset = {0, 0, 0, 0},
        anchor = {0.1, 0.9, 0.65, 0.95},
    },
    center = {
        update_mode = 'overwrite',
        offset = {0, 0, 0, 0},
        anchor = {0.1, 0.9, 0.35, 0.65},
    },
    left = {
        update_mode = 'append',
        offset = {0, 0, 0, 0},
        anchor = {0, 0.5, 0, 1},
    },
    right = {
        update_mode = 'append',
        offset = {0, 0, 0, 0},
        anchor = {0.5, 1, 0, 1},
    },
    full = {
        update_mode = 'append',
        offset = {0, 0, 0, 0},
        anchor = {0.05, 0.95, 0, 1},
    },
    hide = {
        update_mode = 'overwrite',
        offset = {0, 0, 0, 0},
        anchor = {0.1, 0.9, 2.05, 2.35},
    },
}

box_style_presets = {
    light = {
        tint = 1,
        alignment = 'left',
        text_color = 0,
        text_material = '',
        themed = true
    },
    center = {
        tint = 1,
        alignment = 'center',
        text_color = 0,
        text_material = '',
        themed = true
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
        warn('Unknown box pos ' .. tostring(pos_name))
        return
    end

    local style = box_style_presets[style_name]
    if style == nil then
        warn('Unknown box style ' .. tostring(style_name))
        return
    end

    if auto_new_page and pos['update_mode'] == 'append' then
        new_page()
    end

    box_update_mode(pos['update_mode'])
    box_offset(unpack(pos['offset']))
    box_anchor(unpack(pos['anchor']))

    box_tint(style['tint'])
    box_alignment(style['alignment'])
    box_text_color(style['text_color'])
    box_text_material(style['text_material'])
    box_themed(style['themed'] or false)
end

function new_page()
    __Nova.dialogueBoxController:NewPage()
end

function text_delay(time)
    __Nova.dialogueBoxController:SetTextAnimationDelay(time)
end

function box_hide_show(duration, pos_name, style_name)
    duration = duration or 1
    -- set style and new page before animation
    set_box('hide', style_name, false)
    if box_pos_presets[pos_name] and box_pos_presets[pos_name]['update_mode'] == 'append' then
        new_page()
    end
    anim:wait(duration):action(set_box, pos_name, style_name, false)
    text_delay(duration)
end

function box_close_button_on()
    __Nova.dialogueBoxController:ShowCloseButton()
end

function box_close_button_off()
    __Nova.dialogueBoxController:HideCloseButton()
end

function box_finish_icon_on()
    __Nova.dialogueBoxController.dialogueFinishIconEnabled = true
end

function box_finish_icon_off()
    __Nova.dialogueBoxController.dialogueFinishIconEnabled = false
end
