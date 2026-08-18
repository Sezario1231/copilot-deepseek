# DeepSeek Copilot

> 本项目基于 [DanKE123abc/deepseek-copilot](https://github.com/DanKE123abc/deepseek-copilot) 二次开发，在其基础上改为调用本地隔离部署的 DeepSeek Harness，并新增 Chat / Agent 双模式切换等功能。

用 DeepSeek 接管键盘上的 Copilot 键：按一下，屏幕右侧滑出 AI 侧边栏，不依赖国内不可用的微软 Copilot 服务。

## 项目介绍

DeepSeek Copilot 是一个基于 .NET 10 + WPF + WebView2 构建的 Windows 桌面工具，通过全局键盘钩子（KBEHtool）监听 Copilot 键，按下即唤出侧边栏。整体占用极小，单实例运行，常驻托盘图标。

顶栏内置双模式切换：

- **Chat**：打开 DeepSeek 官方网页版聊天，无需任何 Token，登录网页账号即可使用。
- **Agent**：打开本地部署的 DeepSeek Harness（DeepSeek 官方开源的插件化 Agent 框架），支持智能体工作流、工具调用与会话管理，数据完全本地隔离。

主要特性：

- 短按 Copilot 键开关侧边栏，长按触发屏幕截图并自动粘贴进对话。
- 隐藏侧边栏后按设定延迟销毁 WebView，自动释放内存。
- 侧边栏失焦自动滑出，配合鼠标悬停检测，交互顺手。
- 深浅主题跟随系统，宽度、透明度、动画速度均可自定义，支持开机自启。

配套的隔离环境：项目默认指向本地独立部署的 DeepSeek Harness（DSH_HOME 独立、端口 3081、自带便携 Node 22 与 .NET 10 SDK），与机器上已有的 DSH Desktop 完全互不干扰。

技术栈：WPF + WinForms（托盘）、WebView2、KBEHtool 键盘钩子、Svg.Skia 图标渲染、DanKeJson 配置。