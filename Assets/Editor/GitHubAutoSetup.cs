using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>
/// プロジェクトを初めて開いたとき（.git がない場合）に
/// セットアップウィンドウを自動表示する。
/// </summary>
[InitializeOnLoad]
internal static class GitHubAutoSetup
{
    private const string SessionKey = "GitHubAutoSetup.Done";

    static GitHubAutoSetup()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);

        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (Directory.Exists(Path.Combine(root, ".git"))) return;

        EditorApplication.delayCall += () => GitHubSetupWindow.Open(root);
    }
}

internal class GitHubSetupWindow : EditorWindow
{
    private string _repoName    = "";
    private string _description = "";
    private bool   _isPrivate   = false;
    private string _log         = "";
    private bool   _running     = false;
    private bool   _done        = false;
    private string _projectRoot = "";

    internal static void Open(string projectRoot)
    {
        var win = GetWindow<GitHubSetupWindow>(true, "GitHub 自動セットアップ", true);
        win._projectRoot = projectRoot;
        win._repoName    = Path.GetFileName(projectRoot);
        win.minSize      = new Vector2(480, 300);
        win.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("GitHub 自動セットアップ", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(".git が見つかりません。GitHubリポジトリを作成します。",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(8);

        GUI.enabled = !_running && !_done;

        EditorGUILayout.LabelField("リポジトリ名");
        _repoName = EditorGUILayout.TextField(_repoName);

        EditorGUILayout.LabelField("説明（省略可）");
        _description = EditorGUILayout.TextField(_description);

        _isPrivate = EditorGUILayout.Toggle("プライベートにする", _isPrivate);

        EditorGUILayout.Space(10);
        GUI.enabled = !_running && !_done && !string.IsNullOrWhiteSpace(_repoName);

        if (GUILayout.Button("git init → GitHub 作成 → push", GUILayout.Height(36)))
            _ = RunAsync();

        GUI.enabled = true;

        if (_running)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox("実行中... Unityは操作可能です。", MessageType.Info);
        }

        if (!string.IsNullOrEmpty(_log))
        {
            EditorGUILayout.Space(8);
            var style = _done ? MessageType.Info : (_running ? MessageType.Info : MessageType.Warning);
            EditorGUILayout.HelpBox(_log, style);
        }

        if (_done && GUILayout.Button("閉じる"))
            Close();
    }

    private async Task RunAsync()
    {
        _running = true;
        _log     = "処理を開始しました...\n";
        Repaint();

        // すべての重い処理をバックグラウンドスレッドで実行
        string result = await Task.Run(() => RunCommands());

        _log    = result;
        _done   = !result.Contains("失敗");
        _running = false;
        Repaint();
    }

    private string RunCommands()
    {
        var log = "";

        // 1. git init
        if (!Exec("git", "init", out string o1))
            return $"git init 失敗:\n{o1}";
        log += "✓ git init\n";

        // 2. .gitignore
        WriteGitignore();
        log += "✓ .gitignore 生成\n";

        // 3. 初回コミット
        Exec("git", "add -A", out _);
        Exec("git", "commit -m \"Initial commit (auto-setup)\"", out _);
        log += "✓ 初回コミット\n";

        // 4. GitHub リポジトリ作成 & push
        string vis  = _isPrivate ? "--private" : "--public";
        string desc = string.IsNullOrWhiteSpace(_description) ? "" : $"--description \"{_description}\"";
        string args = $"repo create {_repoName} {vis} {desc} --source . --remote origin --push";

        if (!Exec("gh", args, out string o4))
            return log + $"\ngh repo create 失敗:\n{o4}";

        log += "✓ GitHub リポジトリ作成 & push 完了！\n";
        log += $"→ https://github.com/gd071/{_repoName}";
        return log;
    }

    private bool Exec(string cmd, string args, out string output)
    {
        // PATH を Machine + User 両方から読む
        string path = string.Join(";",
            System.Environment.GetEnvironmentVariable("PATH", System.EnvironmentVariableTarget.Machine) ?? "",
            System.Environment.GetEnvironmentVariable("PATH", System.EnvironmentVariableTarget.User)    ?? "");

        string exe = cmd == "gh" ? FindExe("gh.exe", path) ?? "gh" : cmd;

        var psi = new ProcessStartInfo
        {
            FileName               = exe,
            Arguments              = args,
            WorkingDirectory       = _projectRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
        };

        try
        {
            using var proc = Process.Start(psi);
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            output = (stdout + stderr).Trim();
            return proc.ExitCode == 0;
        }
        catch (System.Exception e)
        {
            output = e.Message;
            return false;
        }
    }

    private static string FindExe(string exe, string pathVar)
    {
        foreach (string dir in pathVar.Split(';'))
        {
            try
            {
                string full = Path.Combine(dir.Trim(), exe);
                if (File.Exists(full)) return full;
            }
            catch { }
        }
        return null;
    }

    private void WriteGitignore()
    {
        string path = Path.Combine(_projectRoot, ".gitignore");
        if (File.Exists(path)) return;
        File.WriteAllText(path, @"[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
MemoryCaptures/
.DS_Store
*.pidb
*.suo
*.user
*.csproj
*.sln
.idea/
.vs/
");
    }
}
