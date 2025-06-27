@echo off
REM Clean previous builds
 dotnet clean

REM Publish as single-file, self-contained, with all resources for Windows 64-bit
 dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true --self-contained true

REM Show output folder
explorer bin\Release\net9.0-windows\win-x64\publish

echo.
echo Build and publish complete. The single .exe is in the publish folder above.
pause 