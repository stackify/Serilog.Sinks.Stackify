// Copyright 2015 John Knoll <jpknoll@gmail.com>
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using Serilog.Core;
using Serilog.Events;
using StackifyLib;
using StackifyLib.Internal.Logs;
using StackifyLib.Models;
using StackifyLib.Utils;

namespace Serilog.Sinks.Stackify
{
    public class StackifySink : ILogEventSink, IDisposable
    {
        private readonly ErrorGovernor _Governor = new ErrorGovernor();
        private readonly IFormatProvider _formatProvider;
        private readonly string _apiKey;
        private readonly string _appName;
        private readonly string _environment;
        private readonly JsonDataFormatter _dataFormatter;
        private LogClient _logClient = null;

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        public StackifySink(IFormatProvider formatProvider, string apiKey, string appName, string environment)
        {
            _formatProvider = formatProvider;
            _apiKey = apiKey;
            _appName = appName;
            _environment = environment;

            _dataFormatter = new JsonDataFormatter();
            _logClient = new LogClient("StackifyLib.net-serilog", null, null);
            
            Config.ApiKey = apiKey;
            Config.AppName = appName;
            Config.Environment = environment;
        }


        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            if (StackifyLib.Logger.PrefixEnabled() || StackifyLib.Logger.CanSend())
            {
                var msg = new LogMsg()
                {
                    Level = LevelToSeverity(logEvent),
                    Msg = logEvent.RenderMessage(_formatProvider),
                    data = PropertiesToData(logEvent)
                };

                StackifyError error = null;

                Exception ex = logEvent.Exception;

                if (ex == null)
                {
                    if (logEvent.Level == LogEventLevel.Error || logEvent.Level == LogEventLevel.Fatal)
                    {
                        StringException stringException = new StringException(msg.Msg);
                        stringException.TraceFrames = StackifyLib.Logger.GetCurrentStackTrace(null);
                        error = StackifyError.New(stringException);
                    }
                }
                else if (ex is StackifyError)
                {
                    error = (StackifyError)ex;
                }
                else
                {
                    error = StackifyError.New(ex);
                }

                if (error != null && !StackifyError.IgnoreError(error) && _Governor.ErrorShouldBeSent(error))
                {
                    msg.Ex = error;
                }

                _logClient.QueueMessage(msg);
            }
        }

        private string PropertiesToData(LogEvent logEvent)
        {
            var payload = new StringWriter();
            _dataFormatter.FormatData(logEvent, payload);

            var data = payload.ToString();

            if (data == "{}")
                return null;

            return data;
        }

        static string LevelToSeverity(LogEvent logEvent)
        {
            switch (logEvent.Level)
            {
                case LogEventLevel.Debug:
                    return "DEBUG";
                case LogEventLevel.Error:
                    return "ERROR";
                case LogEventLevel.Fatal:
                    return "FATAL";
                case LogEventLevel.Verbose:
                    return "VERBOSE";
                case LogEventLevel.Warning:
                    return "WARNING";
                default:
                    return "INFORMATION";
            }
        }

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                StackifyLib.Utils.StackifyAPILogger.Log("Serilog target closing");
                _logClient.Close();
                StackifyLib.Internal.Metrics.MetricClient.StopMetricsQueue("Serilog CloseTarget");
            }
            catch (Exception ex)
            {
                StackifyLib.Utils.StackifyAPILogger.Log("Serilog target closing error: " + ex.ToString());
            }
            _disposed = true;
        }
    }
}
