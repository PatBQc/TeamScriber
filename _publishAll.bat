del .\Release\*.* /S /Q
del .\source\bin\Release\*.* /S /Q


REM .Net Version
dotnet publish .\source\TeamScriber.sln -c Release
powershell -command "Compress-Archive -Path .\source\bin\Release\net8.0\publish -DestinationPath .\Release\TeamScriber-v01.01-DotNet.zip"
del .\source\bin\Release\*.* /S /Q

REM Windows x64
dotnet publish .\source\TeamScriber.sln -c Release -r win-x64 --self-contained
powershell -command "Compress-Archive -Path .\source\bin\Release\net8.0\win-x64\publish\ -DestinationPath .\Release\TeamScriber-v01.01-x64-Self-Contained.zip"
del .\source\bin\Release\*.* /S /Q

REM Windows x86
dotnet publish .\source\TeamScriber.sln -c Release -r win-x86 --self-contained
powershell -command "Compress-Archive -Path .\source\bin\Release\net8.0\win-x86\publish\ -DestinationPath .\Release\TeamScriber-v01.01-x86-Self-Contained.zip"
del .\source\bin\Release\*.* /S /Q

REM Windows ARM 64 
dotnet publish .\source\TeamScriber.sln -c Release -r win-arm64 --self-contained
powershell -command "Compress-Archive -Path .\source\bin\Release\net8.0\win-arm64\publish\ -DestinationPath .\Release\TeamScriber-v01.01-ARM64-Self-Contained.zip"
del .\source\bin\Release\*.* /S /Q
