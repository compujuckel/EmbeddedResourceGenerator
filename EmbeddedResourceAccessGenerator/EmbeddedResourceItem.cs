namespace EmbeddedResourceAccessGenerator;

internal record EmbeddedResourceItem(string StrippedName, string ManifestResourceName)
{
	public string StrippedName { get; } = StrippedName;
	public string ManifestResourceName { get; } = ManifestResourceName;
}