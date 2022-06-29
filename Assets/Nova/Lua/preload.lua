add_preload_pattern = Nova.ScriptDialogueEntryParser.AddPattern
add_preload_pattern_with_obj = Nova.ScriptDialogueEntryParser.AddPatternWithObject
add_preload_pattern_for_table = Nova.ScriptDialogueEntryParser.AddPatternForTable
add_preload_pattern_with_obj_for_table = Nova.ScriptDialogueEntryParser.AddPatternWithObjectForTable
add_preload_pattern_with_obj_and_res = Nova.ScriptDialogueEntryParser.AddPatternWithObjectAndResource

function preload(obj, resource_name)
    if obj == nil then
        warn('Preload obj == nil', resource_name)
    end

    if obj == 'Texture' then
        Nova.AssetLoader.Preload(Nova.AssetCacheType.Image, resource_name)
        return
    end

    local _type = obj:GetType()
    if _type == typeof(Nova.PrefabLoader) or _type == typeof(Nova.TimelineController) then
        Nova.AssetLoader.Preload(Nova.AssetCacheType.Prefab, obj.prefabFolder .. '/' .. resource_name)
    elseif _type == typeof(Nova.AudioController) then
        obj:Preload(resource_name)
    else
        if type(resource_name) == 'table' then
            -- dont preload pose
        else
            local pose = get_pose(obj, resource_name)
            if pose then
                -- dont preload pose
            else
                Nova.AssetLoader.Preload(Nova.AssetCacheType.Image, obj.imageFolder .. '/' .. resource_name)
            end
        end
    end
end

function unpreload(obj, resource_name)
    if obj == nil then
        warn('Unpreload obj == nil', resource_name)
    end

    if obj == 'Texture' then
        Nova.AssetLoader.Unpreload(Nova.AssetCacheType.Image, resource_name)
        return
    end

    local _type = obj:GetType()
    if _type == typeof(Nova.PrefabLoader) or _type == typeof(Nova.TimelineController) then
        Nova.AssetLoader.Unpreload(Nova.AssetCacheType.Prefab, obj.prefabFolder .. '/' .. resource_name)
    elseif _type == typeof(Nova.AudioController) then
        obj:Unpreload(resource_name)
    else
        if type(resource_name) == 'table' then
            -- dont preload pose
        else
            local pose = get_pose(obj, resource_name)
            if pose then
                -- dont preload pose
            else
                Nova.AssetLoader.Unpreload(Nova.AssetCacheType.Image, obj.imageFolder .. '/' .. resource_name)
            end
        end
    end
end

function need_preload(obj, resource_name)
end
add_preload_pattern('need_preload')
