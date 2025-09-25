# 相机控制架构复用指南

## 📦 需要复制的核心文件

### 必需文件（最小复用）
```
Interfaces/
├── ICameraParameterController.cs     # 参数控制接口
├── ICameraCaptureEngine.cs          # 图像采集接口
└── IUnifiedCameraController.cs      # 统一控制接口

UvcCameraController.cs               # DirectShow参数控制实现
CameraProperty枚举定义（在UvcCameraController.cs中）
```

### 完整文件（推荐复用）
```
Interfaces/                          # 所有接口文件
Engines/
└── OpenCVCaptureEngine.cs          # OpenCV图像采集引擎
Controllers/
└── UnifiedUVCCameraController.cs   # 统一控制器
Improved/
└── ImprovedCameraManager.cs        # 简化接口
```

### 依赖包（NuGet）
```xml
<PackageReference Include="DirectShowLib" Version="1.0.0" />
<PackageReference Include="OpenCvSharp4" Version="4.9.0.20240103" />
<PackageReference Include="OpenCvSharp4.Extensions" Version="4.9.0.20240103" />
<PackageReference Include="OpenCvSharp4.runtime.win" Version="4.9.0.20240103" />
```

## 🚀 使用场景

### 场景1：新项目集成
```csharp
// 1. 复制文件到新项目
// 2. 安装NuGet包
// 3. 简单使用
using UVCCameraControl.Improved;

var cameraManager = new ImprovedCameraManager();
if (cameraManager.Initialize(0))
{
    // 开始使用
    var frame = cameraManager.CaptureFrame();
    cameraManager.SetCameraProperty(CameraProperty.Brightness, 75);

    // 获取参数范围用于UI绑定
    var (min, max, step, defaultValue, success) = cameraManager.GetCameraPropertyRange(CameraProperty.Brightness);
    if (success)
    {
        Console.WriteLine($"亮度范围: {min} ~ {max}, 步长: {step}, 默认: {defaultValue}");
    }
}
```

### 场景2：WPF/UI项目集成（包含参数范围绑定）
```csharp
// ViewModel中获取并绑定参数范围
public class CameraViewModel : ObservableObject
{
    private ICameraService _cameraService;

    // 参数范围属性（绑定到UI控件）
    [ObservableProperty]
    private int brightnessMin, brightnessMax, brightnessStep, brightnessDefault;

    private async Task LoadCameraRanges()
    {
        // 获取真实硬件支持的范围
        var range = await _cameraService.GetCameraPropertyRangeAsync(CameraProperty.Brightness);
        if (range.success)
        {
            BrightnessMin = range.min;      // 绑定到 Slider.Minimum
            BrightnessMax = range.max;      // 绑定到 Slider.Maximum
            BrightnessStep = range.step;    // 绑定到 Slider.TickFrequency
            BrightnessDefault = range.defaultValue; // 默认值
        }
    }
}
```

```xml
<!-- XAML中的UI绑定 -->
<Slider Minimum="{Binding BrightnessMin}"
        Maximum="{Binding BrightnessMax}"
        TickFrequency="{Binding BrightnessStep}"
        IsSnapToTickEnabled="True"
        Value="{Binding CameraSettings.Brightness}"/>
```

### 场景3：现有项目升级
```csharp
// 替换现有的相机代码
// 原来的代码：
// var oldCamera = new OldCameraClass();

// 新的代码：
using UVCCameraControl.Controllers;
var newCamera = new UnifiedUVCCameraController();
newCamera.Initialize(0);

// 保持相同的API但获得更好的功能
```

### 场景3：库/SDK开发
```csharp
// 创建你自己的相机库
public class MyCustomCameraSDK
{
    private IUnifiedCameraController _camera;

    public MyCustomCameraSDK()
    {
        // 使用我们的统一架构作为基础
        _camera = new UnifiedUVCCameraController();
    }

    // 添加你自己的高级功能
    public bool StartRecording(string filename)
    {
        // 使用_camera.CaptureFrame()实现录制
        return true;
    }
}
```

## 🔧 扩展步骤

### 添加新的参数控制器
1. 实现`ICameraParameterController`接口
2. 添加平台特定的API调用
3. 在统一控制器中注册新的控制器

### 添加新的图像采集引擎
1. 实现`ICameraCaptureEngine`接口
2. 添加新的采集方法（比如DirectShow图像采集）
3. 在统一控制器中使用新的引擎

### 跨平台支持
1. 根据`RuntimeInformation.IsOSPlatform()`检测平台
2. 为每个平台创建适当的控制器组合
3. 使用工厂模式自动选择合适的实现

## 📋 最佳实践

### 1. 接口优先
```csharp
// 好的做法 - 使用接口
ICameraParameterController paramController = GetPlatformSpecificController();

// 避免 - 直接使用具体类
UvcCameraController controller = new UvcCameraController();
```

### 2. 参数范围处理
```csharp
// 获取所有相机参数的范围并绑定到UI
var properties = new[] {
    CameraProperty.Brightness,
    CameraProperty.Contrast,
    CameraProperty.Exposure
};

foreach (var prop in properties)
{
    var range = cameraManager.GetCameraPropertyRange(prop);
    if (range.success)
    {
        Console.WriteLine($"{prop}: {range.min}~{range.max} (step: {range.step})");

        // 特别注意：曝光参数使用对数刻度
        if (prop == CameraProperty.Exposure)
        {
            Console.WriteLine($"曝光使用DirectShow对数刻度，负值表示较短曝光时间");
        }
    }
}
```

### 3. 曝光参数特别说明
```csharp
// DirectShow曝光值说明
var exposureRange = cameraManager.GetCameraPropertyRange(CameraProperty.Exposure);
if (exposureRange.success)
{
    // DirectShow: -13 ~ -1 (对数刻度)
    // v4l2: 1 ~ 5000 (绝对时间微秒)
    // 这两种表示方式都是正确的！

    Console.WriteLine($"DirectShow曝光范围: {exposureRange.min} ~ {exposureRange.max}");
    Console.WriteLine("说明: 负值越大 = 曝光时间越短 = 适合更亮环境");
}
```

### 4. 错误处理
```csharp
try
{
    if (cameraManager.Initialize(cameraIndex))
    {
        // 使用相机
    }
    else
    {
        // 处理初始化失败
        Console.WriteLine("相机初始化失败");
    }
}
catch (Exception ex)
{
    // 处理异常
    Console.WriteLine($"相机错误: {ex.Message}");
}
```

### 3. 资源管理
```csharp
// 总是使用using语句
using var cameraManager = new ImprovedCameraManager();

// 或者明确调用Dispose
try
{
    var cameraManager = new ImprovedCameraManager();
    // 使用相机...
}
finally
{
    cameraManager?.Dispose();
}
```

### 4. 调试信息
```csharp
// 获取详细的架构信息用于调试
var info = cameraManager.GetArchitectureInfo();
Console.WriteLine($"当前架构: {info}");
// 输出: Camera: USB Camera | Parameter Control: DirectShow UVC Parameter Controller | Image Capture: OpenCV Video Capture Engine
```

## 💡 复用优势

1. **分离关注点**: DirectShow处理参数，OpenCV处理图像
2. **自动回退**: OpenCV失败时自动使用DirectShow
3. **动态参数范围**: 自动获取硬件支持的真实参数范围
4. **UI友好绑定**: 参数范围直接绑定到WPF/UI控件
5. **防止无效设置**: 滑块限制在硬件支持范围内，避免用户"乱设置"
6. **曝光参数兼容**: 正确处理DirectShow对数刻度曝光值
7. **平台扩展**: 易于添加Linux V4L2、macOS AVFoundation支持
8. **接口统一**: 所有平台使用相同的API
9. **调试友好**: 详细的日志和状态信息
10. **内存安全**: 正确的资源管理和释放

## 🎯 总结

这个架构让你能够：
- **快速集成**: 复制文件即可使用
- **UI无忧**: 自动获取参数范围，防止无效设置
- **曝光兼容**: 正确处理DirectShow对数刻度(-13~-1)与v4l2绝对时间(1-5000)的差异
- **灵活扩展**: 基于接口的设计支持新平台
- **稳定可靠**: DirectShow + OpenCV的组合提供最佳兼容性
- **易于维护**: 清晰的分层结构便于调试和修改

选择最适合你项目需求的复用方式，从简单的ImprovedCameraManager到完全自定义的跨平台实现。

## 🔗 相关文档

- `EXPOSURE_MAPPING.md` - 详细说明DirectShow vs v4l2曝光参数差异
- `REUSE_GUIDE.md` - 完整的架构复用指南
- `UPGRADE_SUMMARY.md` - 架构升级过程总结