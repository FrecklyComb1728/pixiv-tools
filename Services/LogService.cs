using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace PixivTools.Services;

public static class LogService
{
    private static ILoggerFactory? _factory;
    private static readonly object Lock = new();

    public static ILogger<T> GetLogger<T>()
    {
        if (_factory == null)
        {
            lock (Lock)
            {
                _factory ??= LoggerFactory.Create(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Debug);
                    builder.AddFile("log.txt", LogLevel.Debug);
                    builder.AddConsole();
                });
            }
        }
        return _factory!.CreateLogger<T>();
    }

    private static ILoggingBuilder AddFile(this ILoggingBuilder builder, string path, LogLevel level)
    {
        builder.AddProvider(new FileLoggerProvider(path, level));
        return builder;
    }
}

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _path;
    private readonly LogLevel _level;
    private StreamWriter? _writer;

    public FileLoggerProvider(string path, LogLevel level)
    {
        _path = path;
        _level = level;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, this);
    }

    public void Dispose()
    {
        _writer?.Dispose();
    }

    public void Write(string message)
    {
        if (_writer == null)
        {
            _writer = new StreamWriter(_path, append: true) { AutoFlush = true };
        }
        _writer.WriteLine(message);
    }
}

public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLoggerProvider _provider;

    public FileLogger(string categoryName, FileLoggerProvider provider)
    {
        _categoryName = categoryName;
        _provider = provider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff} - {_categoryName}[{eventId.Id}][{Thread.CurrentThread.Name ?? "unnamed"}, {Environment.CurrentManagedThreadId}] - {logLevel}: {message}";
        if (exception != null)
            line += $"\n{exception}";
        _provider.Write(line);
    }
}
