# PuppyProxy

A simple C# web proxy with support for CONNECT requests.

## New in v1.0.0
- Initial release

## Version History
Notes from previous versions are shown below (summarized to minor build)

v1.0.0
- Initial release

## Getting Started
Compile from source and use Mono's AOT (see below) if running in Linux or Mac.  Running without specifying a configuration file will cause PuppyProxy to begin listening on port 8000, supporting HTTP proxy including CONNECT.  Use ```--cfg=<filename.txt>``` to specify a configuration file.  Use ```--display-cfg``` to show the configuration (helpful if you want a template by which to create your own).

NOTE: You may need to run as administrator if listening on a port under 1024.
```
Windows
C:\> PuppyProxy.exe
C:\> PuppyProxy.exe --cfg=filename.txt
```

## Running under Mono
Mono is missing methods on both Linux (TcpClient.Dispose) and Mac (System.Net.NetworkInformation.UnixIPGlobalProperties.GetActiveTcpConnections) that prevent operation of PuppyProxy.
