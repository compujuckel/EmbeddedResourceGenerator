namespace EmbeddedResourceAccessGenerator.Tests.Tests;

using Xunit;

/// <summary>
/// The main tests. This file is deliberately not in the root folder to test that the generator works no
/// files in the csproj folder.
/// </summary>
public class EmbeddedResourceAccessGeneratorTests
{
	[Fact]
	public void TestTxtIsAccessible()
	{
		using var reader = EmbeddedResources.TestAssets.Test.txt.GetReader();
		Assert.Equal("Success", reader.ReadToEnd());

		using var reader2 = EmbeddedResources.TestAssets.Test.txt.GetStream();
		using var reader3 = new System.IO.StreamReader(reader2);
		Assert.Equal("Success", reader3.ReadToEnd());
	}

	[Fact]
	public void TestWithSpacesTxtIsAccessible()
	{
		using var reader = EmbeddedResources.TestAssets.Test_With_Spaces.txt.GetReader();
		Assert.Equal("Success", reader.ReadToEnd());

		using var reader2 = EmbeddedResources.TestAssets.Test_With_Spaces.txt.GetStream();
		using var reader3 = new System.IO.StreamReader(reader2);
		Assert.Equal("Success", reader3.ReadToEnd());
	}

	[Fact]
	public void SubfolderTestWithSpacesTxtIsAccessible()
	{
		using var reader = EmbeddedResources.TestAssets.Subfolder_With_Spaces.Test_With_Spaces.txt.GetReader();
		Assert.Equal("Success", reader.ReadToEnd());

		using var reader2 = EmbeddedResources.TestAssets.Subfolder_With_Spaces.Test_With_Spaces.txt.GetStream();
		using var reader3 = new System.IO.StreamReader(reader2);
		Assert.Equal("Success", reader3.ReadToEnd());
	}

	[Fact]
	public void RootTestTxtIsAccessible()
	{
		using var reader = EmbeddedResources.Test.txt.GetReader();
		Assert.Equal("Success", reader.ReadToEnd());

		using var reader2 = EmbeddedResources.Test.txt.GetStream();
		using var reader3 = new System.IO.StreamReader(reader2);
		Assert.Equal("Success", reader3.ReadToEnd());
	}

	[Fact]
	public void SubfolderTestTxtIsAccessible()
	{
		using var reader = EmbeddedResources.TestAssets.Subfolder.Test.txt.GetReader();
		Assert.Equal("Success", reader.ReadToEnd());

		using var reader2 = EmbeddedResources.TestAssets.Subfolder.Test.txt.GetStream();
		using var reader3 = new System.IO.StreamReader(reader2);
		Assert.Equal("Success", reader3.ReadToEnd());
	}

}
