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

A Roslyn analyzer for [`Harmony`](https://github.com/pardeike/Harmony) which ensures that there are no typos when using the AccessTools* methods.  

For example, if the user wants to access `_privateField` from some class/struct, but typed instead `_privateFld`, the analyzer will highlight that. It leverages the [`System.Reflection.Metadata`](https://www.nuget.org/packages/System.Reflection.Metadata/) package for fast analysys or public and non-public members of types.  

This drastically reduces runtime errors when using Harmony.  

Also, when speaking in long-term maintenance of mods, if the game's internal API changes and a type member will be renamed or it will be changed to another type (e.g. field to a property), the analyzer will highlight that.  

<p align="center">
  <img src="https://media.discordapp.net/attachments/422092475163869201/916767149815631902/unknown.png" width="800" />
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

## Supported API
Supports `AccessTools` classes from [`Harmony`](https://github.com/pardeike/Harmony), [`HarmonyX`](https://github.com/BepInEx/HarmonyX) and [`Harmony.Extensions`](https://github.com/BUTR/Harmony.Extensions).  
As long as the class starts with `AccessTools`, it will be supported.  
The following API's are supported:  
* Field/DeclaredField
* Property/DeclaredProperty
* Method/DeclaredMethod
* PropertyGetter/DeclaredPropertyGetter
* PropertySetter/DeclaredPropertySetter
* StaticFieldRefAccess
* StructFieldRefAccess
