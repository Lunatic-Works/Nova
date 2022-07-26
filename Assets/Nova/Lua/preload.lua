-- TODO: preload video, avatar, branch image

add_preload_pattern = Nova.ScriptDialogueEntryParser.AddPattern
add_preload_pattern_with_obj = Nova.ScriptDialogueEntryParser.AddPatternWithObject
add_preload_pattern_for_table = Nova.ScriptDialogueEntryParser.AddPatternForTable
add_preload_pattern_with_obj_for_table = Nova.ScriptDialogueEntryParser.AddPatternWithObjectForTable
add_preload_pattern_with_obj_and_res = Nova.ScriptDialogueEntryParser.AddPatternWithObjectAndResource

function preload(obj, resource_name)
    if obj == nil then
        warn('Preload obj == nil, resource ' .. dump(resource_name))
        return
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
    elseif _type == typeof(Nova.SpriteController) then
        Nova.AssetLoader.Preload(Nova.AssetCacheType.Image, obj.imageFolder .. '/' .. resource_name)
    elseif _type == typeof(Nova.GameCharacterController) then
        local pose = get_pose(obj, resource_name)
        obj:Preload(Nova.AssetCacheType.Standing, pose)
    elseif _type == typeof(Nova.OverlaySpriteController) then
        local pose = get_pose(obj, resource_name)
        obj:Preload(Nova.AssetCacheType.Image, pose)
    else
        warn('Unknown obj ' .. dump(obj) .. 'to preload, resource ' .. dump(resource_name))
    end
end

function unpreload(obj, resource_name)
    if obj == nil then
        warn('Unpreload obj == nil, resource ' .. dump(resource_name))
        return
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
    elseif _type == typeof(Nova.SpriteController) then
        Nova.AssetLoader.Unpreload(Nova.AssetCacheType.Image, obj.imageFolder .. '/' .. resource_name)
    elseif _type == typeof(Nova.GameCharacterController) then
        local pose = get_pose(obj, resource_name)
        obj:Unpreload(Nova.AssetCacheType.Standing, pose)
    elseif _type == typeof(Nova.OverlaySpriteController) then
        local pose = get_pose(obj, resource_name)
        obj:Unpreload(Nova.AssetCacheType.Image, pose)
    else
        warn('Unknown obj ' .. dump(obj) .. 'to preload, resource ' .. dump(resource_name))
    end
end

function need_preload(obj, resource_name)
end
add_preload_pattern('need_preload')
