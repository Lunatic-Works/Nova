function input_on()
    __Nova.inputHelper:EnableInput()
end

function input_off()
    __Nova.inputHelper:DisableInput()
end

function click_forward_on()
    __Nova.dialogueBoxController.canClickForward = true
end

function click_forward_off()
    __Nova.dialogueBoxController.canClickForward = false
end

function click_abort_anim_on()
    __Nova.dialogueBoxController.scriptCanAbortAnimation = true
end

function click_abort_anim_off()
    __Nova.dialogueBoxController.scriptCanAbortAnimation = false
end
