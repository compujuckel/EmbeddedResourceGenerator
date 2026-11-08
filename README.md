# EmbeddedResourceAccessGenerator
[![NuGet version (compujuckel.EmbeddedResourceAccessGenerator)](https://img.shields.io/nuget/v/compujuckel.EmbeddedResourceAccessGenerator.svg?style=flat-square)](https://www.nuget.org/packages/compujuckel.EmbeddedResourceAccessGenerator/)


The EmbeddedResourceAccessGenerator is a code generator to allow easy access to all
embedded resources.

## Usage
Get the nuget package [here](https://www.nuget.org/packages/compujuckel.EmbeddedResourceAccessGenerator).

After referencing the `compujuckel.EmbeddedResourceAccessGenerator` nuget the code generation will
automatically create a class `EmbeddedResources` in the root namespace of the project.

The generator creates a nested class hierarchy mirroring the directory structure and file names
of embedded resources. Dots in file names create additional nested classes.

E.g. for a `Test.txt` embedded resource in the `TestAsset` folder:

```csharp
	// Access via the generated class hierarchy
	using Stream s = EmbeddedResources.TestAsset.Test.txt.GetStream();
	using StreamReader sr = EmbeddedResources.TestAsset.Test.txt.GetReader();
```

For a root-level resource `RootTest.txt`:

```csharp
	using Stream s = EmbeddedResources.RootTest.txt.GetStream();
	using StreamReader sr = EmbeddedResources.RootTest.txt.GetReader();
```

## Configuration

### EmbeddedResourcePrefix

To shorten the generated class hierarchy, you can configure a prefix to be stripped from
resource names. Set the `EmbeddedResourcePrefix` MSBuild property in your `.csproj` file:

```xml
<EmbeddedResourcePrefix>Embedded</EmbeddedResourcePrefix>
```

For example, if your resources live under `Embedded\Resources\`, the manifest resource name
`MyProject.Embedded.Resources.Test.txt` would normally generate the class hierarchy
`EmbeddedResources.Embedded.Resources.Test.txt`. With the prefix set to `Embedded`, it becomes
`EmbeddedResources.Resources.Test.txt` — the `Embedded` segment is removed from the class
hierarchy. The manifest resource name used internally remains unchanged.

## Motivation
Instead of using magic strings in the resource access code that may point to non-existant
resources this generator guarantees resources to exist and code to not compile when they are
removed.

The nested class hierarchy provides intuitive, discoverable access to embedded resources
with IDE auto-completion at each level.

Also it saves quite a bit of typing effort.

## About this Fork

This is an AI-assisted fork of the original [`Chorn.EmbeddedResourceAccessGenerator`](https://github.com/ChristophHornung/EmbeddedResourceGenerator). Key
changes compared to the original version:

* **ManifestResourceName-only**: The generator now relies exclusively on the
  `ManifestResourceName` metadata, removing the fallback path-based name generation and
  related semicolon-handling workarounds.
* **Configurable prefix stripping**: The new `EmbeddedResourcePrefix` property allows
  removing a configurable prefix segment from the generated class hierarchy (see above).
* **Simplified codebase**: Removed unused helper code and tests for edge cases that are no
  longer applicable with the ManifestResourceName-only approach.

