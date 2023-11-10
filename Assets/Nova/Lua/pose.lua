local poses = {
    ['ergong'] = {
        ['normal'] = 'body+mouth_smile+eye_normal+eyebrow_normal+hair',
    },
    ['gaotian'] = {
        ['normal'] = 'body+mouth_smile+eye_normal+eyebrow_normal+hair',
        ['cry'] = 'body+mouth_smile+eye_cry+eyebrow_normal+hair',
    },
    ['qianye'] = {
        ['normal'] = 'body+mouth_close+eye_normal+eyebrow_normal+hair',
    },
    ['xiben'] = {
        ['normal'] = 'body+mouth_close+eye_normal+eyebrow_normal+hair',
    },

    ['cg'] = {
        ['rain'] = 'rain_back',
        ['rain_final'] = 'rain_back+rain_text',
    },
}

function get_all_poses_by_name(obj_name)
    local ret = {}
    if poses[obj_name] then
        for k, _ in pairs(poses[obj_name]) do
            ret[#ret + 1] = k
        end
        table.sort(ret)
    end
    return ret
end

function get_pose_by_name(obj_name, pose_name)
    -- Not alias
    if string.find(pose_name, '+') then
        return pose_name
    end

    local pose = poses[obj_name] and poses[obj_name][pose_name]
    if pose then
        return pose
    end

    warn('Unknown pose ' .. dump(pose_name) .. ' for composite sprite ' .. dump(obj_name))
    return pose_name
end

function get_pose(obj, pose_name)
    return get_pose_by_name(obj.luaGlobalName, pose_name)
end
