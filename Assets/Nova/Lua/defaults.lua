---
--- Created by huisedenanhai.
--- DateTime: 2018/7/15 3:09 PM
---

local scriptLoader = nil

function bindScriptLoader(sl)
    scriptLoader = sl
end

--- define label
function label(name, description)
    scriptLoader:RegisterNewNode(name, description)
end

--- jump to the given destination
function jump_to(destination)
    scriptLoader:RegisterJump(destination)
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
        scriptLoader:RegisterBranch(branch.name, branch.destination, branch.metadata)
    end
    scriptLoader:EndRegisterBranch()
end

