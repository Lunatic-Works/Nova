---
--- Created by huisedenanhai.
--- DateTime: 2018/7/15 3:09 PM
---

local Nova_scriptLoader = nil

function bindScriptLoader(sl)
    Nova_scriptLoader = sl
end

--- define label
function label(name, description)
    Nova_scriptLoader:RegisterNewNode(name, description)
end

--- jump to the given destination
function jump_to(destination)
    Nova_scriptLoader:RegisterJump(destination)
end

--- a branch need to have the following structure
--- {
---    name = 'the name of this branch',
---    destination = 'the destination label of this branch',
---    metadata = {a table that contains some additional information}
--- }
--- only the metadata field can be omitted

--- add branches to the current chunk
--- branches should be a list of 'branch'
--- this method can be only called once for each label. i.e. all branches should be added at once
function branch(branches)
    for i, branch in ipairs(branches) do
        Nova_scriptLoader:RegisterBranch(branch.name, branch.destination, branch.metadata)
    end
    Nova_scriptLoader:EndRegisterBranch()
end

--- set the current label as the start point of the game
--- should be called after the label has been defined
--- a flowchart tree CAN have multiple entrance points, which means this method can be called several times under
--- different labels
--- a start point can have a name. If no name is given, the name of the current label will be used.
--- the name of the start point should be unique among all the start up points'.
function is_start(name)
    Nova_scriptLoader:SetCurrentAsStarUpNode(name)
end

--- set the current label as a default start point
--- a game can have only one default start point. this function CAN NOT be called under different labels.
--- the meaning of the parameter name is the same as that of is_start
function is_default_start(name)
    Nova_scriptLoader:SetCurrentAsDefaultStart(name)
end 