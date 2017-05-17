using Windows.Storage.Streams;

namespace ButtplugUWPBluetoothManager.Core
{
    class ButtplugBluetoothUtils
    {
        public static IBuffer WriteString(string s)
        {
            var w = new DataWriter();
            w.WriteString(s);
            return w.DetachBuffer();
        }

        public static IBuffer WriteByteArray(byte[] b)
        {
            var w = new DataWriter();
            w.WriteBytes(b);
            return w.DetachBuffer();
        }

    }
}
