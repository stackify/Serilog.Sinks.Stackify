# Serilog.Sinks.Stackify

A Serilog sink that writes events to stackify. [Stackify](http://www.stackify.com) is a cloud hosted solution to capture log messages. Register for an account at their website and use the provided GUID in the configuration for Serilog.

**Package**

[Serilog.Sinks.Stackify V2](https://www.nuget.org/packages/Serilog.Sinks.Stackify/2.0.2)
| **Platforms** - .NET 4.5 / .NET Standard 1.3

[Serilog.Sinks.Stackify V3](https://www.nuget.org/packages/serilog.sinks.Stackify)
| **Platforms** - .NET 4.6.2 / .NET Standard 2.0

```csharp
var log = new LoggerConfiguration()
    .WriteTo.Stackify()
    .CreateLogger();
```

The sink captures all levels, but respect the minimum level configured on LoggerConfiguration. Serilog properties are converted to stackify's jsondata property.

Refer to Stackify's documentation [Here](https://github.com/stackify/stackify-api-dotnet/) for configuration.
