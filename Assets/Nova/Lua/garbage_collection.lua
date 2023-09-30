local gc_scheduled = false

function schedule_gc()
    gc_scheduled = true
end

function force_gc()
    Nova.AssetLoader.UnloadUnusedAssets()
    gc_scheduled = false
end

add_action_after_lazy_block(function()
    if gc_scheduled and not check_anim_hold() then
        -- TODO: UnloadUnusedAssets() is too slow at present, maybe we can
        -- uncomment it after using incremental GC
        -- Nova.AssetLoader.UnloadUnusedAssets()
        gc_scheduled = false
    end
end)
