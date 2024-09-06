-- Animation groups are only used in holding animations,
-- so they don't need to get restored
local anim_groups = {}
local cs_entry_to_group_name = {}

--- AnimationEntry wrapper

AnimationEntry = {}
AnimationEntry.__index = AnimationEntry

function AnimationEntry:new(o)
    return setmetatable(o or {}, self)
end

function AnimationEntry:_then(args)
    if type(args) ~= 'table' then
        args = { property = args }
    end

    local group_name = self.group_name
    local entry = AnimationEntry:new {
        entry = self.entry:Then(
            args.property,
            args.duration or 0,
            args.easing,
            args.repeat_num or 0
        ),
        group_name = group_name
    }

    if group_name then
        local cs_entry = entry.entry
        table.insert(anim_groups[group_name], cs_entry)
        cs_entry_to_group_name[cs_entry] = group_name
    end

    return entry
end

function AnimationEntry:_and(args)
    if args == nil then
        args = {}
    elseif type(args) ~= 'table' then
        args = { property = args }
    end

    local head, group_name, entry
    if self.head then
        head = self.head
        group_name = head.group_name
        entry = AnimationEntry:new {
            entry = head.entry:Then(
                args.property,
                args.duration or 0,
                args.easing,
                args.repeat_num or 0
            ),
            group_name = group_name
        }
    else
        head = self
        group_name = head.group_name
        entry = AnimationEntry:new {
            entry = head.entry:And(
                args.property,
                args.duration or 0,
                args.easing,
                args.repeat_num or 0
            ),
            group_name = group_name
        }
    end

    if group_name then
        local cs_entry = entry.entry
        table.insert(anim_groups[group_name], cs_entry)
        cs_entry_to_group_name[cs_entry] = group_name
    end

    return entry
end

function AnimationEntry:_for(duration)
    self.entry:For(duration)
    return self
end

function AnimationEntry:_with(func)
    self.entry:With(func)
    return self
end

function AnimationEntry:_repeat(repeat_num)
    self.entry:Repeat(repeat_num)
    return self
end

function AnimationEntry:stop()
    self.entry:Stop()
end

--- AnimationEntry wrapper end

--- NovaAnimation wrapper

NovaAnimation = {}
NovaAnimation.__index = NovaAnimation

function NovaAnimation:new(o)
    return setmetatable(o or {}, self)
end

NovaAnimation._then = AnimationEntry._then

function NovaAnimation:stop()
    if not self.group_name then
        self.entry:Stop()
    else
        -- New animation entries may be created in Stop()
        while true do
            local entry = anim_groups[self.group_name][1]
            if entry == nil then
                break
            end
            entry:Stop()
        end
    end
end

--- NovaAnimation wrapper end

function make_anim_method(func_name, func, preload_func, preload_param)
    if AnimationEntry[func_name] then
        warn('Duplicate animation method: ' .. func_name)
        return
    end

    AnimationEntry[func_name] = func
    NovaAnimation[func_name] = func

    if preload_func then
        if preload_param then
            if type(preload_param) == 'table' then
                for _, item in ipairs(preload_param) do
                    if type(item) == 'table' then
                        preload_func(func_name, unpack(item))
                    else
                        preload_func(func_name, item)
                    end
                end
            else
                preload_func(func_name, preload_param)
            end
        else
            preload_func(func_name)
        end
    end
end

RELATIVE = Nova.UseRelativeValue.Yes

function named_anim_hold(group_name)
    if not check_anim_hold() then
        error('named_anim_hold should only be called in holding animation')
    end

    anim_groups[group_name] = anim_groups[group_name] or {}
    return NovaAnimation:new { entry = anim_hold.entry, group_name = group_name }
end

function remove_anim_entry(cs_entry)
    local group_name = cs_entry_to_group_name[cs_entry]
    if group_name then
        -- When removing an entry, we need to maintain the order of other entries
        -- TODO: Use a queue for better performance
        table.delete(anim_groups[group_name], cs_entry)
        cs_entry_to_group_name[cs_entry] = nil
    end
end

function clear_anim_groups()
    for k, v in pairs(anim_groups) do
        if #v > 0 then
            warn('Animation group is non empty: ' .. k)
        end
    end

    anim_groups = {}
end
