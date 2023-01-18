using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core
{
    public interface ILog
    {
        void Debug(string tag, string message);
        void User(string tag, string message);
        void Warn(string tag, string message);
        void Error(string tag, string message);
    }

    internal class DefaultConsoleLog : ILog
    {
        enum LogType
        {
            Debug,
            User,
            Warning,
            Error,
        }

        private string BuildString(LogType logType, string tag, string message)
        {
            var time = DateTime.Now;
            return $"{time.ToShortTimeString()} [{logType}:{Thread.CurrentThread.ManagedThreadId}] {tag}: {message}";
        }

        private void Output(LogType logType, string tag, string message)
        {
            var fullMessageText = BuildString(logType, tag, message);
            var b = Console.ForegroundColor;

            Console.ForegroundColor = logType switch
            {
                LogType.User => ConsoleColor.Green,
                LogType.Warning => ConsoleColor.Yellow,
                LogType.Error => ConsoleColor.Red,
                LogType.Debug or _ => ConsoleColor.White,
            };

            Console.WriteLine(fullMessageText);

            Console.ForegroundColor = b;
        }

        public void Debug(string tag, string message) => Output(LogType.Debug, tag, message);
        public void User(string tag, string message) => Output(LogType.User, tag, message);
        public void Warn(string tag, string message) => Output(LogType.Warning, tag, message);
        public void Error(string tag, string message) => Output(LogType.Error, tag, message);
    }

    public static class Log
    {
        public static ILog Impl { internal get; set; } = new DefaultConsoleLog();

        public static void Debug(string tag, string message) => Impl?.Debug(tag, message);
        public static void User(string tag, string message) => Impl?.User(tag, message);
        public static void Warn(string tag, string message) => Impl?.Warn(tag, message);
        public static void Error(string tag, string message) => Impl?.Error(tag, message);

        public static void Debug(string message) => Impl?.Debug(string.Empty, message);
        public static void User(string message) => Impl?.User(string.Empty, message);
        public static void Warn(string message) => Impl?.Warn(string.Empty, message);
        public static void Error(string message) => Impl?.Error(string.Empty, message);
    }

    internal static class Log<T>
    {
        private static readonly string TAG;
        static Log() => TAG = typeof(T).Name;

        public static void Debug(string message) => Log.Debug(TAG, message);
        public static void User(string message) => Log.User(TAG, message);
        public static void Warn(string message) => Log.Warn(TAG, message);
        public static void Error(string message) => Log.Error(TAG, message);
    }
}
