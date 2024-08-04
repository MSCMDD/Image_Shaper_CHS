# ImageShaper_CHS

## 介绍

中文化的 ImageShaper ，分支自 [LinKueiOminae/Image_Shaper](https://github.com/LinKueiOminae/Image_Shaper/)

这个程序允许将图像合并，并将图像序列转换为SHP文件。它基本上是 [Image Combine](https://ppmforums.com/viewtopic.php?t=42256) 功能更强大的继任者。

这个工具是为了克服SHP Builder的缺点而创建的，这些缺点包括：

1. 导入非常慢（导入时UI冻结）
2. 在导入期间无法合并不同的渲染通道
3. 不方便的调色板管理，需要为每个想要忽略某些颜色（例如，发光颜色、非重映射红色等）的导入自定义调色板
4. 无法导入已经调色过的图像。SHP Builder仍然应用它自己的颜色转换，这会弄乱具有相同RGB值的不同的调色板颜色（例如，颜色#240的发光像素被改变为非发光颜色#15，非重映射红色被改变）

使用这个工具，您可以在一个操作中将多达3个不同的渲染通道转换为单个SHP，同时只使用一个调色板文件。这个调色板会分配给每个图像，并可能具有不同的设置，例如忽略发光颜色。

对于喜欢对比数字的人来说： 导入576个200x200像素大小的文件

| 软件 | 时间 |
| --- | --- |
| SHP Builder | 超过3分钟 |
| Image Shaper | 25秒 |

如何使用： 将文件拖放到数据网格上，或使用其右键单击上下文菜单来加载图像文件。

## 下载 ImageShaper_CHS

GitHub Release：https://github.com/MSCMDD/Image_Shaper_CHS/releases

Github Action：https://github.com/MSCMDD/Image_Shaper_CHS/actions

## 使用教程：

https://ppmforums.com/topic-44476/image-shaper/
