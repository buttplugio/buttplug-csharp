namespace Buttplug.Devices.Configuration
{
    public interface IProtocolConfiguration
    {
        // Overloading Equals is annoying, and this isn't a strict equals sorta deal, so we'll call
        // it our own thing.
        bool Matches(IProtocolConfiguration aConfig);
    }
}
