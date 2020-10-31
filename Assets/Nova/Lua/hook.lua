local actions_before_lazy_block = {}
local actions_after_lazy_block = {}

function action_before_lazy_block(chara_name)
    for i = 1, #actions_before_lazy_block do
        actions_before_lazy_block[i](chara_name)
    end
end

function action_after_lazy_block(chara_name)
    for i = 1, #actions_after_lazy_block do
        actions_after_lazy_block[i](chara_name)
    end
end

function add_action_before_lazy_block(func)
    table.insert(actions_before_lazy_block, func)
end

function add_action_after_lazy_block(func)
    table.insert(actions_after_lazy_block, func)
end
