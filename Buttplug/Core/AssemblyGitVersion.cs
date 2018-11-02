using System;

namespace Buttplug.Core
{
    /// <summary>
    /// An Assembly Attribute for storing extended Git versions strings (commit SHAs)
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    // ReSharper disable once InheritdocConsiderUsage
    public class AssemblyGitVersion : Attribute
    {
        /// <summary>
        /// Gets the Git Version (includes commit SHA)
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyGitVersion"/> class.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public AssemblyGitVersion()
        {
            Value = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyGitVersion"/> class.
        /// </summary>
        /// <param name="aValue">Git Version (includes commit SHA)</param>
        // ReSharper disable once UnusedMember.Global
        public AssemblyGitVersion(string aValue)
        {
            Value = aValue;
        }
    }
}
