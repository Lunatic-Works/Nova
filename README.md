# Nova（能叶）

![Nova banner](https://github.com/Lunatic-Works/Nova/wiki/img/nova_banner.png)

基于Unity，对程序员友好的视觉小说（VN）/文字冒险游戏（AVG）框架

## 使用说明

1. `git clone`，为了节约时间可以加上`--filter=blob:none`
2. 将`ProjectSettings/ProjectVersion.txt`中的Unity版本号改为你的版本号
3. 在Unity Editor中打开`Assets/Scenes/Main.unity`，运行游戏，把示例作品《Colorless》看一遍
4. 把游戏中的教程看一遍，同时可以试着改一改脚本，脚本是`Assets/Resources/Scenarios/`文件夹下的`tut01.txt`等文件
5. 如果你想修改Colorless的脚本，需要先删除英文版的脚本，否则中文与英文的脚本会对不上
    * `Assets/Resources/LocalizedResources/English/Scenarios/`文件夹下是英文版的脚本，可以全部删除
    * `Assets/Resources/LocalizedResourcePaths.txt`用来记录英文版用到的文件，可以删除
6. 其他资料可以参考[GitHub Wiki](https://github.com/Lunatic-Works/Nova/wiki/)
7. 遇到问题可以在issue里提

## 常见问题

* **网上已经有很多视觉小说引擎/框架了，Nova与它们的差异在哪里？**

    这篇文章介绍了设计思路：https://zhuanlan.zhihu.com/p/272466277

* **支持什么版本的Unity、什么操作系统/平台？**

    支持Unity 2020及更高版本，Windows/Linux/macOS/Android/iOS平台。

    WebGL/微信小程序可以参考[linsyking/Nova-WXM](https://github.com/linsyking/Nova-WXM)，以及一个在线编辑器[linsyking/Nova-online-editor](https://github.com/linsyking/Nova-online-editor)。

* **我可以把解谜/战棋等游戏加到Nova里吗？我可以把Nova作为对话系统加到解谜/战棋等游戏里吗？**

    绝大多数Unity能做的gameplay都可以加到Nova里，但是把Nova加到其他游戏里会比较困难。目前Nova的定位是“框架/模板”，而不是“插件/扩展包”。

* **Unity已死，Godot万岁！**

    我们打算在手头上的一部长篇作品做完之后迁移到Godot，也许会在2024年开始。

## 版本说明

* v0.1：兼容《青箱》v1.1.0的版本
* v0.2：兼容Unity 2019的版本，重要的新功能包括新的脚本parser、新的存档系统、异步的`GameState`
* v0.3：`master` branch上滚动更新的版本，重要的新功能包括Input System、URP，预计的新功能包括Addressables

## 友情链接

如果你在用Nova做你的作品，欢迎来告诉我们，我们可以互相宣传一下

* 我们的第一部视觉小说作品《青箱》：[Steam](https://store.steampowered.com/app/1131740) [知乎](https://www.zhihu.com/question/409724349) [Bangumi](https://bgm.tv/subject/311066) [VNDB](https://vndb.org/v26506)
* 我们的微博：[@LunaticWorks](https://weibo.com/LunaticWorks)
* 我们的QQ群：876212259，如果以后讨论程序的人多了可能会再开一个程序群

也可以看一看其他的视觉小说引擎：

* [Ren'Py](https://github.com/renpy/renpy)：如果你没有编程基础但是想学，或者有Python基础，推荐用这个引擎
* [WebGAL](https://github.com/MakinoharaShoko/WebGAL)：如果你信仰web，推荐用这个引擎
* [Ayaka](https://github.com/Uni-Gal/Ayaka)：如果你信仰Rust，可以试试这个引擎

以及通用游戏引擎：

* [SakuraEngine](https://github.com/SakuraEngine/SakuraEngine)：为高性能而生的游戏运行时与工具箱
* [PainterEngine](https://github.com/matrixcascade/PainterEngine)：由C语言编写的跨平台图形应用框架
* [EtherEngine](https://github.com/EtherProject/EtherEngine)：基于Lua的跨平台游戏接口

以及

* [UniGal](https://github.com/Uni-Gal/UniGal-Script)：为了解决各家视觉小说引擎的碎片化问题，而定义的通用脚本格式
* [Yukimi Script](https://github.com/Strrationalism/YukimiScript)：为描述视觉小说而设计的领域专用语言

本框架的依赖：

* [ToLua#](https://github.com/topameng/tolua)
* [Newtonsoft Json](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.0/manual/index.html)
* [Loop Scroll Rect](https://github.com/qiankanglai/LoopScrollRect)

国内镜像（随缘更新）：

* https://gitee.com/woctordho/Nova

国内的小伙伴也可以用各种GitHub镜像网站来访问这个repo
