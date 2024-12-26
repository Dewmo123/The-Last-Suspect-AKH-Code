using System;
using System.IO;
using System.Threading;

public class FileLogger
{
    private readonly string logFilePath;
    private readonly ReaderWriterLockSlim _lock;
    private readonly bool includeTimestamp;

    public FileLogger(string filePath, bool includeTimestamp = true)
    {
        Console.WriteLine(filePath);
        logFilePath = filePath;
        _lock = new ReaderWriterLockSlim();
        this.includeTimestamp = includeTimestamp;

        // 로그 파일 디렉토리가 없으면 생성
        string directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    public void LogInfo(string message)
    {
        WriteToFile("INFO", message);
    }

    public void LogWarning(string message)
    {
        WriteToFile("WARNING", message);
    }

    public void LogError(string message)
    {
        WriteToFile("ERROR", message);
    }

    private void WriteToFile(string level, string message)
    {
        try
        {
            _lock.EnterWriteLock();
            string logMessage = includeTimestamp
                ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}"
                : $"[{level}] {message}";

            using (StreamWriter sw = File.AppendText(logFilePath))
            {
                sw.WriteLine(logMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"로그 작성 중 오류 발생: {ex.Message}");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public string[] ReadLogs()
    {
        try
        {
            _lock.EnterReadLock();
            if (File.Exists(logFilePath))
            {
                return File.ReadAllLines(logFilePath);
            }
            return new string[0];
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void ClearLogs()
    {
        try
        {
            _lock.EnterWriteLock();
            File.WriteAllText(logFilePath, string.Empty);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}

/*
var logger = new FileLogger("C:\\Logs\\application.log");

// 로그 작성
logger.LogInfo("애플리케이션 시작");
logger.LogWarning("경고 메시지");
logger.LogError("오류가 발생했습니다");

// 로그 읽기
string[] logs = logger.ReadLogs();

// 로그 초기화
logger.ClearLogs();
*/