namespace EmbeddedResourceAccessGenerator.Example;

internal class Program
{
	private static void Main(string[] args)
	{
		Program.Direct();
		Program.ViaClass();
		Program.ForEachRootResource();
	}

	private static void Direct()
	{
		using Stream s = EmbeddedResources.TestAsset.Test.Txt.GetStream();
		using StreamReader sr = new StreamReader(s);
		Console.WriteLine(sr.ReadToEnd());
	}

	private static void ViaClass()
	{
		using Stream s = EmbeddedResources.TestAsset.Test2.Txt.GetStream();
		using StreamReader sr = new StreamReader(s);
		Console.WriteLine(sr.ReadToEnd());

		using Stream s2 = EmbeddedResources.RootTest.Txt.GetStream();
		using StreamReader sr2 = new StreamReader(s2);
		Console.WriteLine(sr2.ReadToEnd());
	}

	private static void ForEachRootResource()
	{
		// Access root-level resources directly
		using Stream s = EmbeddedResources.RootTest.Txt.GetStream();
		using StreamReader sr = new StreamReader(s);
		Console.WriteLine(sr.ReadToEnd());
	}
}
