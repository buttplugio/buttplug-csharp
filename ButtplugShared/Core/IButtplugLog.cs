using Buttplug.Messages;
using JetBrains.Annotations;
using static Buttplug.Messages.Error;

namespace Buttplug.Core
{
    public interface IButtplugLog
    {
        void Trace(string aMsg, bool aLocalOnly = false);

        void Debug(string aMsg, bool aLocalOnly = false);

        void Info(string aMsg, bool aLocalOnly = false);

        void Warn(string aMsg, bool aLocalOnly = false);

        void Error(string aMsg, bool aLocalOnly = false);

        // Fatal is kept here for completeness, even if it is not yet used.
        // ReSharper disable once UnusedMember.Global
        void Fatal(string aMsg, bool aLocalOnly = false);

        [NotNull]
        Error LogErrorMsg(uint aId, ErrorClass aCode, string aMsg);

        [NotNull]
        Error LogWarnMsg(uint aId, ErrorClass aCode, string aMsg);
    }
}
