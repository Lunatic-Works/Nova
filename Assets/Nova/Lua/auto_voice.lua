--- length of the voice filename after padding zeros to the left
local pad_len = 2

function auto_voice_on(name, id)
    v('auto_voice_status_' .. name, 1)
    v('auto_voice_id_' .. name, id)
end

function auto_voice_off(name)
    v('auto_voice_status_' .. name, 0)
end

local auto_voice_delay = 0

function set_auto_voice_delay(value)
    auto_voice_delay = value
end

local function pad_zero(id, prefix)
    local s = tostring(id)
    s = prefix .. string.rep('0', pad_len - #s) .. s
    return s
end

auto_voice_overridden = false

function auto_voice_skip()
    auto_voice_overridden = true
end

function auto_voice_action(name)
    if auto_voice_overridden then
        auto_voice_overridden = false
        return
    end

    if name == nil or name == '' then
        return
    end
    local chara = auto_voice_config:GetCharacterControllerForName(name)
    if chara == nil then
        return
    end
    if v('auto_voice_status_' .. name) ~= 1 then
        return
    end

    local prefix = auto_voice_config:GetVoicePrefixForName(name)

    local id_name = 'auto_voice_id_' .. name
    local id = v(id_name)
    local id_str = pad_zero(id, prefix)
    say(chara, id_str, auto_voice_delay, false)

    v(id_name, id + 1)
    auto_voice_delay = 0
end
add_action_after_lazy_block(auto_voice_action)
