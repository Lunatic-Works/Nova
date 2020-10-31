--- AnimationEntry wrapper

WrapEntry = {}
WrapEntry.__index = WrapEntry

function WrapEntry:new(o)
    return setmetatable(o or {}, self)
end

function WrapEntry:_then(args)
    if type(args) ~= 'table' then
        args = { property = args }
    end
    return WrapEntry:new {
        entry = self.entry:Then(
            args.property,
            args.duration or 0,
            args.easing,
            args.repeat_num or 0
        )
    }
end

function WrapEntry:_and(args)
    if args == nil then
        args = {}
    elseif type(args) ~= 'table' then
        args = { property = args }
    end

    -- if self has head, the new entry will be head's child
    if self.head then
        if self.head.entry then
            -- self.head is WrapEntry
            return WrapEntry:new {
                entry = self.head.entry:Then(
                    args.property,
                    args.duration or 0,
                    args.easing,
                    args.repeat_num or 0
                )
            }
        else
            -- self.head is WrapAnim
            return WrapEntry:new {
                entry = self.head.anim:Do(
                    args.property,
                    args.duration or 0,
                    args.easing,
                    args.repeat_num or 0
                )
            }
        end
    else
        return WrapEntry:new {
            entry = self.entry:And(
                args.property,
                args.duration or 0,
                args.easing,
                args.repeat_num or 0
            )
        }
    end
end

function WrapEntry:_for(duration)
    self.entry:For(duration)
    return self
end

function WrapEntry:_with(func)
    self.entry:With(func)
    return self
end

function WrapEntry:_repeat(repeat_num)
    self.entry:Repeat(repeat_num)
    return self
end

function WrapEntry:stop()
    self.entry:Stop()
end

--- AnimationEntry wrapper end

--- NovaAnimation wrapper

WrapAnim = {}
WrapAnim.__index = WrapAnim

function WrapAnim:new(o)
    return setmetatable(o or {}, self)
end

function WrapAnim:_do(args)
    if type(args) ~= 'table' then
        args = { property = args }
    end
    return WrapEntry:new {
        entry = self.anim:Do(
            args.property,
            args.duration or 0,
            args.easing,
            args.repeat_num or 0
        )
    }
end

function WrapAnim:stop()
    self.anim:Stop()
end

--- NovaAnimation wrapper end

--- alias for defining methods for both WrapEntry and WrapAnim
WrapAnim._then = WrapAnim._do

function make_anim_method(func_name, func, preload_func, preload_param)
    if WrapEntry[func_name] then
        warn('Duplicate animation method: ' .. func_name)
        return
    end
    WrapEntry[func_name] = func
    WrapAnim[func_name] = func
    if preload_func then
        if preload_param then
            if type(preload_param) == 'table' then
                for i = 1, #preload_param do
                    local item = preload_param[i]
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

--- AnimationProperty wrapper

RELATIVE = Nova.UseRelativeValue.Yes

--- argument can be specified as positional or named
--- named argument will override positional ones
--- positional:
---     name:     index:  type:               description:
---     obj       1       string or userdata  the object to move
---     to        2       Vector3             target value, Euler angle for rotation
---     from      3       Vector3             start value, Euler angle for rotation, default is the value when the animation entry starts
---     relative  3       UseRelativeValue    use RELATIVE to indicate that target value is relative
local function wrap_vec3_property(p)
    return function(t)
        local obj = t.obj or t[1]
        if obj == nil then
            warn('obj == nil')
            return nil
        end
        local transform = obj.transform
        local to = t.to or t[2]
        if to == nil then
            warn('to == nil')
            return nil
        end

        local from = t.from
        local relative = t.relative
        if t[3] == RELATIVE then
            relative = relative or t[3]
        else
            from = from or t[3]
        end

        if from then
            return p(transform, from, to)
        elseif relative then
            return p(transform, to, relative)
        else
            return p(transform, to)
        end
    end
end

_move = wrap_vec3_property(Nova.PositionAnimationProperty)
_rotate = wrap_vec3_property(Nova.RotationAnimationProperty)
_scale = wrap_vec3_property(Nova.ScaleAnimationProperty)

--- argument can be specified as positional or named
--- named argument will override positional ones
--- positional:
---     name:   index:  type:     description:
---     action  1       function  action to be done
function _action(t)
    local action = t.action or t[1]
    if action == nil then
        warn('action == nil')
        return nil
    end
    return Nova.ActionAnimationProperty(action)
end

--- argument can be specified as positional or named
--- named argument will override positional ones
--- positional:
---     name:     index:  type:     description:
---     mat       1       Material  the material to animate
---     name      2       string    name of the material property
---     to        3       Vector3   target value
---     from      4       Vector3   start value, default is the value when the animation entry starts
function _change_mat_float(t)
    local mat = t.mat or t[1]
    if mat == nil then
        warn('mat == nil')
        return nil
    end
    local name = t.name or t[2]
    local to = t.to or t[3]
    if to == nil then
        warn('to == nil')
        return nil
    end
    local from = t.from or t[4]

    if from then
       return Nova.MaterialFloatAnimationProperty(mat, name, from, to)
    else
       return Nova.MaterialFloatAnimationProperty(mat, name, to)
    end
end

--- AnimationProperty wrapper end
