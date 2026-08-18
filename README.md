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

## 快速开始（怎么用）

### 方式一：直接下载 Release（推荐，免克隆）

在 [Releases](https://github.com/Sezario1231/copilot-deepseek/releases) 下载 `DeepSeek-Copilot-win-x64.zip`，解压后**双击 exe 即可使用**——已内置 .NET 运行时，无需安装任何东西。

- **Chat 模式开箱即用**：不用填任何 Key，登录 DeepSeek 网页账号即可。
- **Agent 模式**需要本地 DeepSeek Harness（见方式二），未部署时界面会给出友好提示，不会报错。

### 方式二：从源码构建（完整功能）

### 环境要求

| 依赖 | 说明 |
| --- | --- |
| Windows 10/11 | 需支持 Copilot 键（或键盘映射到该键） |
| Git | 拉取 DeepSeek Harness 源码 |
| Node.js ≥ 20 + pnpm | 运行 Harness（`npm install -g pnpm`） |
| .NET 10 SDK | 仅构建时需要（已装则自动使用系统 dotnet） |
| WebView2 运行时 | Win11 自带；Win10 装 Edge 后通常已具备 |

### 三步跑起来

```cmd
:: 1. 部署本地 Harness（只需一次；会下载依赖，耐心等几分钟）
setup-harness.cmd

:: 2. 填 API Key（Agent 模式需要，Chat 模式不需要）
::    用记事本打开 home\.env，在 DEEPSEEK_API_KEY= 后面填你的 key
notepad home\.env

:: 3. 构建并运行（会自动启动 Harness）
build.cmd
run.cmd
```

> 没有 API Key 也能用：顶栏切到 **Chat** 模式，登录 DeepSeek 网页账号即可。

### 日常使用

- 按 **Copilot 键**：显示 / 隐藏侧边栏；**长按**触发屏幕截图并自动粘贴进对话。
- 顶栏 **Chat | Agent** 开关切换网页版 / 本地 Harness。
- 右上角齿轮进入设置：宽度、透明度、动画、开机自启等。
- Harness 独立部署在项目内 `harness\`（数据在 `home\`），端口 3081，与机器上其他 DSH 环境完全隔离。

### 常用脚本

| 脚本 | 作用 |
| --- | --- |
| `setup-harness.cmd` | 一键克隆并安装本地 Harness，生成 `home\.env` 模板 |
| `start-web.cmd` / `stop-web.cmd` | 手动启停 Harness（3081） |
| `build.cmd` | 构建应用（优先系统 dotnet） |
| `run.cmd` | 构建（如需要）并启动应用，自动拉起 Harness |
| `package-release.cmd` | 发布自包含单文件 exe 并打包 zip（用于上传 Release） |

## 技术栈

WPF + WinForms（托盘）、WebView2、KBEHtool 键盘钩子、Svg.Skia 图标渲染、DanKeJson 配置。

## 常见问题

- **Agent 模式对话不可用**：`home\.env` 里的 `DEEPSEEK_API_KEY` 为空或无效，填好后重启 Harness。
- **端口 3081 被占用**：先 `stop-web.cmd`，再重新 `start-web.cmd`。
- **没有 Copilot 键**：在系统键盘设置里把其他键（如右 Ctrl）映射为 Copilot 键，或在设置中开启"映射右 Ctrl"。