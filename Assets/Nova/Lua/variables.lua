function get_nova_variable(name, global)
    local entry
    if global then
        entry = __Nova.checkpointHelper:GetGlobalVariable(name)
    else
        entry = __Nova.variables:Get(name)
    end

    if entry == nil then
        return nil
    else
        return entry.value
    end
end

function set_nova_variable(name, value, global)
    local obj, func
    if global then
        obj = __Nova.checkpointHelper
        func = obj.SetGlobalVariable
    else
        obj = __Nova.variables
        func = obj.Set
    end

    local _type = type(value)
    if value == nil then
        func(obj, name, Nova.VariableType.String, nil)
    elseif _type == 'boolean' then
        func(obj, name, Nova.VariableType.Boolean, value)
    elseif _type == 'number' then
        func(obj, name, Nova.VariableType.Number, value)
    elseif _type == 'string' then
        func(obj, name, Nova.VariableType.String, value)
    else
        warn('Variable can only be boolean, number, string, or nil, but found ' .. _type .. ': ' .. dump(value))
    end
end
