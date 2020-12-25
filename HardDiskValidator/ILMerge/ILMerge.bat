IF ["%programfiles(x86)%"]==[""] SET ilmergePath="%programfiles%\Microsoft\ILMerge"
IF NOT ["%programfiles(x86)%"]==[""] SET ilmergePath="%programfiles(x86)%\Microsoft\ILMerge"

set TargetFramework=%1
if %TargetFramework%==net20 set TargetPlaform=2.0
if %TargetFramework%==net40 set TargetPlaform=4.0
set binaryPath=%CD%\..\bin\Release\%TargetFramework%
set outputPath=%CD%\..\bin\ILMerge\%TargetFramework%
IF NOT EXIST "%outputPath%" MKDIR "%outputPath%"
%ilmergePath%\ilmerge /targetplatform=%TargetPlaform% /ndebug /target:winexe /out:"%outputPath%\HardDiskValidator.exe" "%binaryPath%\HardDiskValidator.exe" "%binaryPath%\DiskAccessLibrary.dll" "%binaryPath%\DiskAccessLibrary.Win32.dll" "%binaryPath%\DiskAccessLibrary.FileSystems.Abstractions.dll"