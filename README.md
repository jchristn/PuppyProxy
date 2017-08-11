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

Linux
$ sudo mono --aot=nrgctx-trampolines=8096,nimt-trampolines=8096,ntrampolines=4048 --server PuppyProxy.exe
$ sudo mono --server PuppyProxy.exe
```

## Running under Mono
PuppyProxy works well in Mono environments to the extent that we have tested it. It is recommended that when running under Mono, you execute the containing EXE using --server and after using the Mono Ahead-of-Time Compiler (AOT).

NOTE: Windows accepts '0.0.0.0' as an IP address representing any interface.  On Mac and Linux you must be specified ('127.0.0.1' is also acceptable, but '0.0.0.0' is NOT).

```
mono --aot=nrgctx-trampolines=8096,nimt-trampolines=8096,ntrampolines=4048 --server PuppyProxy.exe
mono --server PuppyProxy.exe
```