using SuchByte.MacroDeck.Logging;
using System;

namespace FuSan21.MacroDeck.GoogleMeet
{
    public class AppLogger
    {
        public void Error(string message, params object[] args)
        {
            if (args.Length > 0) message = String.Format(message, args);
            MacroDeckLogger.Error(PluginInstance.Plugin, "[ERROR]: {Message}", message);
        }

        public void Info(string message, params object[] args)
        {
            if (args.Length > 0) message = String.Format(message, args);
            MacroDeckLogger.Information(PluginInstance.Plugin, "[INFO]: {Message}", message);
        }

        public void Trace(string message, params object[] args)
        {
            if (args.Length > 0) message = String.Format(message, args);
            MacroDeckLogger.Debug(PluginInstance.Plugin, "[TRACE]: {Message}", message);
        }

        public void Warning(string message, params object[] args)
        {
            if (args.Length > 0) message = String.Format(message, args);
            MacroDeckLogger.Warning(PluginInstance.Plugin, "[WARN]: {Message}", message);
        }
    }
}
