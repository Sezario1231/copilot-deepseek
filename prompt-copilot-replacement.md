# 项目：替代 Copilot 按键的侧边栏 AI 聊天应用

## 目标

替换 Windows 键盘上 Copilot 键的默认行为，按下后从屏幕右侧滑出一个侧边栏窗口，内嵌 WebView 加载 https://chat.deepseek.com。

尽可能减小占用，仅在呼出窗口时占用电脑性能。

键盘事件捕获库使用 C:\Users\DanKe\Documents\GitHub\KBEHtool ，可添加nuget依赖 dotnet add package KBEHtool --version 0.3.0

## 技术栈

- **语言/框架**: C# WPF (.NET 6/8)
- **WebView**: WebView2 (Microsoft Edge WebView2 Runtime，Win11 自带)
- **热键拦截**: 使用C:\Users\DanKe\Documents\GitHub\KBEHtool（nuget依赖）
- **打包**: 单 exe，依赖仅 WebView2 Runtime

## 核心架构

### 1. 键盘钩子 (Keyboard Hook)

- 注册全局低级键盘钩子监听 `WM_KEYDOWN`
- Copilot 键映射: 现代键盘该键 scancode = `0xBE`，extended flag = `true`
- 键按下时 `WM_SYSKEYDOWN` 或 extended key 判定
- 收到 Copilot 按键事件 → 触发窗口管理
- 钩子 dll 需嵌入 exe 或以 native 方式调用

### 2. 窗口管理 (Window Manager)

- 单例窗口，避免重复创建
- **窗口已关闭** → 创建并滑入
- **窗口已打开且可见** → 滑出关闭
- **窗口已打开且隐藏**（失焦自动关闭后）→ 重新滑入
- 监听窗口失焦事件 → 自动滑出关闭

### 3. 侧边栏窗口 (Sidebar Window)

- 无边框窗口，置顶显示
- 位置: 屏幕右侧边缘
- 初始状态: `Left = 屏幕宽度`, `Top = 0`
- 宽度: 420-480px, 高度: 全屏
- 滑入动画: `DoubleAnimation` 驱动 `Left` 从 `屏幕宽度` → `屏幕宽度 - 窗口宽度`
- 滑出动画: `DoubleAnimation` 驱动 `Left` 从 `屏幕宽度 - 窗口宽度` → `屏幕宽度`
- 动画持续时间: 200-300ms，缓动函数 `QuadraticEase` 或 `SineEase`
- 窗口样式: `WindowStyle=None`, `AllowsTransparency=True`, `Topmost=True`, `ShowInTaskbar=False`
- 多显示器支持: 取鼠标所在屏幕或主屏幕

### 4. WebView2 控件

- 嵌入 WPF `WebView2` 控件
- 导航至 `https://chat.deepseek.com`
- 可选: 注入自定义 JS 隐藏页面元素（如页头/页脚）以获得更沉浸的体验
- 可选: 设置 `WebView2.CoreWebView2.Settings`:
  - `IsScriptEnabled = true`
  - `AreDefaultScriptDialogsEnabled = false`
  - `IsWebMessageEnabled = false` 等

### 5. 生命周期

- 应用启动 → 注册钩子 → 系统托盘图标（可选）→ 常驻后台
- 按下 Copilot 键 → 触发展开/收起
- 失焦 → 自动收起
- 右键托盘图标 → 退出应用（释放钩子）

## 关键实现细节

### Copilot Key 检测

使用KBEHtool编写一个检测程序，要求用户协助按下copilot键观察输出

### 滑入动画

```csharp
private void SlideIn()
{
    var screenWidth = SystemParameters.PrimaryScreenWidth;
    var animation = new DoubleAnimation
    {
        From = screenWidth,
        To = screenWidth - SidebarWidth,
        Duration = TimeSpan.FromMilliseconds(250),
        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
    };
    BeginAnimation(LeftProperty, animation);
}
```

### 滑出动画 + 关闭

```csharp
private void SlideOutAndClose()
{
    var screenWidth = SystemParameters.PrimaryScreenWidth;
    var animation = new DoubleAnimation
    {
        From = Left,
        To = screenWidth,
        Duration = TimeSpan.FromMilliseconds(200),
        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
    };
    animation.Completed += (s, e) => Hide(); // 或 Close()
    BeginAnimation(LeftProperty, animation);
}
```

## 可选的增强功能

- 托盘图标：显示/隐藏窗口、退出
- 记住窗口位置/大小
- 开机自启注册表项
- 代理设置
- Cookie 持久化（WebView2 默认支持）
- 快捷键：Esc 关闭

## 交付物

- 完整的 Visual Studio 解决方案（.sln + .csproj）
- 单 exe 发布配置（<PublishSingleFile>true</PublishSingleFile>）
- 代码注释清晰，必要的异常处理
- 不要用任何第三方 NuGet 包（WebView2 SDK 除外）
