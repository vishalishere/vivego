..\..\..\lib\Grpc.Tools.1.4.1\tools\windows_x64\protoc.exe -I=. -I=..\..\models\proto.actor\ --csharp_out=. --csharp_opt=file_extension=.g.cs --grpc_out=. --plugin=protoc-gen-grpc=..\..\..\lib\Grpc.Tools.1.4.1\tools\windows_x64\grpc_csharp_plugin.exe Broker.proto