# UVC Camera Control - Windows WPF 应用

这是一个使用WPF创建的UVC相机控制应用程序，提供类似Linux v4l2-ctl的功能。当前版本为可行性验证阶段的基础框架。

## 项目概述

此项目演示了如何在Windows WPF应用中控制UVC相机，使用现代的MVVM架构和Windows.Media.Capture API（未来集成）。

## 技术栈

- **框架**: WPF (.NET 8.0)
- **架构模式**: MVVM
- **相机控制**: 为Windows.Media.Capture预留接口，当前为模拟实现
- **UI库**: Microsoft.Toolkit.Mvvm

## 项目结构

```
UVCCameraControl/
├── Models/                 # 数据模型
│   └── CameraModels.cs    # 相机设备和设置模型
├── Services/              # 业务逻辑服务
│   ├── ICameraService.cs  # 相机服务接口
│   └── CameraService.cs   # 相机服务实现（当前为模拟）
├── ViewModels/           # MVVM视图模型
│   └── MainViewModel.cs  # 主窗口视图模型
├── Converters/           # 数据转换器
│   └── BooleanToVisibilityConverter.cs
├── MainWindow.xaml      # 主界面设计
├── MainWindow.xaml.cs   # 主界面代码
└── App.xaml             # 应用程序入口
```

## 功能特性（当前版本）

### 已实现
- ✅ WPF项目基础架构
- ✅ MVVM模式实现
- ✅ 相机设备枚举界面（模拟数据）
- ✅ 参数控制界面（滑块和数值显示）
- ✅ 基础的启动/停止预览功能
- ✅ 异步命令处理
- ✅ 状态消息显示

### 参数控制界面
- 亮度 (Brightness)
- 对比度 (Contrast)
- 饱和度 (Saturation)
- 色调 (Hue)
- 伽马 (Gamma)
- 白平衡 (White Balance) - 支持自动模式
- 曝光 (Exposure) - 支持自动模式
- 对焦 (Focus) - 支持自动模式
- 变焦 (Zoom)

## 运行应用

```bash
cd demoUVC
dotnet build
dotnet run
```

## 下一步开发计划

### 阶段1: 真实相机集成
1. **集成Windows.Media.Capture API**
   - 替换模拟的CameraService实现
   - 实现真实的设备枚举
   - 添加错误处理和权限请求

2. **实现预览功能**
   - 集成真实的相机预览显示
   - 可能需要使用Win32互操作或第三方控件
   - 处理不同分辨率和帧率

### 阶段2: UVC参数控制
1. **完整的UVC参数支持**
   - 通过VideoDeviceController访问所有UVC参数
   - 实现参数范围检测和验证
   - 添加参数重置功能

2. **高级功能**
   - 分辨率和帧率设置
   - 图像格式选择
   - 参数配置保存/加载

### 阶段3: 用户体验优化
1. **界面改进**
   - 更好的错误提示和状态显示
   - 实时参数值更新
   - 参数预设管理

2. **性能优化**
   - 异步操作优化
   - 内存管理
   - 预览性能优化

## 技术难点和解决方案

### 1. Windows Runtime API集成
**问题**: 在WPF中使用UWP的Windows.Media.Capture API
**解决方案**: 使用Microsoft.Windows.CsWinRT包，或考虑DirectShow替代方案

### 2. 预览显示
**问题**: WPF中显示相机预览流
**解决方案**:
- 使用Win32互操作
- 第三方控件如AForge.NET
- 自定义渲染控件

### 3. UVC参数控制
**问题**: 访问底层UVC控制
**解决方案**:
- Windows.Media.Devices.VideoDeviceController
- 直接USB通信（高级选项）

## 注意事项

1. **权限**: 应用需要相机访问权限
2. **兼容性**: 目前针对Windows 10/11设计
3. **硬件支持**: 需要UVC兼容的相机设备
4. **性能**: 相机操作可能影响系统性能

## 替代技术方案

如果Windows.Media.Capture遇到问题，可考虑：

1. **DirectShow + DirectShow.NET**
   - 功能最全面
   - 稳定性好
   - 学习成本较高

2. **AForge.NET**
   - 简单易用
   - 功能相对有限
   - 社区支持好

3. **OpenCV + Emgu CV**
   - 跨平台
   - 主要用于图像处理
   - UVC控制功能有限

## 构建说明

确保安装了以下组件：
- .NET 8.0 SDK
- Visual Studio 2022 或 Visual Studio Code
- Windows 10 SDK (如使用Windows Runtime API)

当前版本已经可以成功构建并运行，提供了完整的UI框架用于后续的真实功能集成。