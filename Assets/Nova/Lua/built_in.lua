---
--- Created by huisedenanhai.
--- DateTime: 2018/7/15 3:09 PM
---

__Nova = {}

--- define label
function label(name, description)
    __Nova.scriptLoader:RegisterNewNode(name, description)
end

--- jump to the given destination
function jump_to(destination)
    __Nova.scriptLoader:RegisterJump(destination)
end

--- a branch needs to have the following structure
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
        __Nova.scriptLoader:RegisterBranch(branch.name, branch.destination, branch.metadata)
    end
    __Nova.scriptLoader:EndRegisterBranch()
end

--- set the current label as the start point of the game
--- should be called after the label has been defined
--- a flowchart tree CAN have multiple entrance points, which means this method can be called several times under
--- different labels
--- a start point can have a name. If no name is given, the name of the current label will be used.
--- the name of the start point should be unique among all the start up points'.
function is_start(name)
    __Nova.scriptLoader:SetCurrentAsStarUpNode(name)
end

--- set the current label as a default start point
--- a game can have only one default start point. this function CAN NOT be called under different labels.
--- the meaning of the parameter name is the same as that of is_start
function is_default_start(name)
    __Nova.scriptLoader:SetCurrentAsDefaultStart(name)
end

--- set the current label as an end
--- an end can have a name, different ends should have different names
--- a label can only have one end name, and an end name can only refer to one label
--- if no name is given, the name of the current label will be used
--- all nodes with out child node should be marked as an end. If is_end is not declared under such labels, these nodes
--- will be marked as ends automatically with the name of label
function is_end(name)
    __Nova.scriptLoader:SetCurrentAsEnd(name)
end