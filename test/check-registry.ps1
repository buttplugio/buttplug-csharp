# Check registry values after install to make sure things actually get set correctly
if (!(Get-ItemProperty -Path "HKLM:\SOFTWARE\Classes\AppID\{415579bd-5399-48ef-8521-775ebcd647af}").AccessPermission)
{
    return $false;
}
if ((Get-ItemProperty -Path HKLM:\SOFTWARE\Classes\AppID\Buttplug.exe).AppID.CompareTo("{415579bd-5399-48ef-8521-775ebcd647af}"))
{
    return $false;
}
if ((Get-ItemProperty -Path HKLM:\SOFTWARE\Classes\AppID\ButtplugGUI.exe).AppID.CompareTo("{415579bd-5399-48ef-8521-775ebcd647af}"))
{
    return $false;
}
if ((Get-ItemProperty -Path HKLM:\SOFTWARE\Classes\AppID\ButtplugCLI.exe).AppID.CompareTo("{415579bd-5399-48ef-8521-775ebcd647af}"))
{
    return $false;
}

return $true;
