--- Handle Nova variable whose name starts with `v_` or `gv_` in Lua
--- Disable implicit global variable declaration, see https://www.lua.org/pil/14.2.html
local declared_global_variables = {}
local _sub = string.sub
setmetatable(_G, {
    __index = function(t, name)
        if _sub(name, 1, 2) == 'v_' then
            return get_nova_variable(name, false)
        elseif _sub(name, 1, 3) == 'gv_' then
            return get_nova_variable(name, true)
        else
            if not declared_global_variables[name] then
                warn('Attempt to read undeclared global variable: ' .. name)
            end
            return rawget(t, name)
        end
    end,

    __newindex = function(t, name, value)
        if _sub(name, 1, 2) == 'v_' then
            set_nova_variable(name, value, false)
        elseif _sub(name, 1, 3) == 'gv_' then
            set_nova_variable(name, value, true)
        else
            declared_global_variables[name] = true
            rawset(t, name, value)
        end
    end,
})

__Nova = {}

--- show warning without halting the game
function warn(s)
    print('<color=red>' .. s .. '</color>\n' .. debug.traceback())
end

--- dump a table to string, for debug
function dump(o)
    if type(o) == 'table' then
        local s = '{ '
        for k, v in pairs(o) do
            if type(k) ~= 'number' then
                k = '"' .. tostring(k) .. '"'
            end
            s = s .. '[' .. k .. '] = ' .. dump(v) .. ','
        end
        return s .. '} '
    else
        return tostring(o)
    end
end

function pop_prefix(s, prefix, sep_len)
    sep_len = sep_len or 0
    if string.sub(s, 1, #prefix) == prefix then
        return prefix, string.sub(s, #prefix + sep_len + 1)
    else
        return false, s
    end
end
