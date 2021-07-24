--- Disable implicit global variable declaration
--- See https://www.lua.org/pil/14.2.html
local declared_names = {}

setmetatable(_G, {
    __index = function(t, n)
        if not declared_names[n] then
            warn('Attempt to read undeclared global variable: ' .. n)
        end
        return rawget(t, n)
    end,
    __newindex = function(t, n, v)
        declared_names[n] = true
        rawset(t, n, v)
    end,
})

__Nova = {}

--- show warning without halting the game
function warn(s)
    print('<color=red>' .. s .. '</color>')
end

--- get GameObject
--- caching everything might cause memory leak, so this function will do no cache
--- find GameObject by name might be slow. It is the author's work to decide whether to cache the result or not
function get_go(obj)
    local o
    if type(obj) == 'string' then
        o = __Nova[obj] -- first search Nova default binding table
            or _G[obj] -- then search global variable
            or UnityEngine.GameObject.Find(obj) -- finally find GameObject by name
    elseif type(obj) == 'userdata' then
        o = obj
    end
    if o == nil then
        warn('Cannot find obj: ' .. tostring(obj))
    end
    return o and o.gameObject
end

function remove_entry(t, entry)
    local idx = 0
    for i = 1, #t do
        if t[i] == entry then
            idx = i
            break
        end
    end
    if idx == 0 then
        warn('Entry not found')
        return
    end
    table.remove(t, idx)
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

--- inverse of tostring()
function toboolean(s)
    if s == 'true' then
        return true
    elseif s == 'false' then
        return false
    else
        return nil
    end
end
