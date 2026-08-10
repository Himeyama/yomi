using System.IO;
using Microsoft.Win32.TaskScheduler;

namespace Yomi.App.Services;

/// <summary>
/// タスクスケジューラーにログオントリガー・最高特権(Highest)でタスクを登録することで、
/// 通常起動時にUACプロンプトを出さずに管理者権限でアプリを自動起動できるようにする。
/// タスク登録自体は管理者権限が必要な操作のため、呼び出し元で昇格を担保すること。
/// </summary>
public sealed class AutoStartService
{
    private const string TaskFolderName = "yomi";
    private const string TaskName = "yomi_AutoStart";
    private static string TaskPath => $"\\{TaskFolderName}\\{TaskName}";

    public bool IsRegistered()
    {
        using var ts = new TaskService();
        return ts.GetTask(TaskPath) is not null;
    }

    public void Register(string exePath)
    {
        using var ts = new TaskService();
        var td = ts.NewTask();
        td.RegistrationInfo.Description = "yomi デスクトップモニターの自動起動";
        td.Principal.RunLevel = TaskRunLevel.Highest;
        td.Principal.LogonType = TaskLogonType.InteractiveToken;

        td.Triggers.Add(new LogonTrigger());
        td.Actions.Add(new ExecAction(exePath, null, Path.GetDirectoryName(exePath)));

        td.Settings.DisallowStartIfOnBatteries = false;
        td.Settings.StopIfGoingOnBatteries = false;
        td.Settings.ExecutionTimeLimit = TimeSpan.Zero;
        td.Settings.AllowDemandStart = true;

        var folder = ts.RootFolder.SubFolders.Exists(TaskFolderName)
            ? ts.GetFolder($"\\{TaskFolderName}")
            : ts.RootFolder.CreateFolder(TaskFolderName);

        folder.RegisterTaskDefinition(TaskName, td);
    }

    public void Unregister()
    {
        using var ts = new TaskService();
        var folder = ts.GetFolder($"\\{TaskFolderName}");
        folder?.DeleteTask(TaskName, exceptionOnNotExists: false);
    }
}
