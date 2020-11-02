# 目录结构

如果想体验示例作品，可以直接打开`Assets/Examples/Colorless/Scenes/Main.unity`

* `Assets/Examples/Colorless/`：示例作品《Colorless》，正常制作游戏时可以把这个文件夹下的东西放到`Assets/`下
    * `Prefabs/`：这部作品的`GameController.prefab`（`GameController`是我们的一个历史遗留问题，当时的设计是用来放多个scene都要使用的东西，现在看来只要一个scene就够了）
    * `Resources/Colorless/`：这部作品的资源文件，在`Resources`下面建一个`Colorless`文件夹有利于防止资源的名字跟外面冲突
        * `Backgrounds/`：背景图片（如果需要前景等其他图片，可以单独放一个文件夹）
        * `BGM/`：背景音乐
        * `Scenarios/`：剧本
        * `Sounds/`：音效
        * `Standings/`：立绘部件，每名角色各有一个文件夹
        * `Voices/`：语音，每名角色各有一个文件夹
    * `Scenes/`：场景，只需要一个`Main.unity`
    * `StandingsUncropped/`：未裁剪的立绘部件，Nova提供了裁剪空白部分的功能
* `Assets/Nova/`：Nova使用的文件
    * `CGInc/`：shader公用的代码
    * `Core/`：核心部分的代码
    * `Editor/`：在Unity Editor中使用的代码
    * `Exceptions/`
    * `Fonts/`：字体，包括Textmesh Pro使用的font asset和material preset，以及生成font asset时使用的字符集，字符集由`Tools/Resources/generate_charsets.py`生成
    * `Generate/`：生成的代码
    * `Lua/`
    * `Prefabs/`
    * `Resources/`
        * `Locales/`：翻译UI使用的配置文件
        * `Masks/`：转场使用的遮罩
        * `Shaders/`：转场和特效使用的shader，由`Tools/Resources/generate_shaders.py`生成
    * `Scripts/`：前端部分的代码
    * `Settings/`：游戏设置使用的配置文件
    * `ShaderProtos/`：为了方便生成一个shader的多种版本而编写的文件
    * `ThirdParty/`：第三方库
    * `UI/`
* `Assets/Resources/Lua/`：tolua#生成的文件
* `Tools/`：制作游戏时提供方便的Python脚本
