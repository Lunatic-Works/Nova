-- jump (lazy only)
function jmp(to)
    __Nova.advancedDialogueHelper:Jump(to)
end

-- override next dialogue text (lazy only)
function override_text(to)
    __Nova.advancedDialogueHelper:Override(to)
end
