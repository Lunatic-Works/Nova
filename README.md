# Nova（能叶）

![Nova banner](https://github.com/Lunatic-Works/Nova/wiki/img/nova_banner.png)

基于Unity，对程序员友好的视觉小说（VN）/文字冒险游戏（AVG）框架

## 使用说明

1. `git clone`，为了节约时间可以加上`--depth 1`
2. 将`ProjectSettings/ProjectVersion.txt`中的Unity版本号改为你的版本号
3. 在Unity Editor中打开`Assets/Scenes/Main.unity`，运行游戏，把示例作品《Colorless》看一遍
4. 把游戏中的教程看一遍，同时可以试着改一改脚本，脚本是`Assets/Resources/Scenarios/`文件夹下的`tut01.txt`等文件
5. 如果你想修改Colorless的脚本，需要先删除英文版的脚本，否则中文与英文的脚本会对不上
    * `Assets/Resources/LocalizedResources/English/Scenarios/`文件夹下是英文版的脚本，可以全部删除
    * `Assets/Resources/LocalizedResourcePaths.txt`用来记录英文版用到的文件，可以删除
6. 其他资料可以参考[GitHub Wiki](https://github.com/Lunatic-Works/Nova/wiki/)

## 常见问题

* **网上已经有很多视觉小说引擎/框架了，Nova与它们的差异在哪里？**

    这篇文章介绍了设计思路：https://zhuanlan.zhihu.com/p/272466277

* **支持什么版本的Unity、什么操作系统/平台？**

    支持Unity 2019及更高版本，Windows/Linux/macOS/Android/iOS平台。WebGL目前不支持，不过似乎已经有人成功让我们的依赖（tolua#和Json.NET）支持WebGL了，我们真要做的话应该也可以。

* **我可以把解谜/战棋等游戏加到Nova里吗？我可以把Nova作为对话系统加到解谜/战棋等游戏里吗？**

    绝大多数Unity能做的gameplay都可以加到Nova里，但是把Nova加到其他游戏里会比较困难。目前Nova的定位是“框架/模板”，而不是“插件/扩展包”。

* **Unity已死，Godot万岁！**

    等Godot 4出来了再说。。

## 版本说明

* v0.1：与《青箱》完全兼容的版本
* v0.2：`master` branch上滚动更新的版本，重要的新功能包括新的脚本parser和存档系统，这些新功能不会影响游戏制作者的工作流程
* v0.3：正在与我们的新作同步开发的版本（等我们把代码整理好了会放出来一个branch），预计的新功能包括URP和Addressables，这些新功能对游戏制作者的工作流程有一定影响

## 友情链接

* 我们的第一部视觉小说作品《青箱》：[Steam](https://store.steampowered.com/app/1131740) [知乎](https://www.zhihu.com/question/409724349) [Bangumi](https://bgm.tv/subject/311066) [VNDB](https://vndb.org/v26506)
* 我们的微博：[@LunaticWorks](https://weibo.com/LunaticWorks)
* 我们的QQ群：876212259，如果以后讨论程序的人多了可能会再开一个程序群

也可以看一看其他的视觉小说引擎：

* [Ren'Py](https://github.com/renpy/renpy)：如果你没有编程基础但是想学，或者有Python基础，推荐用这个引擎
* [AVG.js](https://github.com/avgjs/avg-core)：如果你信仰web前端，推荐用这个引擎
* [Librian](https://github.com/RimoChan/Librian)：Python后端和web前端混合的引擎，作者是个萝莉控
* [AVGPlus](https://github.com/avg-plus/avg.renderer)：另一个使用web前端的引擎，似乎得到了Xihe Animation的支持
* [Snowing](https://github.com/Strrationalism/Snowing)：用C++写的硬核引擎

以及通用游戏引擎：

* [PainterEngine](https://github.com/matrixcascade/PainterEngine)：由C语言编写的跨平台图形应用框架
* [EtherEngine](https://github.com/EtherProject/EtherEngine)：基于Lua的跨平台游戏接口
* [Luna Engine](https://github.com/JX-Master/Luna-Engine-0.6)：这个引擎不是我们做的，不过看起来很有意思

以及

* [UniGal](https://github.com/Uni-Gal/UniGal-Script)：为了解决各家视觉小说引擎的碎片化问题，而定义的通用脚本格式

本框架的依赖：

* [tolua#](https://github.com/topameng/tolua)
* [Json.NET](https://github.com/JamesNK/Newtonsoft.Json)

国内镜像（随缘更新）：

* https://gitee.com/woctordho/Nova
