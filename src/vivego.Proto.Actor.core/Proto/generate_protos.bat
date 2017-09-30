setlocal

@rem enter this directory
cd /d %~dp0

set TOOLS_PATH=..\..\..\lib\google.protobuf.tools\3.4.0\tools\windows_x64

%TOOLS_PATH%\protoc.exe -I=. --csharp_out=. .\Protos.proto

endlocal
