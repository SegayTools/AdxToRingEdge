using Iot.Device.Card.CreditCardProcessing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        static DefaultConsoleLog()
        {
            File.Delete(@"ATRE.log");
        }

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
            return $"{time} [{logType}:{Thread.CurrentThread.ManagedThreadId}] {tag}: {message}{Environment.NewLine}";
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

            Console.Write(fullMessageText);
            Console.ForegroundColor = b;

            File.AppendAllText(@"ATRE.log", fullMessageText);
        }

        public void Debug(string tag, string message)
        {
            if (ProgramArgumentOption.Instance.IsDebug)
                Output(LogType.Debug, tag, message);
        }

        public void User(string tag, string message) => Output(LogType.User, tag, message);
        public void Warn(string tag, string message) => Output(LogType.Warning, tag, message);
        public void Error(string tag, string message) => Output(LogType.Error, tag, message);
    }

    public static class Log
    {
        public interface ITaggedLog
        {
            void Debug(string message);
            void User(string message);
            void Warn(string message);
            void Error(string message);
        }

        private class TaggedLog : ITaggedLog
        {
            private readonly string tag;

            public TaggedLog(string tag)
            {
                this.tag = tag;
            }

            public void Debug(string message) => Log.Debug(tag, message);
            public void User(string message) => Log.User(tag, message);
            public void Warn(string message) => Log.Warn(tag, message);
            public void Error(string message) => Log.Error(tag, message);
        }

        public static ITaggedLog CreateTaggedLog(string tag) => new TaggedLog(tag);

        public static ILog Impl { internal get; set; } = new DefaultConsoleLog();

        [Conditional("DEBUG")]
        public static void Debug(string tag, string message) => Impl?.Debug(tag, message);
        public static void User(string tag, string message) => Impl?.User(tag, message);
        public static void Warn(string tag, string message) => Impl?.Warn(tag, message);
        public static void Error(string tag, string message) => Impl?.Error(tag, message);

        [Conditional("DEBUG")]
        public static void Debug(string message) => Impl?.Debug(string.Empty, message);
        public static void User(string message) => Impl?.User(string.Empty, message);
        public static void Warn(string message) => Impl?.Warn(string.Empty, message);
        public static void Error(string message) => Impl?.Error(string.Empty, message);
    }

    internal static class Log<T>
    {
        private static readonly string TAG;
        static Log() => TAG = typeof(T).Name;

        [Conditional("DEBUG")]
        public static void Debug(string message) => Log.Debug(TAG, message);
        public static void User(string message) => Log.User(TAG, message);
        public static void Warn(string message) => Log.Warn(TAG, message);
        public static void Error(string message) => Log.Error(TAG, message);
    }
}
