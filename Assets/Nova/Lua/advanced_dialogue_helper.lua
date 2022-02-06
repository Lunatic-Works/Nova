-- jump (lazy only)
function jmp(to)
    __Nova.advancedDialogueHelper:Jump(to)
end

-- override next dialogue text (lazy only)
function override_text(to)
    __Nova.advancedDialogueHelper:Override(to)
end

-- automatically execute the next dialogue (lazy only)
function fall_through()
    __Nova.advancedDialogueHelper:FallThrough()
end
