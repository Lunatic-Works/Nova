--------------------------------------------------------------------------------
--      Copyright (c) 2015 - 2016 , 蒙占志(topameng) topameng@gmail.com
--      All rights reserved.
--      Use, modification and distribution are subject to the "MIT License"
--------------------------------------------------------------------------------
if jit then
    if jit.opt then
        jit.opt.start(3)
    end

    -- print("ver"..jit.version_num.." jit: ", jit.status())
    -- print(string.format("os: %s, arch: %s", jit.os, jit.arch))
end

Mathf      = require "UnityEngine.Mathf"
Vector3    = require "UnityEngine.Vector3"
Quaternion = require "UnityEngine.Quaternion"
Vector2    = require "UnityEngine.Vector2"
Vector4    = require "UnityEngine.Vector4"
Color      = require "UnityEngine.Color"
Ray        = require "UnityEngine.Ray"
Bounds     = require "UnityEngine.Bounds"
RaycastHit = require "UnityEngine.RaycastHit"
Touch      = require "UnityEngine.Touch"
LayerMask  = require "UnityEngine.LayerMask"
Plane      = require "UnityEngine.Plane"
Time       = require "UnityEngine.Time"

list       = require "list"

require "event"
require "typeof"
require "System.Timer"
require "System.coroutine"
require "System.ValueType"
