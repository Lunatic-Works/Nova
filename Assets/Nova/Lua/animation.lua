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
    return AnimationEntry:new {
        entry = self.entry:Then(
            args.property,
            args.duration or 0,
            args.easing,
            args.repeat_num or 0
        )
    }
end

function AnimationEntry:_and(args)
    if args == nil then
        args = {}
    elseif type(args) ~= 'table' then
        args = { property = args }
    end

    if self.head then
        -- self has head, so the new entry will be head's child
        if self.head.entry then
            -- self.head is AnimationEntry
            return AnimationEntry:new {
                entry = self.head.entry:Then(
                    args.property,
                    args.duration or 0,
                    args.easing,
                    args.repeat_num or 0
                )
            }
        else
            -- self.head is NovaAnimation
            return AnimationEntry:new {
                entry = self.head.anim:Do(
                    args.property,
                    args.duration or 0,
                    args.easing,
                    args.repeat_num or 0
                )
            }
        end
    else
        -- self doesn't have head, so the new entry will be self's child
        return AnimationEntry:new {
            entry = self.entry:And(
                args.property,
                args.duration or 0,
                args.easing,
                args.repeat_num or 0
            )
        }
    end
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

function NovaAnimation:_do(args)
    if type(args) ~= 'table' then
        args = { property = args }
    end
    return AnimationEntry:new {
        entry = self.anim:Do(
            args.property,
            args.duration or 0,
            args.easing,
            args.repeat_num or 0
        )
    }
end

function NovaAnimation:stop()
    self.anim:Stop()
end

--- NovaAnimation wrapper end

--- alias for defining methods for both AnimationEntry and NovaAnimation
NovaAnimation._then = NovaAnimation._do

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
