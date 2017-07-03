using System;
using System.Threading;
using ButtplugServerGUI;
using Xunit;

namespace ButtplugServerGUITest
{
    public class GUITest
    {
        private Exception didStart;
        private void StartGUI()
        {
            try
            {
                var m = new MainWindow();
                m.Close();
            }
            catch (Exception e)
            {
                didStart = e;
            }
        }

        [Fact]
        public void TestGUIBringup()
        {           
            var t = new Thread(StartGUI);
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (didStart is null)
            {
                Assert.True(true, "GUI Came up without exception");
            }
            else
            {
                Assert.True(false, $"{didStart.Message}\n{didStart.StackTrace}");
            }
        }
    }
}
