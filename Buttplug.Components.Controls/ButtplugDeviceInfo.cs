namespace Buttplug.Components.Controls
{
    public class ButtplugDeviceInfo
    {
        private string Name { get; }

        public uint Index { get; }

        public string[] Messages { get; }

        public ButtplugDeviceInfo(uint aIndex, string aName, string[] aMessages)
        {
            Index = aIndex;
            Name = aName;
            Messages = aMessages;
        }

        public override string ToString()
        {
            return $"{Index}: {Name}";
        }
    }
}