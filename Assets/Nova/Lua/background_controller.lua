---
--- Created by L.
--- DateTime: 2018/7/21 1:10 AM
---

require 'built_in'

function show_background(imageName)
    __Nova.backgroundController:SetImage(imageName)
end

function clear_background()
    __Nova.backgroundController:ClearImage()
end

function background_fade_from(from_alpha, time)
    iTween.FadeFrom(__Nova.backgroundController.gameObject, from_alpha, time)
end