# Cirreum.Cors

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Cors.svg?style=flat-square)](https://www.nuget.org/packages/Cirreum.Cors/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Cors.svg?style=flat-square)](https://www.nuget.org/packages/Cirreum.Cors/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Cors?style=flat-square)](https://github.com/cirreum/Cirreum.Cors/releases)


A flexible and secure CORS (Cross-Origin Resource Sharing) configuration library for ASP.NET Core applications. This library provides a configuration-driven approach to managing CORS policies with built-in security validations and automatic header management.

## Features

- **Configuration-driven**: Define CORS policies in `appsettings.json`
- **Multiple named policies**: Support for multiple CORS policies in a single application
- **Security validations**: Automatic HTTPS enforcement for non-wildcard origins
- **Wildcard subdomain support**: Built-in support for subdomain wildcards (e.g., `https://*.example.com`)
- **Automatic header injection**: Automatically adds required headers like `X-Cirreum-App-Name`
- **Flexible policy selection**: Uses default policy or first configured policy automatically

## Installation

```bash
# Package installation command will depend on your distribution method
# If published to NuGet:
dotnet add package Cirreum.Cors
```

## Quick Start

### 1. Configure CORS in appsettings.json

```json
{
  "Cirreum": {
    "Cors": {
      "default": {
        "origins": ["https://*.mycompany.com", "https://app.mycompany.com"],
        "methods": ["GET", "POST", "PUT", "DELETE"],
        "headers": ["Authorization", "Content-Type"],
        "exposedHeaders": ["Content-Disposition"]
      }
    }
  }
}
```

### 2. Add CORS to your application

```csharp
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configure CORS from appsettings.json
builder.ConfigureCors();

var app = builder.Build();

// Use the configured CORS policy
app.UseConfiguredCors();

app.Run();
```

## Configuration

### Basic Configuration Structure

The CORS configuration is defined under the `Cirreum:Cors` section in your `appsettings.json`. Each key represents a named policy:

```json
{
  "Cirreum": {
    "Cors": {
      "policy-name": {
        "origins": ["array of allowed origins"],
        "methods": ["array of allowed HTTP methods"],
        "headers": ["array of allowed headers"],
        "exposedHeaders": ["array of headers to expose to client"]
      }
    }
  }
}
```

### Configuration Properties

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `origins` | `string[]` | List of allowed origins. Use `["*"]` for any origin, `https://*.domain.com` for subdomain wildcards | `["https://app.example.com", "https://*.api.example.com"]` |
| `methods` | `string[]` | Allowed HTTP methods (case-sensitive, uppercase) | `["GET", "POST", "PUT", "DELETE"]` |
| `headers` | `string[]` | Request headers that clients can send | `["Authorization", "Content-Type"]` |
| `exposedHeaders` | `string[]` | Response headers exposed to client-side code | `["Content-Disposition", "X-Total-Count"]` |

### Multiple Policies Example

```json
{
  "Cirreum": {
    "Cors": {
      "default": {
        "origins": ["https://*.mycompany.com"],
        "methods": ["GET", "POST", "PUT", "DELETE"],
        "headers": ["Authorization", "Content-Type"],
        "exposedHeaders": ["Content-Disposition"]
      },
      "public-api": {
        "origins": ["*"],
        "methods": ["GET"],
        "headers": ["Authorization"],
        "exposedHeaders": []
      },
      "admin-panel": {
        "origins": ["https://admin.mycompany.com"],
        "methods": ["GET", "POST", "PUT", "DELETE", "PATCH"],
        "headers": ["Authorization", "Content-Type", "X-Admin-Token"],
        "exposedHeaders": ["X-Rate-Limit-Remaining"]
      }
    }
  }
}
```

## Usage Patterns

### Using Specific Named Policies

While `UseConfiguredCors()` automatically selects the default or first policy, you can also use specific policies:

```csharp
// Use a specific named policy
app.UseCors("public-api");
```

### Accessing Pre-defined Policy Names

The library provides constants for common policy names:

```csharp
using Cirreum.Cors;

// Use pre-defined policy name constants
var defaultPolicy = Policies.DefaultPolicyName; // "Default"
var allowAllPolicy = Policies.AllowAllPolicyName; // "AllowAll"
// ... other predefined policy names
```

## Security Features

### HTTPS Enforcement

The library automatically enforces HTTPS for all non-wildcard origins:

```json
// ✅ Valid configurations
{
  "origins": ["*"] // Wildcard allowed
}

{
  "origins": ["https://api.example.com"] // HTTPS required
}

// ❌ Invalid - will throw exception
{
  "origins": ["http://api.example.com"] // HTTP not allowed for specific origins
}
```

### Wildcard Origin Handling

When `"*"` is present in the origins array, all other origins are ignored:

```json
{
  "origins": ["*", "https://example.com", "https://api.example.com"]
  // Only "*" will be used, others are ignored with a warning
}
```

### Automatic Header Injection

The library automatically adds the `X-Cirreum-App-Name` header to your CORS configuration to support Application identification.

## Advanced Usage

### Custom Policy Selection

The library uses this policy selection logic:

1. If a "default" policy exists, it's used as the default policy
2. If no "default" policy exists, the first configured policy is used
3. You can override this by explicitly calling `UseCors("policy-name")`

### Wildcard Subdomain Support

The library supports subdomain wildcards:

```json
{
  "origins": ["https://*.api.example.com"]
  // Allows: https://v1.api.example.com, https://v2.api.example.com, etc.
}
```

## Requirements

- **.NET Core**: Compatible with modern .NET applications
- **ASP.NET Core**: Built for ASP.NET Core applications
- **Configuration section**: Requires `Cirreum:Cors` section in configuration

## Error Handling

The library provides clear error messages for common configuration issues:

- **Missing configuration**: Throws `InvalidOperationException` if `Cirreum:Cors` section is missing
- **No default policy**: Throws `InvalidOperationException` if no "Default" policy is configured
- **HTTP origins**: Throws `ArgumentException` for HTTP origins when not using wildcards

## Contributing

This package is part of the Cirreum ecosystem. Follow the established patterns when contributing new features or provider implementations.

---

For more information about CORS in ASP.NET Core, see the [official Microsoft documentation](https://docs.microsoft.com/en-us/aspnet/core/security/cors).