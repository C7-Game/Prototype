# Testing the ini-parser NuGet package against unit ini files

$NugetPath = '/Users/jim/.nuget/packages'
$Civ3Root = '/Users/jim/civ3'
$IniPath = $Civ3Root + '/Art/Units/Samurai/Samurai.INI'

Add-Type -Path ($NugetPath + '/ini-parser/3.4.0/lib/net20/INIFileParser.dll')
<#

$parser = New-Object IniParser.FileIniDataParser
$data = $parser.ReadFile($IniPath)
$data.Sections["Animations"] | Select-Object KeyName, Value | Format-List
#>

Add-Type -Path ('bin/Debug/netstandard2.0/ReadCivData.ConvertCiv3Media.dll')

$foo = New-Object ReadCivData.ConvertCiv3Media.Civ3UnitSprite($IniPath)