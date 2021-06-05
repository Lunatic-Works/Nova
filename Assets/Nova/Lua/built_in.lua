__Nova = {}

--- show warning without halting the game
function warn(s)
    print('<color=red>' .. s .. '</color>')
end

local last_display_name

function action_new_file()
    last_display_name = nil
end

--- define a node
function label(name, display_name)
    if display_name == nil then
        if last_display_name == nil then
            display_name = name
        else
            display_name = last_display_name
        end
    else
        last_display_name = display_name
    end

    if __Nova.scriptLoader.stateLocale == Nova.I18n.DefaultLocale then
        __Nova.scriptLoader:RegisterNewNode(name)
    else
        __Nova.scriptLoader:BeginAddLocaleForNode(name)
    end

    Nova.I18nHelper.NodeNames:Set(name, __Nova.scriptLoader.stateLocale, display_name)
end

--- jump to the given destination
--- should be called at the end of the node
function jump_to(dest)
    __Nova.scriptLoader:RegisterJump(dest)
end

--- add branches to the current node
--- should be called at the end of the node
--- should be called only once for each node, i.e., all branches of the node should be added at once
--- branches should be a list of 'branch'. A 'branch' is a table with the following structure:
--- {
---    dest = 'name of the destination node'
---    text = 'text on the button', should not use if mode is jump
---    image = {'image_name', {x, y, scale}}, should not use if mode is jump
---    mode = 'normal|jump|show|enable', optional, default is normal
---    cond = a function that returns a bool, should not use if mode is show, optional if mode is jump
--- }
--- if cond is a string, it will be converted to a function returning that expression
function branch(branches)
    for i, branch in ipairs(branches) do
        local image_info = nil
        if branch.image ~= nil then
            local image_name, image_coord = unpack(branch.image)
            local pos_x, pos_y, scale = unpack(image_coord)
            image_info = Nova.BranchImageInformation(image_name, pos_x, pos_y, scale)
        end

        local mode = Nova.BranchMode.Normal
        if branch.mode == nil or branch.mode == 'normal' then
            -- pass
        elseif branch.mode == 'jump' then
            mode = Nova.BranchMode.Jump
        elseif branch.mode == 'show' then
            mode = Nova.BranchMode.Show
        elseif branch.mode == 'enable' then
            mode = Nova.BranchMode.Enable
        else
            warn('Unknown branch mode: ' .. tostring(branch.mode) .. ', text: ' .. tostring(branch.text))
            return
        end

        local cond = branch.cond
        if type(cond) == 'string' then
            cond = load('return ' .. cond)
        end

        __Nova.scriptLoader:RegisterBranch(tostring(i), branch.dest, branch.text, image_info, mode, cond)
    end
    __Nova.scriptLoader:EndRegisterBranch()
end

--- set the current node as a start node
--- should be called at the end of the node
--- a game can have multiple start points, which means this function can be called several times under
--- different nodes
--- a name can be assigned to a start point, which can differ from the node name
--- the name should be unique among all start point names
--- if no name is given, the name of the current node will be used
function is_start(name)
    __Nova.scriptLoader:SetCurrentAsStart(name)
end

--- set the current node as a start point which is unlocked when running the game for the first time
--- should be called at the end of the node
--- indicates is_start()
function is_unlocked_start(name)
    __Nova.scriptLoader:SetCurrentAsUnlockedStart(name)
end

--- set the current node as the default start point
--- should be called at the end of the node
--- a game can have only one default start node, so this function cannot be called under different nodes
--- indicates is_unlocked_start()
function is_default_start(name)
    __Nova.scriptLoader:SetCurrentAsDefaultStart(name)
end

--- set the current node as an end node
--- should be called at the end of the node
--- a name can be assigned to an end point, which can differ from the node name
--- the name should be unique among all end point names
--- if no name is given, the name of the current node will be used
--- all nodes without child nodes should be marked as end nodes
--- if is_end() is not called under those nodes, they will be marked as end nodes automatically
function is_end(name)
    __Nova.scriptLoader:SetCurrentAsEnd(name)
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

-- jump (lazy only)
function jmp(to)
    __Nova.advancedDialogueHelper:Jump(to)
end

-- override next dialogue text (lazy only)
function override_text(to)
    __Nova.advancedDialogueHelper:Override(to)
end
