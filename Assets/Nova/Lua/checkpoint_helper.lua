local ckpt_helper = Nova.CheckpointHelper()

anim_persist_begun = false

function anim_persist_begin()
    anim_persist:stop()
    -- TODO: warn when anim_persist lasts for over 100 dialogue entries
    ckpt_helper:RestrainCheckpoint(100, true)
    anim_persist_begun = true
end

function anim_persist_end()
    anim_persist:stop()
    ckpt_helper:RestrainCheckpoint(0, true)
    clear_loop_actions()
    anim_persist_begun = false
end

function ensure_ckpt_on_next_dialogue()
    ckpt_helper:EnsureCheckpointOnNextDialogue()
end
Nova.ScriptDialogueEntryParser.AddCheckpointPattern('anim_persist_begin', 'ensure_ckpt_on_next_dialogue')

function update_global_save()
    ckpt_helper:UpdateGlobalSave()
end
