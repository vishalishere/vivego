setlocal

@rem enter this directory
cd /d %~dp0

set TOOLS_PATH=..\..\..\lib\grpc.tools\1.6.1\tools\windows_x64

%TOOLS_PATH%\protoc.exe --csharp_out ./ ./Protos.proto --grpc_out ./ --plugin=protoc-gen-grpc=%TOOLS_PATH%\grpc_csharp_plugin.exe

endlocal
