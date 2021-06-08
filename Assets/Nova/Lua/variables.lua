-- access variable at run time (lazy only)
-- value can be boolean, number, or string
-- get: v(name)
-- set: v(name, value)
function v(name, value)
    if value == nil then
        local entry = __Nova.variables:Get(name)
        if entry == nil then
            return nil
        elseif entry.type == Nova.VariableType.Boolean then
            return toboolean(entry.value)
        elseif entry.type == Nova.VariableType.Number then
            return tonumber(entry.value)
        else -- entry.type == Nova.VariableType.String
            return entry.value
        end
    else
        if type(value) == 'boolean' then
            __Nova.variables:Set(name, Nova.VariableType.Boolean, tostring(value))
        elseif type(value) == 'number' then
            __Nova.variables:Set(name, Nova.VariableType.Number, tostring(value))
        elseif type(value) == 'string' then
            __Nova.variables:Set(name, Nova.VariableType.String, value)
        else
            warn('Variable can only be boolean, number, or string, but found ' .. tostring(value))
        end
    end
end

-- global variable
function gv(name, value)
    if value == nil then
        local entry = __Nova.checkpointHelper:GetGlobalVariable(name)
        if entry == nil then
            return nil
        elseif entry.type == Nova.VariableType.Boolean then
            return toboolean(entry.value)
        elseif entry.type == Nova.VariableType.Number then
            return tonumber(entry.value)
        else -- entry.type == Nova.VariableType.String
            return entry.value
        end
    else
        if type(value) == 'boolean' then
            __Nova.checkpointHelper:SetGlobalVariable(name, Nova.VariableType.Boolean, tostring(value))
        elseif type(value) == 'number' then
            __Nova.checkpointHelper:SetGlobalVariable(name, Nova.VariableType.Number, tostring(value))
        elseif type(value) == 'string' then
            __Nova.checkpointHelper:SetGlobalVariable(name, Nova.VariableType.String, value)
        else
            warn('Variable can only be boolean, number, or string, but found ' .. tostring(value))
        end
    end
end

-- temporary variable, not saved in checkpoints, not calculated in varables hash
local tv_storage = {}

function tv(name, value)
    if value == nil then
        return tv_storage[name]
    else
        tv_storage[name] = value
    end
end
