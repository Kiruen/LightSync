# LightSync
## 项目介绍

LightSync 是一款超轻量级文件同步工具，旨在简化文件备份的过程。它利用了everything的 `es` 命令行工具获取，实现了文件同步的功能。相较于其他文件备份工具，LightSync 不需要完整分析目录结构，因此备份速度更快，特别适用于包含大量子目录和文件的大目录的备份工作。

目前，LightSync 已经实现了实时备份和全量备份的基本功能。然而，它的功能还不完善，并且尚未进行充分的测试，因此无法保证备份的文件安全性。此外，由于 LightSync 依赖于everything和其命令行工具，因此它无法获取完整的文件变更信息。未来的计划是模仿everything的做法，通过文件系统日志来精确分析文件的变更情况，从而确保备份的绝对完整性，同时兼顾极快的目录分析速度。

## 如何使用

1. 在"源路径"中输入备份源的路径，按Enter加入到列表，每个Job可支持备份多个源目录。
2. 在"目标路径"中输入备份目标的路径。
3. 点击 "添加" 按钮，向任务列表中加入当前创建的备份任务。
4. 点击 "全量" 按钮开始全量备份，备份完毕后会生成一个目录文件记录records.txt。
5. 点击 "开始" 按钮开始实时备份，LightSync 将监控备份源目录中的文件变化，并将添加或修改的文件同步到备份目标目录。

## 要求

LightSync 需要以下环境和依赖：

- Windows 操作系统
- 安装了 .NET Framework 4.7.2 或更高版本
- 安装了 Everything 软件

## 贡献

欢迎对 LightSync 的改进和贡献！如果您发现问题或有任何建议，请在 GitHub 上提出 issue 或提交 pull 请求。

## 许可证

LightSync 采用 MIT 许可证进行发布。请查阅 LICENSE 文件获取更多详细信息。

## 链接

- LightSync GitHub 仓库：[https://github.com/your-repo-url]([https://github.com/your-repo-url](https://github.com/Kiruen/LightSync)
- Everything 官方网站：[https://www.voidtools.com/zh-cn/](https://www.voidtools.com/zh-cn/)
