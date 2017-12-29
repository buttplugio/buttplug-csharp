using Buttplug.Core;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    [TestFixture]
    public class AssemblyGitVersionTests
    {
        [Test]
        public void AssemblyGitVersionTest1()
        {
            var gv = new AssemblyGitVersion();
            Assert.AreEqual(string.Empty, gv.Value);
        }

        [Test]
        public void AssemblyGitVersionTest2()
        {
            var gv = new AssemblyGitVersion("foo");
            Assert.AreEqual("foo", gv.Value);
        }
    }
}
