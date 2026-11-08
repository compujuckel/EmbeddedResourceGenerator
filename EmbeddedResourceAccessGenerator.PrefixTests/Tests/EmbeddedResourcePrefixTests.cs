namespace EmbeddedResourceAccessGenerator.PrefixTests.Tests;

using Xunit;

/// <summary>
/// Tests that the EmbeddedResourcePrefix property correctly strips the configured prefix
/// from the generated class hierarchy. With prefix "Embedded", resources under
/// "Embedded\Resources" are accessible as EmbeddedResources.Resources.* instead of
/// EmbeddedResources.Embedded.Resources.*.
/// </summary>
public class EmbeddedResourcePrefixTests
{
	[Fact]
	public void PrefixStripped_ResourceIsAccessible()
	{
		// With EmbeddedResourcePrefix="Embedded", the manifest resource name
		// "EmbeddedResourceAccessGenerator.PrefixTests.Embedded.Resources.Test.txt"
		// becomes "Resources.Test.txt" after stripping root namespace and prefix.
		// So it's accessible as EmbeddedResources.Resources.Test.txt
		using var reader = EmbeddedResources.Resources.Test.Txt.GetReader();
		Assert.Equal("PrefixTestResource", reader.ReadToEnd().Trim());
		
		using var stream = EmbeddedResources.Resources.Test.Txt.GetStream();
		using var reader2 = new System.IO.StreamReader(stream);
		Assert.Equal("PrefixTestResource", reader2.ReadToEnd().Trim());
	}

	[Fact]
	public void PrefixStripped_NestedResourceIsAccessible()
	{
		// Nested resource under Subfolder should also be accessible with the prefix stripped.
		// Manifest: "EmbeddedResourceAccessGenerator.PrefixTests.Embedded.Resources.Subfolder.Nested.txt"
		// After stripping: "Resources.Subfolder.Nested.txt"
		// Accessible as: EmbeddedResources.Resources.Subfolder.Nested.txt
		using var reader = EmbeddedResources.Resources.Subfolder.Nested.Txt.GetReader();
		Assert.Equal("NestedResource", reader.ReadToEnd().Trim());

		using var stream = EmbeddedResources.Resources.Subfolder.Nested.Txt.GetStream();
		using var reader2 = new System.IO.StreamReader(stream);
		Assert.Equal("NestedResource", reader2.ReadToEnd().Trim());
	}

	[Fact]
	public void PrefixStripped_ManifestResourceNameIsCorrect()
	{
		// Verify that the manifest resource name used in GetManifestResourceStream
		// still includes the full path (prefix stripping only affects the class hierarchy).
		var assembly = typeof(EmbeddedResources).Assembly;
		var names = assembly.GetManifestResourceNames();

		// The manifest resource name should still contain "Embedded" — only the class
		// hierarchy is affected by the prefix.
		Assert.Contains(names, n => n.Contains("Embedded.Resources.Test.txt"));
		Assert.Contains(names, n => n.Contains("Embedded.Resources.Subfolder.Nested.txt"));
	}
}
