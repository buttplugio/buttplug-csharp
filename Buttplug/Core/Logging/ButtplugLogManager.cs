using System;
using System.Collections.Generic;
using Buttplug.Core.Messages;
using Buttplug.Logging;
using JetBrains.Annotations;

namespace Buttplug.Core.Logging
{
    /// <inheritdoc cref="IButtplugLogManager"/>
    // ReSharper disable once InheritdocConsiderUsage
    public class ButtplugLogManager : IButtplugLogManager
    {
        /// <summary>
        /// List of listeners and their respective log levels. Requires since using normal
        /// EventHandlers would require extra logic on the event handler side.
        /// </summary>
        /// <remarks>
        /// This could probably be optimized as a dictionary but not really worth it unless this
        /// somehow becomes a performance issue.
        /// </remarks>
        private readonly List<(ButtplugLogLevel, Action<Log>)> _listeners = new List<(ButtplugLogLevel, Action<Log>)>();

        /// <summary>
        /// Records the highest level of message we want to receive. Optimization that allows us to
        /// not have to traverse the listener array on levels no one is listening for.
        /// </summary>
        public ButtplugLogLevel MaxLevel { get; private set; } = ButtplugLogLevel.Off;

        public void AddLogListener(ButtplugLogLevel aLevel, Action<Log> aListener)
        {
            RemoveLogListener(aListener);
            if (aLevel == ButtplugLogLevel.Off)
            {
                return;
            }

            _listeners.Add((aLevel, aListener));
            ResetMaxLevel();
        }

        public void RemoveLogListener(Action<Log> aListener)
        {
            var listener = _listeners.Find((x) => x.Item2 == aListener);
            if (listener.Item2 != null)
            {
                _listeners.Remove(listener);
                ResetMaxLevel();
            }
        }

        private void ResetMaxLevel()
        {
            foreach (var listener in _listeners)
            {
                if (listener.Item1 > MaxLevel)
                {
                    MaxLevel = listener.Item1;
                }
            }
        }

        private void LogMessageHandler([NotNull] object aObject, [NotNull] ButtplugLogMessageEventArgs aMsg)
        {
            ButtplugUtils.ArgumentNotNull(aObject, "aObject");
            ButtplugUtils.ArgumentNotNull(aMsg, "aMsg");

            // If no one is listening for this level of message, just bail.
            if (MaxLevel < aMsg.LogMessage.LogLevel)
            {
                return;
            }

            foreach (var listener in _listeners)
            {
                if (listener.Item1 >= aMsg.LogMessage.LogLevel)
                {
                    listener.Item2(aMsg.LogMessage);
                }
            }
        }

        /// <inheritdoc cref="IButtplugLogManager"/>
        public IButtplugLog GetLogger([NotNull] Type aType)
        {
            if (aType == null)
            {
                throw new ArgumentNullException(nameof(aType));
            }

            // Just pass the type in instead of traversing the stack to find it.
            var logger = new ButtplugLog(LogProvider.GetLogger(aType.Name));
            logger.LogMessageReceived += LogMessageHandler;
            return logger;
        }
    }
}
