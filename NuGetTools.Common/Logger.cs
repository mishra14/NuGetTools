using System;
using System.Diagnostics;
using Microsoft.Azure.WebJobs.Host;

namespace NuGetTools.Common
{
    public class Logger
    {
        private TraceWriter _traceWriter;

        public Logger(TraceWriter traceWriter = null)
        {
            _traceWriter = traceWriter;
        }

        public void Error(string message)
        {
            Log(TraceLevel.Error, message);
        }

        public void Info(string message)
        {
            Log(TraceLevel.Info, message);
        }

        public void Verbose(string message)
        {
            Log(TraceLevel.Verbose, message);
        }

        public void Warning(string message)
        {
            Log(TraceLevel.Warning, message);
        }

        private void Log(TraceLevel level, string message)
        {
            LogToConsole(message);
            LogToTraceWriter(TraceLevel.Error, message);
        }

        private void LogToConsole(string message)
        {
            Console.WriteLine(message);
        }

        private void LogToTraceWriter(TraceLevel level, string message)
        {
            _traceWriter.Trace(new TraceEvent(level, message));
        }

        private void LogToTraceWriter(TraceEvent traceEvent)
        {
            if (_traceWriter != null)
            {
                _traceWriter.Trace(traceEvent);
            }
        }
    }
}
