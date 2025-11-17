using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Unityのコンソールログをテキストファイルに出力するユーティリティ
/// </summary>
public class LogToFile : MonoBehaviour
{
    [Header("Log Settings")]
    [SerializeField] private bool m_enableLogging = true;
    [SerializeField] private bool m_logToConsole = true; // コンソールにも出力するか
    [SerializeField] private bool m_includeStackTrace = false; // スタックトレースを含めるか

    private string m_logFilePath;
    private StreamWriter m_logWriter;

    private void Awake()
    {
        // シーンをまたいで保持
        DontDestroyOnLoad(gameObject);

        if (!m_enableLogging) return;

        // ログファイルのパスを設定（プロジェクトのLogsフォルダに出力）
        string projectPath = Application.dataPath; // Assets フォルダのパス
        string logsFolder = Path.Combine(Directory.GetParent(projectPath).FullName, "Logs");

        // Logs フォルダが存在しない場合は作成
        if (!Directory.Exists(logsFolder))
        {
            Directory.CreateDirectory(logsFolder);
        }

        string fileName = "LatestLog.txt"; // 固定ファイル名（毎回上書き）
        m_logFilePath = Path.Combine(logsFolder, fileName);

        // StreamWriterを初期化
        try
        {
            m_logWriter = new StreamWriter(m_logFilePath, false);
            m_logWriter.AutoFlush = true; // 自動フラッシュを有効化

            // ログハンドラーを登録
            Application.logMessageReceived += HandleLog;

            WriteToFile($"=== ログ記録開始 ===");
            WriteToFile($"日時: {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
            WriteToFile($"ファイル: {m_logFilePath}");
            WriteToFile($"==============================\n");

            Debug.Log($"[LogToFile] ログファイルを作成しました: {m_logFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LogToFile] ログファイルの作成に失敗: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        if (!m_enableLogging) return;

        // ログハンドラーを解除
        Application.logMessageReceived -= HandleLog;

        // ファイルを閉じる
        if (m_logWriter != null)
        {
            WriteToFile($"\n=== ログ記録終了 ===");
            WriteToFile($"日時: {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
            WriteToFile($"==============================");

            m_logWriter.Close();
            m_logWriter = null;

            if (m_logToConsole)
            {
                Debug.Log($"[LogToFile] ログファイルを保存しました: {m_logFilePath}");
            }
        }
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (!m_enableLogging || m_logWriter == null) return;

        // タイムスタンプを追加
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

        // ログタイプに応じたプレフィックス
        string prefix = type switch
        {
            LogType.Error => "[ERROR]",
            LogType.Warning => "[WARNING]",
            LogType.Log => "[LOG]",
            LogType.Exception => "[EXCEPTION]",
            LogType.Assert => "[ASSERT]",
            _ => "[UNKNOWN]"
        };

        // ログを書き込み
        WriteToFile($"[{timestamp}] {prefix} {logString}");

        // スタックトレースを含める場合
        if (m_includeStackTrace && !string.IsNullOrEmpty(stackTrace))
        {
            WriteToFile($"  Stack Trace:\n{stackTrace}");
        }

        WriteToFile(""); // 空行を追加
    }

    private void WriteToFile(string message)
    {
        if (m_logWriter != null)
        {
            m_logWriter.WriteLine(message);
        }
    }

    /// <summary>
    /// ログ記録のオン/オフを切り替え
    /// </summary>
    public void ToggleLogging(bool enable)
    {
        m_enableLogging = enable;
    }

    /// <summary>
    /// スタックトレース記録のオン/オフを切り替え
    /// </summary>
    public void ToggleStackTrace(bool enable)
    {
        m_includeStackTrace = enable;
    }
}
