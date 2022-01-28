function input_on()
    __Nova.inputHelper:EnableInput()
end

function input_off()
    stop_auto_ff()
    __Nova.inputHelper:DisableInput()
end
