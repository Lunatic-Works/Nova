function input_on()
    __Nova.InputHelper:DisableInput(false)
end

function input_off()
    __Nova.InputHelper:DisableInput(true)
end

function click_forward_on()
    __Nova.dialogueBoxController.clickForwardAbility = true
end

function click_forward_off()
    __Nova.dialogueBoxController.clickForwardAbility = false
end

function click_abort_anim_on()
    __Nova.dialogueBoxController.scriptAbortAnimationAbility = true
end

function click_abort_anim_off()
    __Nova.dialogueBoxController.scriptAbortAnimationAbility = false
end
