# MauiBrowser.Sdk

MSBuild SDK that composes Blazor WebAssembly defaults with MAUI Browser runtime conventions.

## Default Behavior

- imports `Microsoft.NET.Sdk.BlazorWebAssembly` props/targets
- validates `TargetFramework` is `net10.0` by default
- adds an implicit runtime reference:
  - package reference to `MauiBrowser.Runtime` by default, or
  - project reference when `MauiBrowserRuntimeProjectPath` is supplied

## Usage
```xml
<Project Sdk="MauiBrowser.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

## Configuration Knobs

- `MauiBrowserRuntimePackageId` (default: `MauiBrowser.Runtime`)
- `MauiBrowserRuntimeVersion` (default: `$(MauiBrowserSdkVersion)`)
- `MauiBrowserEnableImplicitRuntimeReference` (default: `true`)
- `MauiBrowserRuntimeProjectPath` (default: empty)
- `MauiBrowserValidateTargetFramework` (default: `true`)
- `MauiBrowserRequiredTargetFramework` (default: `net10.0`)

When `MauiBrowserRuntimeProjectPath` is set, the SDK adds a `ProjectReference` and skips implicit package reference injection.
