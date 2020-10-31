local gc_scheduled = false

function schedule_gc()
    gc_scheduled = true
end

function try_gc()
    if gc_scheduled and not anim_persist_begun then
        -- TODO: UnloadUnusedAssets() is too slow at present, maybe we can
        -- uncomment it after using incremental GC
        -- Nova.AssetLoader.UnloadUnusedAssets()
        gc_scheduled = false
    end
end
add_action_before_lazy_block(try_gc)

function force_gc()
    Nova.AssetLoader.UnloadUnusedAssets()
    gc_scheduled = false
end
