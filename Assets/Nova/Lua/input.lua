function input_on()
    __Nova.inputHelper:EnableInput()
end

function input_off()
    stop_auto_ff()
    __Nova.inputHelper:DisableInput()
end

function wait_fence()
    while __Nova.coroutineHelper.fence == nil do
        coroutine.step()
    end
    return __Nova.coroutineHelper:TakeFence()
end

local function check_lazy_not_before(name)
    if __Nova.executionContext.mode ~= Nova.ExecutionMode.Lazy then
        error(name .. ' should only be called in lazy execution blocks')
        return false
    end
    if __Nova.executionContext.stage == Nova.DialogueActionStage.BeforeCheckpoint then
        error(name .. ' should not be called in BeforeCheckpoint stage')
        return false
    end
    return true
end

function minigame_begin()
    if not check_lazy_not_before('minigame_begin') then
        return
    end
    input_off()
end

function minigame_end()
    if not check_lazy_not_before('minigame_end') then
        return
    end
    wait_fence()
    __Nova.coroutineHelper:SaveInterrupt()
    input_on()
end
