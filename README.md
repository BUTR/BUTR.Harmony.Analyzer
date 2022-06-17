# BUTR.Harmony.Analyzer
<p align="center">
  <a href="https://github.com/BUTR/BUTR.Harmony.Analyzer" alt="Lines Of Code">
    <img src="https://tokei.rs/b1/github/BUTR/BUTR.Harmony.Analyzer?category=code" />
  </a>
  <a href="https://www.codefactor.io/repository/github/butr/butr.harmony.analyzer">
    <img src="https://www.codefactor.io/repository/github/butr/butr.harmony.analyzer/badge" alt="CodeFactor" />
  </a>
  <a href="https://codeclimate.com/github/BUTR/BUTR.Harmony.Analyzer/maintainability">
    <img alt="Code Climate maintainability" src="https://img.shields.io/codeclimate/maintainability-percentage/BUTR/BUTR.Harmony.Analyzer">
  </a>
  <!--
  <a href="https://butr.github.io/BUTR.Harmony.Analyzer" alt="Documentation">
    <img src="https://img.shields.io/badge/Documentation-%F0%9F%94%8D-blue?style=flat" />
  </a>
  -->
  </br>
  <a href="https://github.com/BUTR/BUTR.Harmony.Analyzer/actions/workflows/test.yml?query=branch%3Amaster">
    <img alt="GitHub Workflow Status (event)" src="https://img.shields.io/github/workflow/status/BUTR/BUTR.Harmony.Analyzer/Test?branch=master&label=Tests">
  </a>
  <a href="https://codecov.io/gh/BUTR/BUTR.Harmony.Analyzer">
    <img src="https://codecov.io/gh/BUTR/BUTR.Harmony.Analyzer/branch/master/graph/badge.svg" />
  </a>
  </br>
  <a href="https://www.nuget.org/packages/BUTR.Harmony.Analyzer" alt="NuGet BUTR.Harmony.Analyzer">
    <img src="https://img.shields.io/nuget/v/BUTR.Harmony.Analyzer.svg?label=NuGet%20BUTR.Harmony.Analyzer&colorB=blue" />
  </a>
</p>

A Roslyn analyzer for [`Harmony`](https://github.com/pardeike/Harmony) which adds the ability to do dynamic type checking and ensures that there are no typos when using the AccessTools* methods.

For example, if the user wants to access `_privateField` from some class/struct, but typed instead `_privateFld`, the analyzer will highlight that. It leverages the [`System.Reflection.Metadata`](https://www.nuget.org/packages/System.Reflection.Metadata/) package for fast analysys or public and non-public members of types, since Roslyn can't access non public members.

This drastically reduces runtime errors when using Harmony.

Also, when speaking in long-term maintenance of mods, if the game's internal API changes and a type member will be renamed or it will be changed to another type (e.g. field to a property), the analyzer will highlight that.

<p align="center">
  <img src="https://cdn.discordapp.com/attachments/422092475163869201/987282699347714078/unknown.png" width="800" />
</p>

```csharp
// The analyzer works only when full data is provided for the method in compile-time
// so the following methods will work:
AccessTools.Method(typeof(Class), "MemberName");
AccessTools.Method("System.Class:MemberName");

// But this won't be supported, because the information will be available only at runtime
Type type = ExternalMethod();
AccessTools.Method(type, "MemberName");
```


## Rules

You'll find the rules in the documentation: [the rules and their explanation](https://github.com/BUTR/BUTR.Harmony.Analyzer/tree/main/docs)

## Installation

- NuGet package (recommended): <https://www.nuget.org/packages/BUTR/BUTR.Harmony.Analyzer/>

## Supported API
Supports `AccessTools` classes from [`Harmony`](https://github.com/pardeike/Harmony), [`HarmonyX`](https://github.com/BepInEx/HarmonyX) and [`Harmony.Extensions`](https://github.com/BUTR/Harmony.Extensions).  
As long as the class starts with `AccessTools`, it will be supported.  
The following API's are supported:
* Constructor/DeclaredConstructor
* Field/DeclaredField
* Property/DeclaredProperty
* Method/DeclaredMethod
* PropertyGetter/DeclaredPropertyGetter
* PropertySetter/DeclaredPropertySetter
* Delegate/DeclaredDelegate
* StaticFieldRefAccess
* StructFieldRefAccess

## Additional Analyzers
We believe that static typed member check (via `typeof(Type)`) adds more problems than it solves, because we are bound to the public ABI of the library that is patched.  
Instead, we suggest to use dynamic typed member check (via a string containing the full name of the type).  
Common sense would suggest that this is a bad idea, since you can't check whether the member you want to get get exists, but the sole purpose of `BUTR.Harmony.Analyzer` is to solve this exact problem by creating warnings at compile time if the type was not found.

One of the most common problems that is solved is type namespace moving, since it breaks the public ABI.  
Usually, the modder won't notice that a type was moved if both old and new namespaces are referenced and the full name of the type is not used.  
The dynamic typed check requires the full name of the type, so a namespace change will create a warning that the type does not exists.  
There is a edge case that is not covered by the analyzer - moving a type within different assemblies with keeping the exact namespace.