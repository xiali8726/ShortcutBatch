# ShortcutBatch

批量为文件创建快捷方式（`.lnk`）并统一放入指定文件夹的 Windows 小工具。既可以在图形界面里点选操作，也可以在命令行里用参数批量执行。

## 功能

- 源可为**文件**或**文件夹**，可混合添加多个；文件夹可选递归子目录。
- 可按文件名通配符过滤（如 `*.exe;*.txt`），留空则全部。
- 目标文件夹不存在时自动创建。
- 冲突规则：目标中已有同名 `.lnk` 时，若其指向同一源文件则**覆盖**；否则**自动改名**（如 `名称 (2).lnk`）保留两者。
- **目标默认值**：留空目标时自动套用「源文件所在文件夹 / 固定文件夹 / 上次的目标文件夹」之一（可在界面下拉里选）；固定文件夹支持 `~` 表示用户目录。
- **导入 / 导出列表**：把当前源文件表导出为文本（每行一个路径），或从文本导入批量添加（`#` 开头为注释，空行与重复项自动跳过）。
- **记住上次路径**：添加文件/文件夹对话框会记住上次打开的目录，重启不丢。
- **默认以管理员身份运行**：避免因目标目录（如 `C:\Program Files`）权限不足而创建失败。
- 双模式：带参数走命令行，不带参数打开图形界面。

## 用法

### 图形界面

直接双击 `ShortcutBatch.exe`（或不带任何参数运行）。首次会弹 UAC 请求管理员权限，点「是」即可。

### 命令行

```
ShortcutBatch.exe -Source <文件或文件夹> -Target <文件夹> [选项]
```

| 参数 | 说明 |
| --- | --- |
| `-Source <路径>` | 源文件或文件夹；多个用分号分隔，或多次指定（必填） |
| `-Target <路径>` | 目标文件夹，不存在时自动创建（必填） |
| `-Recurse` | 源为文件夹时递归处理子文件夹 |
| `-Filter <通配>` | 按文件名过滤，多个用分号分隔，如 `*.exe;*.txt` |
| `-Help` | 显示帮助 |

示例：

```
ShortcutBatch.exe -Source D:\tools -Target D:\快捷方式 -Recurse -Filter *.exe
ShortcutBatch.exe -Source "D:\a.txt;D:\b.txt" -Target D:\快捷方式
```

## 编译

本机无需安装 .NET SDK，用系统自带的 .NET Framework 4.x 编译器即可：

```
powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
```

产出单文件 `ShortcutBatch.exe`（.NET Framework 4 运行环境，Windows 10 已内置）。用户设置保存在 exe 同目录的 `ShortcutBatch.ini`。

## 许可证

[MIT](LICENSE)
