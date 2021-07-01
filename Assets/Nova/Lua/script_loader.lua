only_included_scenario_names = {}

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
            warn('Unknown branch mode: ' .. tostring(branch.mode) .. ', text: ' .. tostring(branch.text))
            return
        end

        local cond = branch.cond
        if type(cond) == 'string' then
            cond = loadstring('return ' .. cond)
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
