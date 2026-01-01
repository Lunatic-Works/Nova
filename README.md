# Nova（能叶）

![Nova banner](https://github.com/Lunatic-Works/Nova/wiki/img/nova_banner.png)

基于Unity，对程序员友好的视觉小说（VN）/文字冒险游戏（AVG）框架

## 使用说明

1. `git clone`，为了节约时间可以加上`--filter=blob:none`
2. 将`ProjectSettings/ProjectVersion.txt`中的Unity版本号改为你的版本号
3. 在Unity Editor中打开`Assets/Scenes/Main.unity`，运行游戏，把示例作品《Colorless》看一遍
4. 把游戏中的教程看一遍
    * 同时可以试着改一改脚本，脚本是`Assets/Resources/Scenarios/`文件夹下的`tut01.txt`等文件
    * 在章节选择界面按Ctrl可以看到更多测试用的脚本
5. 如果你想修改Colorless的脚本，需要先删除英文版的脚本，否则中文与英文的脚本会对不上
    * `Assets/Resources/LocalizedResources/English/Scenarios/`文件夹下是英文版的脚本，可以全部删除
    * `Assets/Resources/LocalizedResourcePaths.txt`用来记录英文版用到的文件，可以删除
6. 其他资料可以参考[GitHub Wiki](https://github.com/Lunatic-Works/Nova/wiki/)
7. 遇到问题可以在issue里提

## 常见问题

* **网上已经有很多galgame引擎/框架了，Nova与它们的差异在哪里？**

    这篇文章介绍了设计思路：https://zhuanlan.zhihu.com/p/272466277

* **支持什么版本的Unity、什么操作系统/平台？**

    支持Unity 2020到2022。Unity 6需要用兼容模式，如果你想迁移到render graph，欢迎来帮忙。

    支持Windows/macOS/Android/iOS平台，我们已经有作品在Google Play、TapTap、App Store等平台上架。

    Linux版可以在Steam Deck上运行，但是不一定支持其他Linux发行版，因为Linux的图形界面太多了，如果遇到问题欢迎提issue。

    理论上支持WebGL，但是还需要更多测试。在WebGL平台上，ToLua# native plugin用的是Lua 5.1.5而不是LuaJIT，某些行为会有差异。如果你想把LuaJIT编译到WebGL，欢迎来帮忙。

    支持上传到IPFS，比如 https://ipfs.io/ipfs/bafybeiddftgjosnsmhux62wxkwuzabzoi64735iknml5q3uiq2qrqghjj4/

    理论上可以用团结引擎编译到OpenHarmony，需要重新编译ToLua# native plugin。

* **可以把解谜/战棋等游戏加到Nova里吗？可以把Nova作为对话系统加到解谜/战棋等游戏里吗？**

    绝大多数Unity能做的gameplay都可以加到Nova里，但是把Nova加到其他游戏里会比较困难。目前Nova的定位是“框架/模板”，而不是“插件/扩展包”。

    Nova的整个对话和存档系统基本上是围绕着“随时回跳到之前的任何一句对话”这个功能做的。如果你的游戏不需要这个功能，那你可能并不需要Nova。为了保证这个功能，把一些gameplay加到Nova里也会比较困难。

* **可以自定义UI吗？**

    你可以用任何在Unity里做UI的方法来自定义Nova的UI。我们认为每部作品往往都需要根据自己的主题来自定义UI，所以Nova只提供了一套非常朴素的默认UI。

* **Unity已死，Godot万岁！**

    [Nova2](https://github.com/Lunatic-Works/Nova2/issues/1)在做了，欢迎来帮忙！

## 版本说明

* v0.1：兼容《青箱》v1.1.0
* v0.2：兼容Unity 2019，重要的新功能包括异步的`GameState`、小游戏支持、新的脚本解析系统、新的存档系统
* v0.3：兼容Unity 2020，重要的新功能包括URP、新的立绘合成系统、Input System
* v0.4：`master` branch上滚动更新，重要的新功能包括新的对话框，预计的新功能包括Addressables

## 友情链接

* 我们的微博：[@LunaticWorks](https://weibo.com/LunaticWorks)
* 我们的QQ群：876212259，如果以后讨论程序的人多了可能会再开一个程序群

使用Nova的作品：

* [青箱](https://store.steampowered.com/app/1131740)
* [东北之夏](https://store.steampowered.com/app/2121360)
* [初夏倾语](https://store.steampowered.com/app/2075410)
* [溢爱](https://store.steampowered.com/app/2663600)
* [完美恋人](https://store.steampowered.com/app/2773520)
* [机械恋心](https://store.steampowered.com/app/2980050)
* [黄油罐头](https://store.steampowered.com/app/3713200)
* [水鬼](https://store.steampowered.com/app/3111010)

如果你用Nova做出了作品，欢迎来告诉我们，我们可以互相宣传一下

开发工具：

* [VS Code扩展](https://github.com/Lunatic-Works/vscode-nova-script)

本框架的依赖：

* [ToLua#](https://github.com/topameng/tolua)
* [Newtonsoft Json](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.0/manual/index.html)
* [Loop Scroll Rect](https://github.com/qiankanglai/LoopScrollRect)

也可以看一看其他的galgame引擎：

* [Ren'Py](https://github.com/renpy/renpy)：如果你没有编程基础但是想学，或者有Python基础，推荐用这个引擎
* [WebGAL](https://github.com/MakinoharaShoko/WebGAL)：如果你信仰web，推荐用这个引擎
* [Ayaka](https://github.com/Uni-Gal/Ayaka)：如果你信仰Rust，可以试试这个引擎
* [VoidNovelEngine](https://github.com/VoidmatrixHeathcliff/VoidNovelEngine)：自由、现代化的视觉小说引擎

以及通用游戏引擎：

* [SakuraEngine](https://github.com/SakuraEngine/SakuraEngine)：为高性能而生的游戏运行时与工具箱
* [PainterEngine](https://github.com/matrixcascade/PainterEngine)：由C语言编写的跨平台图形引擎

以及

* [UniGal](https://github.com/Uni-Gal/UniGal-Script)：为了解决各家视觉小说引擎的碎片化问题，而定义的通用脚本格式
* [Yukimi Script](https://github.com/Strrationalism/YukimiScript)：为描述视觉小说而设计的领域专用语言
* [语涵编译器](https://github.com/PrepPipe/preppipe-python)：视觉小说的编译器框架

国内镜像（随缘更新）：

* https://gitee.com/Lunatic-Works/Nova

国内的小伙伴也可以用各种GitHub镜像网站来访问这个repo
