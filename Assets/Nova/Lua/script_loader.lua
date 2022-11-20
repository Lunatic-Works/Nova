only_included_scenario_names = {}

local current_filename
local last_display_name

function action_new_file(filename)
    current_filename = filename
    last_display_name = nil
end

local function check_eager(name)
    if __Nova.executionContext.mode ~= Nova.ExecutionMode.Eager then
        error(name .. ' should only be called in eager execution blocks')
        return false
    end
    return true
end

local function try_get_local_name(name)
    if name == nil then
        return nil
    end

    local prefix
    prefix, name = pop_prefix(name, 'l_')
    if prefix then
        name = current_filename .. ':' .. name
    end
    return name
end

--- define a node
function label(name, display_name)
    if not check_eager('label') then
        return
    end

    if display_name == nil then
        if last_display_name == nil then
            display_name = name
        else
            display_name = last_display_name
        end
    else
        last_display_name = display_name
    end

    name = try_get_local_name(name)

    if __Nova.scriptLoader.stateLocale == Nova.I18n.DefaultLocale then
        __Nova.scriptLoader:RegisterNewNode(name, display_name)
    else
        __Nova.scriptLoader:AddLocalizedNode(name, display_name)
    end
end

--- jump to the given destination
--- should be called at the end of the node
function jump_to(dest)
    if not check_eager('jump_to') then
        return
    end
    dest = try_get_local_name(dest)
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
    if not check_eager('branch') then
        return
    end

    for i, branch in ipairs(branches) do
        local name = tostring(i)

        local dest = try_get_local_name(branch.dest)

        local image_info = nil
        if branch.image then
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
            warn('Unknown branch mode: ' .. dump(branch.mode) .. ', text: ' .. dump(branch.text))
            return
        end

        local cond = branch.cond
        if type(cond) == 'string' then
            cond = loadstring('return ' .. cond)
        end

        if __Nova.scriptLoader.stateLocale == Nova.I18n.DefaultLocale then
            __Nova.scriptLoader:RegisterBranch(name, dest, branch.text, image_info, mode, cond)
        else
            __Nova.scriptLoader:AddLocalizedBranch(name, dest, branch.text)
        end
    end
    __Nova.scriptLoader:EndRegisterBranch()
end

--- set the current node as a start node
--- should be called at the end of the node
--- a game can have multiple start points, which means this function can be called several times under
--- different nodes
function is_start()
    if not check_eager('is_start') then
        return
    end
    __Nova.scriptLoader:SetCurrentAsStart()
end

--- set the current node as a start point which is unlocked when running the game for the first time
--- should be called at the end of the node
--- indicates is_start()
function is_unlocked_start()
    if not check_eager('is_unlocked_start') then
        return
    end
    __Nova.scriptLoader:SetCurrentAsUnlockedStart()
end

function is_debug()
    if not check_eager('is_debug') then
        return
    end
    __Nova.scriptLoader:SetCurrentAsDebug()
end

--- set the current node as an end node
--- should be called at the end of the node
--- a name can be assigned to an end point, which can differ from the node name
--- the name should be unique among all end point names
--- if no name is given, the name of the current node will be used
--- all nodes without child nodes should be marked as end nodes
--- if is_end() is not called under those nodes, they will be marked as end nodes automatically
function is_end(name)
    if not check_eager('is_end') then
        return
    end
    name = try_get_local_name(name)
    __Nova.scriptLoader:SetCurrentAsEnd(name)
end

function text_need_interpolate(s)
    return string.find(s, '{{([^}]+)}}') ~= nil
end

function interpolate_text(s)
    return string.gsub(s, '{{([^}]+)}}', function(x)
            return _G[x]
        end)
end

function is_restoring()
    return __Nova.executionContext.isRestoring
end
