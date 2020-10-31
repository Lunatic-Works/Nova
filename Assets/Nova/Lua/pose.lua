local poses = {
    ['ergong'] = {
        ['normal'] = {'body', 'mouth_smile', 'eye_normal', 'eyebrow_normal', 'hair'},
    },
    ['gaotian'] = {
        ['normal'] = {'body', 'mouth_smile', 'eye_normal', 'eyebrow_normal', 'hair'},
        ['cry'] = {'body', 'mouth_smile', 'eye_cry', 'eyebrow_normal', 'hair'},
    },
    ['qianye'] = {
        ['normal'] = {'body', 'mouth_close', 'eye_normal', 'eyebrow_normal', 'hair'},
    },
    ['xiben'] = {
        ['normal'] = {'body', 'mouth_close', 'eye_normal', 'eyebrow_normal', 'hair'},
    },
}

function get_pose(obj, pose_name)
    if poses[obj.luaGlobalName] and poses[obj.luaGlobalName][pose_name] then
        return poses[obj.luaGlobalName][pose_name]
    end
    return nil
end
