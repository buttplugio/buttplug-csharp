using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buttplug.Core
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class AssemblyGitVersion : Attribute
    {
        public string Value { get; private set; }

        public AssemblyGitVersion()
        {
            Value = string.Empty;
        }

        public AssemblyGitVersion(string value)
        {
            Value = value;
        }
    }
}
