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
function jump_to(dest)
    __Nova.scriptLoader:RegisterJump(dest)
end

--- add branches to the current node
--- branches should be a list of 'branch'
--- a branch is a table with the following structure:
--- {
---    dest = 'name of the destination node',
---    text = 'text on the button to select this branch', should not use if mode is jump
---    mode = 'normal|jump|show|enable', optional, default is normal
---    cond = a function that returns a bool, should not use if mode is show, optional if mode is jump
--- }
--- this method can be only called once for each node. i.e. all branches of the node should be added at once
function branch(branches)
    for i, branch in ipairs(branches) do
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
            warn('Unknown branch mode: ' .. tostring(branch.mode) .. ', text: ' .. tostring(text))
            return
        end
        __Nova.scriptLoader:RegisterBranch(tostring(i), branch.dest, branch.text, mode, branch.cond)
    end
    __Nova.scriptLoader:EndRegisterBranch()
end

--- set the current node as a start point of the game
--- should be called after the node has been defined
--- a flowchart tree CAN have multiple entrance points, which means this method can be called several times under
--- different nodes
--- a start point can have a name. If no name is given, the name of the current node will be used
--- the name of the start point should be unique among that of all the start points
function is_start(name)
    __Nova.scriptLoader:SetCurrentAsStartUpNode(name)
end

--- set the current node as the default start point
--- a game can have only one default start point. This function CANNOT be called under different nodes
--- the meaning of the parameter name is the same as that of is_start()
function is_default_start(name)
    __Nova.scriptLoader:SetCurrentAsDefaultStart(name)
end

--- set the current node as an end
--- an end can have a name, different ends should have different names
--- a node can only have one end name, and an end name can only refer to one node
--- if no name is given, the name of the current node will be used
--- all nodes without child node should be marked as an end. If is_end() is not called under such nodes, those nodes
--- will be marked as ends automatically
function is_end(name)
    __Nova.scriptLoader:SetCurrentAsEnd(name)
end

--- get GameObject
--- caching everything might cause memory leak, so this method will do no cache
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
        warn('entry not found')
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

local function try_to_number(x)
    local x_number = tonumber(x)
    if x_number then
        return x_number
    else
        return x
    end
end

-- access variable at run time (lazy only)
-- value should not be nil
-- get: v(name)
-- set: v(name, value)
function v(name, value)
    if value == nil then
        return try_to_number(__Nova.variables:Get(name))
    else
        __Nova.variables:Set(name, value)
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
