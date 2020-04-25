# PuppyProxy

A simple C# web proxy with support for CONNECT requests.

## New in v1.1.1

- Fix for multi-platform

Refer to CHANGELOG.md for previous versions.

## Getting Started

PuppyProxy is targeted to both .NET Core and .NET Framework.  You **MUST** run PuppyProxy as an administrator.

### .NET Core
```
# dotnet PuppyProxy.dll
```

### .NET Framework
```
> PuppyProxy.exe
```

## Testing

You can set your proxy in ```Internet Options``` to point to PuppyProxy for both HTTP and HTTPS on the default IP address of ```127.0.0.1``` and port ```8000```.  

Alternatively, you can specify a proxy address while using cURL by using ```-x http://127.0.0.1:8000```.
