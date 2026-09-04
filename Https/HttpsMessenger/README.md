# HttpsMessenger

基于 **C# Windows Forms + .NET 8** 的 **HTTPS 双向消息通讯** 桌面软件。本机可同时作为 HTTPS 服务端与客户端，展示 TLS/HTTP 全过程，支持可配置的请求→响应字段规则（SQLite）、通讯历史回放、运行日志与多语言界面。

> 程序内按 **F1** 或菜单「帮助 → 打开帮助文档」可打开 HTML 版详细帮助。  
> 另见：[Help/帮助文档.html](Help/帮助文档.html) · [Help/运行环境要求清单.html](Help/运行环境要求清单.html)

---

## 目录

- [功能概览](#功能概览)
- [运行环境](#运行环境)
- [快速开始](#快速开始)
- [详细使用方法](#详细使用方法)
- [技术架构](#技术架构)
- [API 接口说明](#api-接口说明)
- [数据存储路径](#数据存储路径)
- [完整开发步骤与代码](#完整开发步骤与代码)
- [项目结构](#项目结构)
- [常见问题 FAQ](#常见问题-faq)

---

## 功能概览

| 模块 | 功能 | 说明 |
|------|------|------|
| **HTTPS 通讯** | 本机 HTTPS 服务 | `TcpListener` + `SslStream`，自签名证书，TLS 1.2/1.3 |
| | 对端消息发送 | `HttpClient` POST `/api/message`，可附加自定义请求字段 |
| | 发送/响应分窗 | 发送窗仅显示「回复摘要」；全部响应窗显示原始 JSON 等 |
| | 全过程追踪 | TCP → TLS → HTTP → 规则匹配 → 响应，可回看历史 |
| | 字段规则 CRUD | SQLite 存储请求字段→响应字段规则，增删改查 |
| | 运行日志 | INFO/WARN/ERROR/DEBUG，内存显示 + 按日文件 |
| **通用** | 窗口可缩放 | 分隔条拖动；顶部服务/对端配置区固定尺寸 |
| | 多语言 | 中文 / English / 中英对照 |
| | 帮助文档 | HTML 帮助 + 运行环境清单 |

> **说明**：项目中仍保留 MES 生产接口后端代码（`Services/Mes/`）及本地 MES 模拟路由，当前版本 UI 已移除 MES 页签，可通过本机 HTTPS 服务模拟 `/mes/*` 接口供联调。

---

## 运行环境

| 项目 | 要求 |
|------|------|
| 操作系统 | Windows 10 / 11（64 位） |
| 运行时 | [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| 开发工具 | Visual Studio 2022 17.8+ 或 VS Code + C# Dev Kit |
| SDK | .NET 8 SDK（编译时需要） |
| 磁盘 | 约 50 MB（含 SQLite 数据库与日志） |
| 网络 | 本机联调需开放监听端口（默认 8443） |

---

## 快速开始

### 1. 编译

```bash
cd HttpsMessenger
dotnet restore
dotnet build -c Release
```

### 2. 运行

```bash
dotnet run -c Release
```

或直接运行编译产物：

```text
bin\Release\net8.0-windows\HttpsMessenger.exe
```

### 3. 单机自测（3 步）

1. 点击 **「启动服务」**（默认端口 `8443`）
2. 对端地址保持 `https://127.0.0.1:8443`，勾选 **「忽略证书错误」**
3. 输入消息（如 `你好`）点击 **「发送」**，观察发送窗、响应窗与全过程面板

---

## 详细使用方法

### 一、HTTPS 通讯页签

#### 1. 本机 HTTPS 服务（左侧固定区域）

| 控件 | 说明 |
|------|------|
| 本机名称 | 显示在服务端响应中，默认取计算机名 |
| 端口 | 默认 `8443`，范围 1~65535 |
| 启动/停止服务 | 启动后监听 `https://0.0.0.0:端口/` |
| 重建证书 | 删除旧 PFX 并生成新的自签名证书（需先停止服务） |
| 证书信息 | 显示主题、指纹、有效期 |

首次运行会自动在 `%LocalAppData%\HttpsMessenger\` 生成 `server.pfx` 自签名证书。

#### 2. 对端连接（右侧固定区域）

| 控件 | 说明 |
|------|------|
| 对端 HTTPS 地址 | 如 `https://192.168.1.100:8443` |
| 测试连通 | GET `/api/ping`，验证 TCP + TLS + HTTP |
| 忽略证书错误 | 开发/联调时勾选，接受自签名证书 |
| 自动回复 | 收到对端消息后自动回发确认 |
| 自定义请求字段 | 附加 `requestField` / `requestValue` 到 JSON，供规则匹配 |

#### 3. 消息区域

| 窗口 | 显示内容 |
|------|----------|
| **发送信息窗口（仅回复）** | 简洁摘要，格式：`[时间] 响应摘要 HTTP 200 成功` + `回复信息:xxx` |
| **全部响应信息** | 发送/接收记录、匹配规则、customFields、原始响应正文 |
| **HTTPS 通讯全过程** | 分步追踪：TCP 连接 → TLS 握手 → HTTP 请求/响应 |
| **本次响应 JSON** | 结构化 `ApiResponse` 对象 |

#### 4. 快捷操作

- **Enter** 发送消息（Shift+Enter 换行需多行输入框时自行扩展）
- **F1** 打开帮助文档
- **导出消息** 将两个窗口内容导出为 `.txt`
- 菜单 **语言** 切换：中文 / English / 中英对照

### 二、请求/响应字段规则页签

服务端收到 `POST /api/message` 后，根据 SQLite 中的规则匹配请求 JSON 字段，将匹配结果写入响应的 `customFields`。

| 列 | 含义 |
|----|------|
| 规则名称 | 便于识别的名称 |
| 请求字段 | 如 `content`、`type`、`requestField` |
| 匹配方式 | `Equals` / `Contains` / `Any` |
| 请求值 | 期望匹配的值（`Any` 模式可为空） |
| 响应字段 | 写入 `customFields` 的键名 |
| 响应值 | 写入的值，支持占位符 `{now}` `{utc}` `{machine}` |
| HTTP 状态 | 匹配后返回的状态码 |
| 响应消息 | 覆盖 `ApiResponse.message` |
| 启用 | 是否参与匹配 |

**预置 6 条示例规则**（首次启动自动写入）：

| 规则 | 触发条件 | 响应字段 |
|------|----------|----------|
| 问候语规则 | content = 你好 | reply |
| 帮助规则 | content 包含 帮助 | help |
| 时间查询 | content = 时间 | currentTime = {now} |
| 类型 ping | type = ping | status = alive |
| 自定义字段 echo | requestField = demoKey | demoKey |
| 任意发送者欢迎 | sender 任意非空 | welcome |

操作：**新增 / 修改 / 删除 / 刷新**，双击行可编辑。

### 三、通讯过程历史页签

每次客户端发送或服务端接收请求后，完整 `CommunicationTrace` 写入 SQLite。左侧列表显示最近 80 条，选中后在右侧查看详细过程文本。

### 四、运行日志页签

- 级别筛选：全部 / INFO / WARN / ERROR / DEBUG
- 内存缓冲最多 2000 条，同时按日写入 `%LocalAppData%\HttpsMessenger\logs\HttpsMessenger_yyyyMMdd.log`
- 支持清空内存日志、导出、打开日志文件夹

---

## 技术架构

### 整体架构

```text
┌─────────────────────────────────────────────────────────────┐
│                      MainForm (WinForms UI)                  │
│  ┌─────────────┐ ┌──────────────┐ ┌──────────┐ ┌───────────┐ │
│  │ HTTPS 通讯  │ │ 字段规则 CRUD │ │ 通讯历史 │ │ 运行日志  │ │
│  └──────┬──────┘ └──────┬───────┘ └────┬─────┘ └─────┬─────┘ │
└─────────┼───────────────┼────────────────┼─────────────┼───────┘
          │               │                │             │
          ▼               ▼                ▼             ▼
   HttpsChatServer   SqliteStore      SqliteStore    AppLogger
   HttpsChatClient   FieldRuleMatcher
   CertificateHelper
          │
          ▼
   TcpListener → SslStream → HTTP 解析 → 路由分发
                                          ├─ GET  /api/ping
                                          ├─ POST /api/message → FieldRuleMatcher
                                          └─ POST /mes/*       → MesMockHandler（可选）
```

### 核心技术栈

| 层次 | 技术 | 用途 |
|------|------|------|
| UI | Windows Forms (.NET 8) | 桌面界面、TabControl、SplitContainer |
| 服务端 HTTPS | `TcpListener` + `SslStream` | 手动实现 HTTP/1.1 over TLS |
| 客户端 HTTPS | `HttpClient` + `HttpClientHandler` | 发送请求、证书校验回调 |
| 证书 | `System.Security.Cryptography.X509Certificates` | RSA 2048 自签名证书 |
| 序列化 | `System.Text.Json` | ChatMessage / ApiResponse JSON |
| 数据库 | Microsoft.Data.Sqlite | 字段规则、通讯历史 |
| 日志 | 自研 AppLogger | 内存 + 文件双写 |
| 多语言 | 自研 Lang.cs | 中英双语字典 + 事件通知 |
| Excel（MES 后端） | ClosedXML | 生产日志（后端保留） |

### HTTPS 通讯全流程

**客户端发送消息：**

```text
1. 构造 ChatMessage JSON
2. HttpClient 解析 URL → 建立 TCP 连接
3. TLS 客户端握手（可选忽略证书错误）
4. POST /api/message + Content-Type: application/json
5. 读取 HTTP 响应状态码与 JSON 正文
6. 反序列化为 ApiResponse
7. 记录 CommunicationTrace → SQLite
8. UI：发送窗显示摘要，响应窗显示完整信息
```

**服务端处理请求：**

```text
1. TcpListener.AcceptTcpClientAsync
2. SslStream.AuthenticateAsServerAsync（TLS 1.2/1.3）
3. 读取 HTTP 请求行、头、Body
4. 路由分发：
   - GET  /api/ping     → 返回 pong JSON
   - POST /api/message  → 解析 ChatMessage → 规则匹配 → ApiResponse
   - POST /mes/*        → MesMockHandler（本地模拟）
5. 写入 HTTP/1.1 响应（Connection: close）
6. 触发 TraceCompleted 事件 → 保存历史
```

---

## API 接口说明

### GET /api/ping

连通测试，返回：

```json
{
  "success": true,
  "message": "pong from 本机名称",
  "serverTime": "2026-09-03T21:00:00"
}
```

### POST /api/message

**请求体（ChatMessage）：**

```json
{
  "id": "a1b2c3...",
  "sender": "ClientA",
  "content": "你好",
  "timestamp": "2026-09-03T21:00:00",
  "type": "text",
  "requestField": "demoKey",
  "requestValue": "demoValue"
}
```

**响应体（ApiResponse）：**

```json
{
  "success": true,
  "message": "问候已处理",
  "serverTime": "2026-09-03T21:00:00",
  "echo": { "...": "原消息回显" },
  "matchedRules": ["问候语规则"],
  "customFields": {
    "reply": "你好！我是 HttpsMessenger 服务端。"
  }
}
```

### GET /

返回简单 HTML 主页，显示服务运行状态。

---

## 数据存储路径

| 类型 | 路径 |
|------|------|
| SQLite 数据库 | `%LocalAppData%\HttpsMessenger\https_messenger.db` |
| 自签名证书 PFX | `%LocalAppData%\HttpsMessenger\server.pfx` |
| 证书公钥 CER | `%LocalAppData%\HttpsMessenger\server.cer` |
| 运行日志 | `%LocalAppData%\HttpsMessenger\logs\HttpsMessenger_yyyyMMdd.log` |
| MES 配置（后端） | `%LocalAppData%\HttpsMessenger\mes_config.json` |
| MES Excel 日志（后端） | 可配置，默认 `%LocalAppData%\HttpsMessenger\logs\Production_yyyyMMdd.xlsx` |

---

## 完整开发步骤与代码

以下按实际开发顺序，分 **12 个步骤** 说明从零构建本程序的完整过程，每步给出关键代码。

---

### 步骤 1：创建 WinForms 项目

**目标**：建立 .NET 8 Windows Forms 工程，引入 SQLite 依赖。

**操作**：

```bash
dotnet new winforms -n HttpsMessenger -f net8.0-windows
cd HttpsMessenger
dotnet add package Microsoft.Data.Sqlite
dotnet add package ClosedXML
```

**HttpsMessenger.csproj**：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>HttpsMessenger</RootNamespace>
    <AssemblyName>HttpsMessenger</AssemblyName>
    <Version>1.0.0</Version>
    <Description>基于 HTTPS 协议的双向消息通讯软件</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ClosedXML" Version="0.105.1" />
    <PackageReference Include="Microsoft.Data.Sqlite" Version="10.0.11" />
  </ItemGroup>
</Project>
```

**Program.cs** — 程序入口：

```csharp
namespace HttpsMessenger;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
```

---

### 步骤 2：定义数据模型

**目标**：定义消息、响应、规则、通讯追踪等核心数据结构。

**Models/ChatMessage.cs**：

```csharp
using System.Text.Json.Serialization;

namespace HttpsMessenger.Models;

public sealed class ChatMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("sender")]
    public string Sender { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("requestField")]
    public string? RequestField { get; set; }

    [JsonPropertyName("requestValue")]
    public string? RequestValue { get; set; }
}

public sealed class ApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("serverTime")]
    public DateTime ServerTime { get; set; } = DateTime.Now;

    [JsonPropertyName("echo")]
    public ChatMessage? Echo { get; set; }

    [JsonPropertyName("matchedRules")]
    public List<string> MatchedRules { get; set; } = new();

    [JsonPropertyName("customFields")]
    public Dictionary<string, string> CustomFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class FieldRule
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RequestField { get; set; } = "content";
    public string MatchMode { get; set; } = "Equals"; // Equals / Contains / Any
    public string RequestValue { get; set; } = string.Empty;
    public string ResponseField { get; set; } = "reply";
    public string ResponseValue { get; set; } = string.Empty;
    public int HttpStatus { get; set; } = 200;
    public string ResponseMessage { get; set; } = "OK";
    public bool IsEnabled { get; set; } = true;
    public string Remark { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public sealed class CommunicationTrace
{
    public string Direction { get; set; } = "Client";
    public string Title { get; set; } = string.Empty;
    public List<TraceStep> Steps { get; set; } = new();
    public string RequestMethod { get; set; } = string.Empty;
    public string RequestUrl { get; set; } = string.Empty;
    public string RequestBody { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime FinishedAt { get; set; } = DateTime.Now;

    public void AddStep(string phase, string detail)
    {
        Steps.Add(new TraceStep
        {
            Order = Steps.Count + 1,
            Phase = phase,
            Detail = detail,
            Time = DateTime.Now
        });
    }

    public string FormatProcessText()
    {
        var lines = new List<string>
        {
            $"【{Direction}】{Title}",
            $"结果：{(Success ? "成功" : "失败")}",
            "===== HTTPS 通讯过程 ====="
        };
        foreach (var step in Steps)
            lines.Add($"{step.Order}. {step.Phase} — {step.Detail}");
        lines.Add($"请求：{RequestMethod} {RequestUrl}");
        lines.Add(RequestBody);
        lines.Add($"响应：HTTP {ResponseStatusCode}");
        lines.Add(ResponseBody);
        return string.Join(Environment.NewLine, lines);
    }
}

public sealed class TraceStep
{
    public int Order { get; set; }
    public string Phase { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTime Time { get; set; } = DateTime.Now;
}
```

---

### 步骤 3：实现自签名证书管理

**目标**：自动创建/加载 RSA 自签名证书，供 HTTPS 服务端 TLS 握手使用。

**Services/CertificateHelper.cs**：

```csharp
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace HttpsMessenger.Services;

public static class CertificateHelper
{
    public static string DefaultCertPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HttpsMessenger", "server.pfx");

    public const string DefaultPassword = "HttpsMessenger@2026";

    public static X509Certificate2 LoadOrCreate()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DefaultCertPath)!);
        if (File.Exists(DefaultCertPath))
            return new X509Certificate2(DefaultCertPath, DefaultPassword,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

        var cert = CreateSelfSigned("CN=HttpsMessenger-Local");
        File.WriteAllBytes(DefaultCertPath, cert.Export(X509ContentType.Pfx, DefaultPassword));
        return cert;
    }

    public static X509Certificate2 CreateSelfSigned(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));

        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        san.AddIpAddress(System.Net.IPAddress.Loopback);
        request.CertificateExtensions.Add(san.Build());

        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(5));
        var pfx = cert.Export(X509ContentType.Pfx, DefaultPassword);
        return new X509Certificate2(pfx, DefaultPassword, X509KeyStorageFlags.Exportable);
    }
}
```

**要点**：
- 使用 RSA 2048 + SHA256 签名
- SAN 扩展包含 `localhost` 与 `127.0.0.1`，便于本机联调
- 证书持久化到 `%LocalAppData%\HttpsMessenger\server.pfx`

---

### 步骤 4：实现 HTTPS 服务端

**目标**：基于 `TcpListener` + `SslStream` 手动解析 HTTP/1.1，处理 `/api/ping` 与 `/api/message`。

**Services/HttpsChatServer.cs**（核心片段）：

```csharp
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using HttpsMessenger.Models;

namespace HttpsMessenger.Services;

public sealed class HttpsChatServer : IDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private X509Certificate2? _certificate;
    private Func<IReadOnlyList<FieldRule>>? _ruleProvider;

    public bool IsRunning { get; private set; }
    public int Port { get; private set; }
    public string DisplayName { get; set; } = Environment.MachineName;

    public event Action<ChatMessage>? MessageReceived;
    public event Action<CommunicationTrace>? TraceCompleted;

    public void SetRuleProvider(Func<IReadOnlyList<FieldRule>> provider)
        => _ruleProvider = provider;

    public void Start(int port, X509Certificate2 certificate)
    {
        _certificate = certificate;
        Port = port;
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        IsRunning = true;
        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var client = await _listener!.AcceptTcpClientAsync(token);
            _ = Task.Run(() => HandleClientAsync(client, token), token);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        var trace = new CommunicationTrace { Direction = "Server", StartedAt = DateTime.Now };
        using (client)
        {
            await using var network = client.GetStream();
            await using var ssl = new SslStream(network, false);

            // TLS 服务端握手
            await ssl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
            {
                ServerCertificate = _certificate!,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }, token);

            trace.AddStep("TLS 握手完成", $"协议={ssl.SslProtocol}");

            // 读取 HTTP 请求
            var (method, path, headers, body) = await ReadHttpRequestAsync(ssl, token);
            trace.RequestMethod = method ?? "";
            trace.RequestBody = body;

            // 路由分发
            if (method == "GET" && path?.StartsWith("/api/ping") == true)
            {
                var json = JsonSerializer.Serialize(new ApiResponse
                {
                    Success = true,
                    Message = $"pong from {DisplayName}",
                    ServerTime = DateTime.Now
                });
                await WriteHttpAsync(ssl, 200, "OK", json, token);
                trace.ResponseStatusCode = 200;
                trace.ResponseBody = json;
            }
            else if (method == "POST" && path?.StartsWith("/api/message") == true)
            {
                var message = JsonSerializer.Deserialize<ChatMessage>(body);
                MessageReceived?.Invoke(message!);

                var rules = _ruleProvider?.Invoke() ?? Array.Empty<FieldRule>();
                var (apiResponse, status, matched) = FieldRuleMatcher.Match(body, message, rules);

                var json = JsonSerializer.Serialize(apiResponse);
                await WriteHttpAsync(ssl, status, "OK", json, token);
                trace.ResponseStatusCode = status;
                trace.ResponseBody = json;
            }

            trace.Success = true;
            trace.FinishedAt = DateTime.Now;
            TraceCompleted?.Invoke(trace);
        }
    }

    private static async Task WriteHttpAsync(SslStream ssl, int code, string reason, string body, CancellationToken token)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        var header = $"HTTP/1.1 {code} {reason}\r\n" +
                     $"Content-Type: application/json; charset=utf-8\r\n" +
                     $"Content-Length: {bytes.Length}\r\nConnection: close\r\n\r\n";
        await ssl.WriteAsync(Encoding.ASCII.GetBytes(header), token);
        await ssl.WriteAsync(bytes, token);
    }

    public void Stop() { _cts?.Cancel(); _listener?.Stop(); IsRunning = false; }
    public void Dispose() => Stop();
}
```

**要点**：
- 每个连接独立 Task，支持并发
- 手动解析 `\r\n\r\n` 分隔的请求头与 Content-Length Body
- 通过 `SetRuleProvider` 注入 SQLite 规则，解耦存储层

---

### 步骤 5：实现 HTTPS 客户端

**目标**：使用 `HttpClient` 发送消息并记录完整通讯过程。

**Services/HttpsChatClient.cs**（核心片段）：

```csharp
using System.Net.Security;
using System.Text;
using System.Text.Json;
using HttpsMessenger.Models;

namespace HttpsMessenger.Services;

public sealed class HttpsChatClient : IDisposable
{
    private HttpClient? _httpClient;
    public bool IgnoreCertificateErrors { get; set; } = true;

    public HttpsChatClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, errors) =>
                IgnoreCertificateErrors || errors == SslPolicyErrors.None
        };
        _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
    }

    public async Task<(ApiResponse Response, CommunicationTrace Trace)> SendMessageWithTraceAsync(
        string baseUrl, ChatMessage message, CancellationToken token = default)
    {
        var url = BuildUrl(baseUrl, "/api/message");
        var json = JsonSerializer.Serialize(message);
        var trace = new CommunicationTrace
        {
            Direction = "Client",
            Title = $"发送到 {url}",
            RequestMethod = "POST",
            RequestUrl = url,
            RequestBody = json
        };

        trace.AddStep("TLS 握手", IgnoreCertificateErrors ? "忽略自签名证书" : "严格校验");
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient!.PostAsync(url, content, token);

        var text = await response.Content.ReadAsStringAsync(token);
        trace.ResponseStatusCode = (int)response.StatusCode;
        trace.ResponseBody = text;
        trace.Success = response.IsSuccessStatusCode;

        var api = JsonSerializer.Deserialize<ApiResponse>(text)
                  ?? new ApiResponse { Success = false, Message = "空响应" };
        trace.FinishedAt = DateTime.Now;
        return (api, trace);
    }

    public static string BuildUrl(string baseUrl, string path)
    {
        baseUrl = baseUrl.Trim().TrimEnd('/');
        if (!baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            baseUrl = "https://" + baseUrl;
        return baseUrl + path;
    }

    public void Dispose() => _httpClient?.Dispose();
}
```

---

### 步骤 6：实现字段规则匹配引擎

**目标**：将请求 JSON 扁平化，按规则匹配并生成响应自定义字段。

**Services/FieldRuleMatcher.cs**：

```csharp
using System.Text.Json;
using HttpsMessenger.Models;

namespace HttpsMessenger.Services;

public static class FieldRuleMatcher
{
    public static (ApiResponse Response, int StatusCode, List<FieldRule> Matched) Match(
        string requestJson, ChatMessage? message, IReadOnlyList<FieldRule> rules)
    {
        var response = new ApiResponse
        {
            Success = true,
            Message = "消息已接收",
            ServerTime = DateTime.Now,
            Echo = message
        };

        var matched = new List<FieldRule>();
        var status = 200;
        var flat = FlattenJson(requestJson);

        // 附加 UI 传入的自定义字段
        if (message?.RequestField is not null && message.RequestValue is not null)
            flat[message.RequestField] = message.RequestValue;

        foreach (var rule in rules.Where(r => r.IsEnabled))
        {
            if (!flat.TryGetValue(rule.RequestField, out var actual)) continue;
            if (!IsMatch(rule.MatchMode, actual, rule.RequestValue)) continue;

            matched.Add(rule);
            response.CustomFields[rule.ResponseField] = ExpandPlaceholders(rule.ResponseValue);
            response.MatchedRules.Add(rule.Name);
            status = rule.HttpStatus;
            if (!string.IsNullOrWhiteSpace(rule.ResponseMessage))
                response.Message = rule.ResponseMessage;
        }

        return (response, status, matched);
    }

    private static bool IsMatch(string mode, string actual, string expected) => mode switch
    {
        "Any" => !string.IsNullOrWhiteSpace(actual),
        "Contains" => actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
        _ => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
    };

    private static string ExpandPlaceholders(string value) =>
        value.Replace("{now}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
             .Replace("{utc}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
             .Replace("{machine}", Environment.MachineName);

    private static Dictionary<string, string> FlattenJson(string json)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
            result[prop.Name] = prop.Value.ToString();
        return result;
    }
}
```

---

### 步骤 7：实现 SQLite 数据存储

**目标**：持久化字段规则与通讯历史，首次启动写入 6 条种子规则。

**Services/SqliteStore.cs**（核心片段）：

```csharp
using Microsoft.Data.Sqlite;
using HttpsMessenger.Models;

namespace HttpsMessenger.Services;

public sealed class SqliteStore
{
    private readonly string _connectionString;
    public string DatabasePath { get; }

    public SqliteStore()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HttpsMessenger");
        Directory.CreateDirectory(dir);
        DatabasePath = Path.Combine(dir, "https_messenger.db");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = DatabasePath }.ToString();
        Initialize();
    }

    private void Initialize()
    {
        using var conn = Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS FieldRules (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL, RequestField TEXT NOT NULL,
                    MatchMode TEXT DEFAULT 'Equals', RequestValue TEXT DEFAULT '',
                    ResponseField TEXT NOT NULL, ResponseValue TEXT NOT NULL,
                    HttpStatus INTEGER DEFAULT 200, ResponseMessage TEXT DEFAULT 'OK',
                    IsEnabled INTEGER DEFAULT 1, Remark TEXT DEFAULT '',
                    CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS CommunicationLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Direction TEXT, Title TEXT, ProcessText TEXT,
                    RequestBody TEXT, ResponseBody TEXT,
                    Success INTEGER, CreatedAt TEXT
                );
                """;
            cmd.ExecuteNonQuery();
        }

        using (var countCmd = conn.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(1) FROM FieldRules;";
            if (Convert.ToInt64(countCmd.ExecuteScalar()) == 0)
                SeedDefaults(conn);
        }
    }

    public List<FieldRule> GetEnabledRules() =>
        GetAllRules().Where(r => r.IsEnabled).ToList();

    public void SaveCommunicationLog(CommunicationTrace trace)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO CommunicationLogs
            (Direction, Title, ProcessText, RequestBody, ResponseBody, Success, CreatedAt)
            VALUES ($d, $t, $p, $req, $res, $ok, $c);
            """;
        cmd.Parameters.AddWithValue("$d", trace.Direction);
        cmd.Parameters.AddWithValue("$t", trace.Title);
        cmd.Parameters.AddWithValue("$p", trace.FormatProcessText());
        cmd.Parameters.AddWithValue("$req", trace.RequestBody ?? "");
        cmd.Parameters.AddWithValue("$res", trace.ResponseBody ?? "");
        cmd.Parameters.AddWithValue("$ok", trace.Success ? 1 : 0);
        cmd.Parameters.AddWithValue("$c", DateTime.Now.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }
}
```

> 完整 CRUD 实现见源码 `Services/SqliteStore.cs`，含 6 条预置规则种子数据。

---

### 步骤 8：实现运行日志

**目标**：内存缓冲 + 按日文件滚动，支持级别筛选与实时 UI 更新。

**Services/AppLogger.cs**：

```csharp
namespace HttpsMessenger.Services;

public sealed class AppLogger
{
    private readonly object _sync = new();
    private readonly List<LogEntry> _entries = new();
    public event Action<LogEntry>? EntryAdded;

    public string TodayLogPath =>
        Path.Combine(LogDirectory, $"HttpsMessenger_{DateTime.Now:yyyyMMdd}.log");

    public string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HttpsMessenger", "logs");

    public AppLogger() => Directory.CreateDirectory(LogDirectory);

    public void Info(string msg) => Write("INFO", msg);
    public void Warn(string msg) => Write("WARN", msg);
    public void Error(string msg) => Write("ERROR", msg);

    public void Write(string level, string message)
    {
        var entry = new LogEntry { Time = DateTime.Now, Level = level, Message = message };
        lock (_sync)
        {
            _entries.Add(entry);
            File.AppendAllText(TodayLogPath, entry.ToLine() + Environment.NewLine);
        }
        EntryAdded?.Invoke(entry);
    }

    public IReadOnlyList<LogEntry> GetEntries(string? levelFilter = null) =>
        string.IsNullOrEmpty(levelFilter) || levelFilter == "全部"
            ? _entries.ToList()
            : _entries.Where(e => e.Level == levelFilter).ToList();
}

public sealed class LogEntry
{
    public DateTime Time { get; set; }
    public string Level { get; set; } = "INFO";
    public string Message { get; set; } = "";
    public string ToLine() => $"[{Time:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] {Message}";
}
```

---

### 步骤 9：实现多语言支持

**目标**：中文 / English / 中英对照三模式，切换时刷新全部 UI 文本。

**Localization/Lang.cs**（核心片段）：

```csharp
namespace HttpsMessenger.Localization;

public enum AppLanguage { Chinese, English, Bilingual }

public static class Lang
{
    public static AppLanguage Current { get; private set; } = AppLanguage.Chinese;
    public static event Action? LanguageChanged;

    private static readonly Dictionary<string, (string Zh, string En)> Map = new()
    {
        ["AppTitle"] = ("HttpsMessenger - HTTPS 通讯工具", "HttpsMessenger - HTTPS Messenger"),
        ["StartServer"] = ("启动服务", "Start Server"),
        ["Send"] = ("发送", "Send"),
        ["ResponseSummary"] = ("响应摘要", "Response Summary"),
        ["ReplyInfo"] = ("回复信息", "Reply"),
        // ... 100+ 条目，见源码 Localization/Lang.cs
    };

    public static string T(string key)
    {
        if (!Map.TryGetValue(key, out var pair)) return key;
        return Current switch
        {
            AppLanguage.English => pair.En,
            AppLanguage.Bilingual => $"{pair.Zh} / {pair.En}",
            _ => pair.Zh
        };
    }

    public static void SetLanguage(AppLanguage lang)
    {
        Current = lang;
        LanguageChanged?.Invoke();
    }
}
```

**MainForm 中订阅语言变更**：

```csharp
Lang.LanguageChanged += () => BeginInvoke(ApplyUiLanguage);

private void ApplyUiLanguage()
{
    Text = Lang.T("AppTitle");
    btnStartServer.Text = Lang.T("StartServer");
    btnSend.Text = Lang.T("Send");
    // ... 刷新所有控件文本
}
```

---

### 步骤 10：构建主窗体 UI

**目标**：TabControl 四页签 + SplitContainer 分窗布局 + 事件绑定。

**MainForm 核心字段与构造**：

```csharp
public partial class MainForm : Form
{
    private readonly HttpsChatServer _server = new();
    private readonly HttpsChatClient _client = new();
    private readonly SqliteStore _store = new();
    private readonly AppLogger _logger = new();

    public MainForm()
    {
        Lang.LoadPreference();
        InitializeComponent();  // Designer 生成的控件布局
        WireEvents();
        LoadDefaults();
        ApplyUiLanguage();
    }

    private void WireEvents()
    {
        _server.SetRuleProvider(() => _store.GetEnabledRules());
        _server.MessageReceived += OnServerMessageReceived;
        _server.TraceCompleted += OnServerTraceCompleted;

        btnStartServer.Click += async (_, _) => await StartServerAsync();
        btnSend.Click += async (_, _) => await SendMessageAsync();
        btnRuleAdd.Click += (_, _) => AddRule();
        // ... 更多事件绑定
    }
}
```

**UI 布局要点**（MainForm.Designer.cs）：

```text
Form (可缩放, MinimumSize = 1024×640)
├── MenuStrip（语言 / 帮助）
├── TabControl
│   ├── Tab1: HTTPS 通讯
│   │   ├── 顶部固定区：grpServer + grpPeer（固定高度，不随窗口拉伸）
│   │   └── splitBottom（水平分隔）
│   │       ├── splitMessages（垂直分隔）
│   │       │   ├── rtbSend（发送信息窗口 - 仅回复）
│   │       │   └── rtbRecv（全部响应信息）
│   │       └── grpProcess（全过程 + 响应 JSON）
│   ├── Tab2: 字段规则 → dgvRules + CRUD 按钮
│   ├── Tab3: 通讯历史 → lstHistory + rtbHistoryDetail
│   └── Tab4: 运行日志 → cmbLogLevel + rtbLog
└── StatusStrip（数据库路径 / 日志路径）
```

**发送消息核心逻辑**：

```csharp
private async Task SendMessageAsync()
{
    var message = new ChatMessage
    {
        Sender = txtLocalName.Text.Trim(),
        Content = txtInput.Text.Trim(),
        RequestField = txtReqField.Text.Trim(),
        RequestValue = txtReqValue.Text.Trim()
    };

    _client.IgnoreCertificateErrors = chkIgnoreCert.Checked;
    var (result, trace) = await _client.SendMessageWithTraceAsync(txtPeerUrl.Text, message);

    ShowTrace(trace);                          // 全过程面板
    AppendReplyOnlyToSend(result, trace);      // 发送窗：仅摘要
    AppendFullResponseToRecv(result, trace);   // 响应窗：完整信息
    _store.SaveCommunicationLog(trace);        // 写入 SQLite
    RefreshHistory();
}
```

**发送窗摘要格式**：

```csharp
private void AppendReplyOnlyToSend(ApiResponse response, CommunicationTrace trace)
{
    var replyText = response.CustomFields.GetValueOrDefault("reply")
                    ?? response.Echo?.Content
                    ?? response.Message;
    var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {Lang.T("ResponseSummary")}  " +
               $"HTTP {trace.ResponseStatusCode} {Lang.T(response.Success ? "Success" : "Fail")}" +
               Environment.NewLine + $"{Lang.T("ReplyInfo")}:{replyText}";
    AppendSendLine(text, response.Success ? Color.SteelBlue : Color.Firebrick);
}
```

---

### 步骤 11：实现规则编辑对话框

**目标**：新增/编辑 FieldRule 的弹出窗体。

**RuleEditForm.cs**（核心片段）：

```csharp
public sealed class RuleEditForm : Form
{
    public FieldRule Rule { get; private set; }

    public RuleEditForm(FieldRule? existing = null)
    {
        Rule = existing ?? new FieldRule();
        // 构建表单：名称、请求字段、匹配方式(ComboBox)、请求值、
        //           响应字段、响应值、HTTP状态(NumericUpDown)、响应消息、启用、备注
        _btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text)) return;
            Rule.Name = _txtName.Text.Trim();
            Rule.RequestField = _txtRequestField.Text.Trim();
            Rule.MatchMode = _cmbMatchMode.SelectedItem?.ToString() ?? "Equals";
            Rule.ResponseField = _txtResponseField.Text.Trim();
            Rule.ResponseValue = _txtResponseValue.Text;
            Rule.IsEnabled = _chkEnabled.Checked;
            DialogResult = DialogResult.OK;
        };
    }
}
```

**MainForm 中 CRUD 调用**：

```csharp
private void AddRule()
{
    using var dlg = new RuleEditForm();
    if (dlg.ShowDialog(this) == DialogResult.OK)
    {
        _store.AddRule(dlg.Rule);
        RefreshRulesGrid();
    }
}
```

---

### 步骤 12：编写帮助文档与收尾

**目标**：HTML 帮助、运行环境清单、项目 README、编译配置。

**Help 目录复制到输出**（csproj 已配置）：

```xml
<ItemGroup>
  <None Update="Help\帮助文档.html">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  <None Update="Help\运行环境要求清单.html">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

**F1 打开帮助**：

```csharp
protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
{
    if (keyData == Keys.F1) { OpenHelp(); return true; }
    return base.ProcessCmdKey(ref msg, keyData);
}

private void OpenHelp()
{
    var path = Path.Combine(AppContext.BaseDirectory, "Help", "帮助文档.html");
    if (File.Exists(path))
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
}
```

**最终编译验证**：

```bash
dotnet build -c Release
dotnet run -c Release
```

---

## 项目结构

```text
HttpsMessenger/
├── Program.cs                      # 程序入口
├── MainForm.cs                     # 主窗体逻辑
├── MainForm.Designer.cs            # 主窗体 UI 布局（Designer 生成）
├── RuleEditForm.cs                 # 规则编辑对话框
├── Localization/
│   └── Lang.cs                     # 多语言资源（中/英/对照）
├── Models/
│   ├── ChatMessage.cs              # ChatMessage / ApiResponse / FieldRule / CommunicationTrace
│   └── Mes/                        # MES JSON 模型（后端保留）
├── Services/
│   ├── HttpsChatServer.cs          # HTTPS 服务端（TcpListener + SslStream）
│   ├── HttpsChatClient.cs          # HTTPS 客户端（HttpClient）
│   ├── CertificateHelper.cs        # 自签名证书管理
│   ├── FieldRuleMatcher.cs         # 请求→响应字段规则匹配
│   ├── SqliteStore.cs              # SQLite CRUD + 种子数据
│   ├── AppLogger.cs                # 运行日志（内存 + 文件）
│   └── Mes/                        # MES 后端（API客户端/离线队列/Excel/Mock）
│       ├── MesApiClient.cs
│       ├── MesService.cs
│       ├── MesConfig.cs
│       ├── MesOfflineStore.cs
│       ├── MesProductionExcelLogger.cs
│       └── MesMockHandler.cs
├── Help/
│   ├── 帮助文档.html               # 完整 HTML 帮助
│   └── 运行环境要求清单.html       # 环境依赖清单
├── HttpsMessenger.csproj           # 项目文件
└── README.md                       # 本文件
```

---

## 常见问题 FAQ

### Q1：启动服务失败，提示端口被占用？

修改端口为其他值（如 `9443`），或在命令行执行 `netstat -ano | findstr 8443` 查找占用进程。

### Q2：客户端连接失败，证书错误？

勾选 **「忽略证书错误」**。若需严格校验，将对端安装 `%LocalAppData%\HttpsMessenger\server.cer` 到「受信任的根证书颁发机构」。

### Q3：发送消息后响应窗有内容，发送窗没有摘要？

发送窗仅在收到 HTTP 响应后显示摘要。若连接失败，错误信息会出现在「全部响应信息」窗口。

### Q4：规则不生效？

确认规则已 **启用**；请求 JSON 中对应字段名与 **请求字段** 一致；匹配方式与 **请求值** 正确。可在「通讯过程历史」查看 `matchedRules`。

### Q5：两台电脑如何互联？

1. 电脑 A 启动服务，防火墙放行端口
2. 电脑 B 对端地址填 `https://电脑A的IP:8443`
3. 勾选忽略证书错误（或导入 A 的 CER 证书）

### Q6：数据库在哪里？如何备份？

路径见 [数据存储路径](#数据存储路径)。直接复制 `https_messenger.db` 即可备份规则与历史。

### Q7：MES 功能如何使用？

当前版本 UI 已移除 MES 页签，但本机 HTTPS 服务仍内置 **MES Mock 路由**（`POST /mes/*` + `jsonData` 表单），可用于接口联调测试。完整 MES 客户端代码在 `Services/Mes/` 目录，可按需重新集成 UI。

---

## 依赖包

| 包名 | 版本 | 用途 |
|------|------|------|
| Microsoft.Data.Sqlite | 10.0.11 | SQLite 数据库 |
| ClosedXML | 0.105.1 | MES Excel 生产日志（后端） |

---

## 许可证

本项目仅供学习、实验与现场联调使用。自签名证书仅适用于开发环境，生产环境请使用正规 CA 签发的证书。

---

*HttpsMessenger v1.3 · 最后更新：2026-09-03*
