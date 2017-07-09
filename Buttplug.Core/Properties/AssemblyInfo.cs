using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ButtplugShared")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("ButtplugShared")]
[assembly: AssemblyCopyright("Copyright Metafetish 2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("7d7fd21b-87db-4cb5-a1ca-53b288c835fd")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("0.1.1.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// Let test project see internals
[assembly: InternalsVisibleTo("Buttplug")]
[assembly: InternalsVisibleTo("ButtplugTest")]
[assembly: InternalsVisibleTo("ButtplugUWPBluetoothManager")]
[assembly: InternalsVisibleTo("ButtplugXInputGamepadManager")]
[assembly: InternalsVisibleTo("ButtplugUWPGamepadManager")]
[assembly: InternalsVisibleTo("ButtplugClient")]
