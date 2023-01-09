function input_on()
    __Nova.inputHelper.inputEnabled = true
end

function input_off()
    stop_auto_ff()
    __Nova.inputHelper.inputEnabled = false
end
