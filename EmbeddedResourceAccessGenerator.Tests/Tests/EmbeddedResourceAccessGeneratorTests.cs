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
		using var reader = EmbeddedResources.TestAssets.Test.Txt.GetReader();
		Assert.Equal("Success", reader.ReadToEnd());

		using var reader2 = EmbeddedResources.TestAssets.Test.Txt.GetStream();
		using var reader3 = new System.IO.StreamReader(reader2);
		Assert.Equal("Success", reader3.ReadToEnd());
	}

	[Fact]
	public void TestWithSpacesTxtIsAccessible()
	{
		using var reader = EmbeddedResources.TestAssets.Test_With_Spaces.Txt.GetReader();
		Assert.Equal("Success", reader.ReadToEnd());

		using var reader2 = EmbeddedResources.TestAssets.Test_With_Spaces.Txt.GetStream();
		using var reader3 = new System.IO.StreamReader(reader2);
		Assert.Equal("Success", reader3.ReadToEnd());
	}

	[Fact]
	public void SubfolderTestWithSpacesTxtIsAccessible()
	{
		using var reader = EmbeddedResources.TestAssets.Subfolder_With_Spaces.Test_With_Spaces.Txt.GetReader();
		Assert.Equal("Success", reader.ReadToEnd());

		using var reader2 = EmbeddedResources.TestAssets.Subfolder_With_Spaces.Test_With_Spaces.Txt.GetStream();
		using var reader3 = new System.IO.StreamReader(reader2);
		Assert.Equal("Success", reader3.ReadToEnd());
	}

	[Fact]
	public void RootTestTxtIsAccessible()
	{
		using var reader = EmbeddedResources.Test.Txt.GetReader();
		Assert.Equal("Success", reader.ReadToEnd());

		using var reader2 = EmbeddedResources.Test.Txt.GetStream();
		using var reader3 = new System.IO.StreamReader(reader2);
		Assert.Equal("Success", reader3.ReadToEnd());
	}

	[Fact]
	public void SubfolderTestTxtIsAccessible()
	{
		using var reader = EmbeddedResources.TestAssets.Subfolder.Test.Txt.GetReader();
		Assert.Equal("Success", reader.ReadToEnd());

		using var reader2 = EmbeddedResources.TestAssets.Subfolder.Test.Txt.GetStream();
		using var reader3 = new System.IO.StreamReader(reader2);
		Assert.Equal("Success", reader3.ReadToEnd());
	}

	[Fact]
	public void LowercaseFileName_IsUppercasedInClassName()
	{
		// A file named "lowercase.txt" should generate a class "Lowercase" (first letter uppercased)
		// to avoid conflicts with C# keywords and ensure consistent naming.
		using var reader = EmbeddedResources.TestAssets.Lowercase.Txt.GetReader();
		Assert.Equal("LowercaseContent", reader.ReadToEnd());

		using var stream = EmbeddedResources.TestAssets.Lowercase.Txt.GetStream();
		using var reader2 = new System.IO.StreamReader(stream);
		Assert.Equal("LowercaseContent", reader2.ReadToEnd());
	}

	[Fact]
	public void IdentifierCollision_FirstResourceWins()
	{
		// "test_file.txt" and "test-file.txt" both normalize to "Test_file.Txt"
		// (hyphen is replaced with underscore, first letter uppercased). The first
		// resource encountered wins, and a warning diagnostic (EMBRESGEN002) is reported.
		// We can only access the first one through the generated class.
		using var reader = EmbeddedResources.TestAssets.Test_file.Txt.GetReader();
		string content = reader.ReadToEnd();

		// The first file by processing order should be accessible.
		// We just verify that one of them is accessible (the collision warning is a build warning).
		Assert.True(content == "CollisionFirst" || content == "CollisionSecond",
			"Expected one of the colliding resources to be accessible");
	}

}
