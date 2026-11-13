<div align="center">

# PlanPeople

**纯本地 · AI 智能 · 云端加密同步的 Windows 效率工具**

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-blue?logo=windows)](https://github.com/Azurelab-Azure/PlanPeople)
[![Framework](https://img.shields.io/badge/.NET-8.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![UI](https://img.shields.io/badge/UI-Avalonia%20%2B%20PleasantUI-5C2D91)](https://avaloniaui.net/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

</div>

---

## 简介

**PlanPeople** 是一款专为 Windows 打造的现代化日程管理工具。我们相信数据应该属于用户自己，因此 PlanPeople 默认采用 **纯本地 SQLite 存储**，并提供基于 **WebDAV + AES-256 端到端加密** 的安全同步方案。

通过接入兼容 OpenAI 的大模型（如智谱 GLM、OpenAI），你可以用最自然的语言（如*"明天下午三点和客户开会"*）一秒创建日程。同时，我们内置了桌面灵动岛、双屏 Markdown 编辑器，让效率与颜值并存。

---

## 核心特性

- 🚀 **AI 自然语言解析**：接入智谱/OpenAI，直接将对话转化为结构化日程。
- 🔒 **端到端加密**：WebDAV 同步全程采用 AES-256-GCM 加密，云端看不到明文。
- 📅 **现代化日程管理**：今日/周视图切换、多优先级标签、CSV / ICS 导入导出。
- 📝 **沉浸式文档编辑**：左写右预览的双屏 Markdown 编辑器，支持一键 AI 润色。
- 📱 **桌面灵动岛**：极简悬浮胶囊/顶部栏，实时显示天气、系统状态、热搜与日程。
- 🎨 **多主题与多语言**：WinUI 3 风格，支持浅色、深色、冰蓝、云杉，及中繁英日 4 种语言。

---

## 下载与安装

> 当前版本：`v1.0.0` (Windows x64 绿色版)

1. 前往 [Releases](https://github.com/Azurelab-Azure/PlanPeople/releases) 页面。
2. 下载最新的 `PlanPeople.zip` 压缩包。
3. 解压后，双击 `PlanPeople.App.exe` 即可运行（无需安装任何运行环境）。

---

## 生态与相关产品

PlanPeople 是我们开源生态的一部分。如果你也在关注教育或效率工具，可以了解一下我们的另一款产品：

- **[MyClassroom / MyPlan](https://github.com/Azurelab-Azure/myclassroom/releases/download/v1.1.5/My-Plan-Setup-1.1.5.exe)**：
  一款专为教育场景打造的全能排课与学习规划工具。

---

## 开发环境

如果你想编译或参与贡献代码，请确保你安装了：

- Windows 10 1809+ / 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) 或 [VS Code](https://code.visualstudio.com/) 

执行 `dotnet build` 即可完成编译。

---

## 开源协议

本项目基于 [MIT License](LICENSE) 开源。欢迎提交 PR 或提出 Issue。