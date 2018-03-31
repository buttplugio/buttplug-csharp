using System;

namespace Buttplug.Core
{
    /// <summary>
    /// An Assembly Attribute for storing extended Git versions strings (commit SHAs)
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class AssemblyGitVersion : Attribute
    {
        /// <summary>
        /// Git Version (includes commit SHA)
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyGitVersion"/> class.
        /// </summary>
        public AssemblyGitVersion()
        {
            Value = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyGitVersion"/> class.
        /// </summary>
        /// <param name="value">Git Version (includes commit SHA)</param>
        public AssemblyGitVersion(string value)
        {
            Value = value;
        }
    }
}
