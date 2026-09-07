# DotNetCoreRpc基于.NetCore的RPC框架

#### 前言
一直以来都想实现一个简单的RPC框架。.net core不断完善之后借助其自身的便利实现一个RPC框架。框架分Server端和Client端两部分。Client端可在Console或Web端等，能运行.net core的host上运行。Server端依赖Asp.Net Core,接下来介绍大致使用，详细介绍请参阅 [https://www.cnblogs.com/wucy/p/13096515.html](https://www.cnblogs.com/wucy/p/13096515.html)

#### 运行环境
- visual studio 2022
- 核心库 `DotNetCoreRpc.Core` 目标 `.netstandard2.1`
- 客户端/服务端库多目标 `netcoreapp3.1;net5.0;net6.0;net7.0;net8.0;net9.0`
- 服务端依赖 asp.net core 6.0 及以上（推荐 asp.net core 8.0/9.0 LTS）

#### 序列化协议
- 默认使用 **MessagePack** 二进制序列化（`TypelessContractlessStandardResolver`，自动携带类型标记以还原 `object` 多态参数/返回值）。
- 核心库仅依赖 `MessagePack`，已与 `System.Text.Json` 解耦；`System.Text.Json` 仅用于 demo 层日志与控制台的中文 JSON 打印。
- 协议升级需 **客户端与服务端同步部署**，原有 JSON 负载与之不兼容。

#### 测试与验证
- 单元测试：`dotnet test`（`tests/DotNetCoreRpc.Core.Tests`）。
- 集成测试：`tests/DotNetCoreRpc.Tests.Integration`——基于 `WebApplicationFactory` 的进程内宿主 + 真实 `RpcClient` 走完整 HTTP 管道。
- 端到端冒烟脚本：`pwsh ./e2e-smoke.ps1`——真实进程 + 真实端口的服务端/客户端联调。

#### 使用方式
由于`v1.1.3之后版本`优化了一下client和server端的注册方式，所以分两个文档介绍。
+ [v1.1.3及之前版本](https://github.com/softlgl/DotNetCoreRpc/blob/master/docs/1.1.3及之前版本.md)
+ [v1.1.3之后版本](https://github.com/softlgl/DotNetCoreRpc/blob/master/docs/1.1.3更高版本.md)
