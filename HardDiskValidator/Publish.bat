msbuild /p:TargetFramework=net20 /p:Configuration=Release
msbuild /p:TargetFramework=net40 /p:Configuration=Release
dotnet publish HardDiskValidator.csproj -r win7-x64 --framework netcoreapp3.1 -c Release --self-contained /p:PublishSingleFile=true