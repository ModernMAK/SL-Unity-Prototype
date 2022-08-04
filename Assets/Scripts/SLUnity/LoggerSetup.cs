using System;
using OpenMetaverse;
using UnityEngine;

namespace SLUnity
{
    public class LoggerSetup : MonoBehaviour
    {
        [Flags]
        public enum LogLevelFlags
        {
            Info = 0x1,
            Debug = 0x2,
            Warning = 0x4,
            Error = 0x8
        }

        public LogLevelFlags LogLevels = (LogLevelFlags)0x15;
        private static bool _setup = false;
    
    
        void OnEnable()
        {
            if(!_setup)
                OpenMetaverse.Logger.OnLogMessage += LoggerOnOnLogMessage;
            _setup = true;
        }

        private void OnDisable()
        {
            _setup = false;
            OpenMetaverse.Logger.OnLogMessage -= LoggerOnOnLogMessage;
        }

        private void LoggerOnOnLogMessage(object message, Helpers.LogLevel level)
        {
            switch (level)
            {
                case Helpers.LogLevel.None:
                case Helpers.LogLevel.Info:
                    if (LogLevels.HasFlag(LogLevelFlags.Info))
                        Debug.Log(level + "\n" + message);
                    break;
                case Helpers.LogLevel.Debug:
                    if (LogLevels.HasFlag(LogLevelFlags.Debug))
                        Debug.Log( level+ "\n"+message);
                    break;
                case Helpers.LogLevel.Warning:
                    if (LogLevels.HasFlag(LogLevelFlags.Warning))
                        Debug.LogWarning( level+ "\n"+message);
                    break;
                case Helpers.LogLevel.Error:
                    if (LogLevels.HasFlag(LogLevelFlags.Error))
                        Debug.LogError( level+ "\n"+message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
    }
}