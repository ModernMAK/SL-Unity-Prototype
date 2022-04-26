using System;
using OpenMetaverse;
using UnityEngine;

public class LoggerSetup : MonoBehaviour
{
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
            case Helpers.LogLevel.Debug:
                Debug.Log( level+ "\n"+message);
                break;
            case Helpers.LogLevel.Warning:
                Debug.LogWarning( level+ "\n"+message);
                break;
            case Helpers.LogLevel.Error:
                Debug.LogError( level+ "\n"+message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }
}