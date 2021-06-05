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
    elseif obj == __Nova.timelineController then
        Nova.AssetLoader.Preload(Nova.AssetCacheType.Timeline, obj.timelinePrefabFolder .. '/' .. resource_name)
    elseif tostring(obj:GetType()) == 'Nova.AudioController' then
        obj:Preload(resource_name)
    else
        if type(resource_name) == 'table' then
            obj:PreloadPose(resource_name)
        else
            local pose = get_pose(obj, resource_name)
            if pose then
                obj:PreloadPose(pose)
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
    elseif obj == __Nova.timelineController then
        Nova.AssetLoader.Unpreload(Nova.AssetCacheType.Timeline, obj.timelinePrefabFolder .. '/' .. resource_name)
    elseif tostring(obj:GetType()) == 'Nova.AudioController' then
        obj:Unpreload(resource_name)
    else
        if type(resource_name) == 'table' then
            obj:UnpreloadPose(resource_name)
        else
            local pose = get_pose(obj, resource_name)
            if pose then
                obj:UnpreloadPose(pose)
            else
                Nova.AssetLoader.Unpreload(Nova.AssetCacheType.Image, obj.imageFolder .. '/' .. resource_name)
            end
        end
    end
end

function need_preload(obj, resource_name)
end
add_preload_pattern('need_preload')
