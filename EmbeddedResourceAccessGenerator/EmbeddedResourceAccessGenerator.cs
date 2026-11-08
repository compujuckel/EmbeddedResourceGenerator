namespace EmbeddedResourceAccessGenerator;

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// The generator for the embedded resource access.
/// </summary>
[Generator]
public class EmbeddedResourceAccessGenerator : IIncrementalGenerator
{
	private static readonly DiagnosticDescriptor generationWarning = new(
		id: "EMBRESGEN001",
		title: "Exception on generation",
		messageFormat: "Exception '{0}' {1}",
		category: "MessageExtensionGenerator",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor resourceCollisionWarning = new(
		id: "EMBRESGEN002",
		title: "Resource name collision",
		messageFormat: "Multiple resources map to the same identifier '{0}'. Only the first resource will be accessible. Affected resources: '{1}' and '{2}'.",
		category: "MessageExtensionGenerator",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	/// <inheritdoc />
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		//Debugger.Launch();

		// The analyzer config options provider — we need it to read per-file metadata.
		IncrementalValueProvider<AnalyzerConfigOptionsProvider> analyzerConfigProvider =
			context.AnalyzerConfigOptionsProvider;

		// We need a value provider for any additional file.
		// All embedded resources are added as additional files through our build props integrated into the nuget.
		IncrementalValueProvider<ImmutableArray<AdditionalText>> additionalFilesProvider =
			context.AdditionalTextsProvider.Select((t, _) => t).Collect();

		// The root namespace value provider. Can this ever be null? So far I have not seen it.
		IncrementalValueProvider<string?> rootNamespaceProvider = context.AnalyzerConfigOptionsProvider.Select((x, _) =>
			x.GlobalOptions.TryGetValue("build_property.RootNamespace", out string? rootNamespace)
				? rootNamespace
				: null);

		// We combine the providers to generate the parameters for our source generation.
		IncrementalValueProvider<(
			ImmutableArray<AdditionalText> files,
			AnalyzerConfigOptionsProvider AnalyzerConfig,
			string? rootNamespace)> combined = additionalFilesProvider
			.Combine(analyzerConfigProvider)
			.Combine(rootNamespaceProvider)
			.Select((c, _) => (c.Left.Left, c.Left.Right, c.Right));

		context.RegisterSourceOutput(combined, this.GenerateSourceIncremental);
	}

	private void GenerateSourceIncremental(SourceProductionContext context,
		(ImmutableArray<AdditionalText> files,
		 AnalyzerConfigOptionsProvider AnalyzerConfig,
		 string? rootNamespace) arg)
	{
		try
		{
			this.GenerateSource(context, arg.files, arg.AnalyzerConfig, arg.rootNamespace);
		}
		catch (Exception e)
		{
			// We generate a diagnostic message on all internal failures.
			context.ReportDiagnostic(Diagnostic.Create(EmbeddedResourceAccessGenerator.generationWarning, Location.None,
				e.Message, e.StackTrace));
		}
	}

	/// <summary>
	/// Reads the manifest resource name for an embedded resource from MSBuild metadata.
	/// Assumes ManifestResourceName is always available.
	/// </summary>
	private static string GetManifestResourceName(AdditionalText additionalText, AnalyzerConfigOptionsProvider analyzerConfig)
	{
		AnalyzerConfigOptions options = analyzerConfig.GetOptions(additionalText);

		if (options.TryGetValue("build_metadata.EmbeddedResource.ManifestResourceName", out string? manifestName)
		    && !string.IsNullOrEmpty(manifestName))
		{
			return manifestName;
		}

		return string.Empty;
	}

	/// <summary>
	/// Reads the optional prefix to strip from resource names. Configured via
	/// build_property.EmbeddedResourcePrefix in .editorconfig.
	/// </summary>
	private static string? GetResourcePrefix(AnalyzerConfigOptionsProvider analyzerConfig)
	{
		if (analyzerConfig.GlobalOptions.TryGetValue("build_property.EmbeddedResourcePrefix", out string? prefix)
		    && !string.IsNullOrEmpty(prefix))
		{
			return prefix;
		}

		return null;
	}

	/// <summary>
	/// Strips the root namespace and optional configured prefix from a manifest resource name
	/// to produce the relative name used for the class hierarchy.
	/// E.g. if rootNamespace is "MyProject", prefix is "Embedded", and name is "MyProject.Embedded.Test.test.txt",
	/// the result is "Test.test.txt".
	/// </summary>
	private static string GetRelativeResourceName(string manifestResourceName, string? rootNamespace, string? prefix)
	{
		// First, strip the root namespace prefix.
		string relative = manifestResourceName;
		if (!string.IsNullOrEmpty(rootNamespace))
		{
			string nsPattern = rootNamespace + ".";
			if (relative.StartsWith(nsPattern, StringComparison.Ordinal))
			{
				relative = relative.Substring(nsPattern.Length);
			}
		}

		// Then, strip the configured prefix if any.
		if (string.IsNullOrEmpty(prefix))
		{
			return relative;
		}

		string pattern = prefix + ".";
		if (relative.StartsWith(pattern, StringComparison.Ordinal))
		{
			return relative.Substring(pattern.Length);
		}

		return relative;
	}

	private void GenerateSource(SourceProductionContext context,
		ImmutableArray<AdditionalText> files,
		AnalyzerConfigOptionsProvider analyzerConfig,
		string? rootNamespace)
	{
		if (files.IsDefaultOrEmpty)
		{
			return;
		}

		string? resourcePrefix = EmbeddedResourceAccessGenerator.GetResourcePrefix(analyzerConfig);

		List<EmbeddedResourceItem> embeddedResources = [];
		foreach (AdditionalText text in files)
		{
			string manifestResourceName = EmbeddedResourceAccessGenerator.GetManifestResourceName(text, analyzerConfig);
			if (string.IsNullOrEmpty(manifestResourceName))
			{
				continue;
			}

			string relativeName = EmbeddedResourceAccessGenerator.GetRelativeResourceName(manifestResourceName, rootNamespace, resourcePrefix);
			embeddedResources.Add(new EmbeddedResourceItem(relativeName, manifestResourceName));
		}

		if (embeddedResources.Count == 0)
		{
			return;
		}

		// If root namespace is null or empty, skip generation to avoid producing invalid code.
		if (string.IsNullOrEmpty(rootNamespace))
		{
			return;
		}

		// Build a tree structure from the stripped manifest resource names.
		// The name (e.g. "TestAssets.Subfolder.Test.txt") maps to nested classes.
		var root = new ResourceNode(string.Empty, null);

		foreach (var item in embeddedResources)
		{
			// Split the stripped name by '.' to create nested classes for each part.
			// e.g., "TestAssets.Subfolder.Test.txt" -> "TestAssets" -> "Subfolder" -> "Test" -> "txt"
			string[] nameParts = item.StrippedName.Split('.');
			ResourceNode current = root;
			for (int i = 0; i < nameParts.Length; i++)
			{
				string identifier = EmbeddedResourceAccessGenerator.GetValidIdentifierName(nameParts[i]);
				// Only the last part gets the resource item.
				EmbeddedResourceItem? resourceItem = i == nameParts.Length - 1 ? item : null;
				var result = current.FindOrCreateChild(identifier, resourceItem);
				if (result.Collision && resourceItem != null)
				{
					context.ReportDiagnostic(Diagnostic.Create(EmbeddedResourceAccessGenerator.resourceCollisionWarning, Location.None,
						identifier, result.ExistingNode.ResourceItem?.ManifestResourceName ?? "unknown", resourceItem.ManifestResourceName));
				}
				current = result.ExistingNode;
			}
		}

		StringBuilder sourceBuilder = new();
		sourceBuilder.AppendLine("#nullable enable");
		sourceBuilder.AppendLine("#pragma warning disable CS8981 // The type name differs only by case from the local type name");
		sourceBuilder.AppendLine($"namespace {rootNamespace};");
		sourceBuilder.AppendLine("using System;");
		sourceBuilder.AppendLine("using System.IO;");
		sourceBuilder.AppendLine("using System.Reflection;");
		sourceBuilder.AppendLine();
		sourceBuilder.AppendLine("/// <summary>");
		sourceBuilder.AppendLine("/// Auto-generated class to access all embedded resources in an assembly.");
		sourceBuilder.AppendLine("/// </summary>");
		sourceBuilder.AppendLine("internal static partial class EmbeddedResources");
		sourceBuilder.AppendLine("{");

		// Generate the class hierarchy.
		this.EmitResourceTree(sourceBuilder, root, 1);

		sourceBuilder.AppendLine("}");
		sourceBuilder.AppendLine("#nullable restore");

		SourceText source = SourceText.From(sourceBuilder.ToString(), Encoding.UTF8);
		context.AddSource("EmbeddedResources.generated.cs", source);
	}

	/// <summary>
	/// Recursively emits the resource tree as nested static classes.
	/// Each file becomes a nested static class with GetStream() and GetReader() methods.
	/// Each directory becomes a nested static class containing its children.
	/// </summary>
	private void EmitResourceTree(StringBuilder sb, ResourceNode node, int indent)
	{
		int indentSize = indent * 2;
		string indentStr = new(' ', indentSize);

		foreach (var child in node.Children)
		{
			if (child.ResourceItem != null)
			{
				// This is a leaf node — a file resource. Emit a nested class for it.
				string manifestName = child.ResourceItem.ManifestResourceName;

				sb.AppendLine($"{indentStr}/// <summary>");
				sb.AppendLine($"{indentStr}/// Represents the embedded resource '{manifestName}'.");
				sb.AppendLine($"{indentStr}/// </summary>");
				sb.AppendLine($"{indentStr}internal static class {child.Identifier}");
				sb.AppendLine($"{indentStr}{{");

				string innerIndent = new(' ', (indent + 1) * 2);

				sb.AppendLine($"{innerIndent}/// <summary>");
				sb.AppendLine($"{innerIndent}/// Gets the embedded resource '{manifestName}' as a stream.");
				sb.AppendLine($"{innerIndent}/// </summary>");
				sb.AppendLine($"{innerIndent}internal static Stream GetStream()");
				sb.AppendLine($"{innerIndent}{{");
				sb.AppendLine($"{innerIndent}\tAssembly assembly = typeof(EmbeddedResources).Assembly;");
				sb.AppendLine($"{innerIndent}\treturn assembly.GetManifestResourceStream(\"{manifestName}\")!;");
				sb.AppendLine($"{innerIndent}}}");
				sb.AppendLine();
				sb.AppendLine($"{innerIndent}/// <summary>");
				sb.AppendLine($"{innerIndent}/// Gets the embedded resource '{manifestName}' as a StreamReader.");
				sb.AppendLine($"{innerIndent}/// </summary>");
				sb.AppendLine($"{innerIndent}internal static StreamReader GetReader()");
				sb.AppendLine($"{innerIndent}{{");
				sb.AppendLine($"{innerIndent}\tAssembly assembly = typeof(EmbeddedResources).Assembly;");
				sb.AppendLine($"{innerIndent}\treturn new StreamReader(assembly.GetManifestResourceStream(\"{manifestName}\")!);");
				sb.AppendLine($"{innerIndent}}}");

				sb.AppendLine($"{indentStr}}}");
				sb.AppendLine();
			}
			else
			{
				// This is a directory node — emit a nested static class.
				sb.AppendLine($"{indentStr}/// <summary>");
				sb.AppendLine($"{indentStr}/// Auto-generated class for embedded resources in the '{child.Identifier}' folder.");
				sb.AppendLine($"{indentStr}/// </summary>");
				sb.AppendLine($"{indentStr}internal static class {child.Identifier}");
				sb.AppendLine($"{indentStr}{{");

				this.EmitResourceTree(sb, child, indent + 1);

				sb.AppendLine($"{indentStr}}}");
				sb.AppendLine();
			}
		}
	}

	private static string GetValidIdentifierName(string resourceName)
	{
		StringBuilder sb = new(resourceName);

		bool first = true;
		for (int index = 0; index < resourceName.Length; index++)
		{
			char c = resourceName[index];
			bool replace;
			switch (char.GetUnicodeCategory(c))
			{
				case UnicodeCategory.LowercaseLetter:
				case UnicodeCategory.UppercaseLetter:
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.ModifierLetter:
				case UnicodeCategory.OtherLetter:
					replace = false;
					break;
				case UnicodeCategory.ConnectorPunctuation:
				case UnicodeCategory.DecimalDigitNumber:
				case UnicodeCategory.Format:
				case UnicodeCategory.LetterNumber:
				case UnicodeCategory.NonSpacingMark:
				case UnicodeCategory.SpacingCombiningMark:
					// Only valid in non-leading position.
					replace = first;
					break;
				default:
					replace = true;
					break;
			}

			if (replace)
			{
				sb[index] = '_';
			}

			first = false;
		}

 		string identifier = sb.ToString();

 		// Ensure the identifier always starts with an uppercase letter to avoid
 		// conflicts with C# keywords (which are all lowercase). This also handles
 		// the case-insensitive filesystem collision issue: files like "Test.txt" and
 		// "test.txt" will produce the same identifier after uppercasing the first char,
 		// but they will be caught by the collision detection below.
 		if (identifier.Length > 0 && char.IsLower(identifier[0]))
 		{
 			identifier = char.ToUpper(identifier[0], CultureInfo.InvariantCulture) + identifier.Substring(1);
 		}

 		return identifier;
 	}

}

/// <summary>
/// Represents a node in the resource tree (either a folder or a file).
/// </summary>
internal class ResourceNode
{
	public string Identifier { get; }
	public EmbeddedResourceItem? ResourceItem { get; }
	public List<ResourceNode> Children { get; } = [];

	public ResourceNode(string identifier, EmbeddedResourceItem? resourceItem)
	{
		this.Identifier = identifier;
		this.ResourceItem = resourceItem;
	}

	public (bool Collision, ResourceNode ExistingNode) FindOrCreateChild(string identifier, EmbeddedResourceItem? resourceItem)
	{
		foreach (var child in this.Children)
		{
			if (child.Identifier == identifier)
			{
				// Collision detected — return the existing node and flag the collision.
				return (true, child);
			}
		}

		var newNode = new ResourceNode(identifier, resourceItem);
		this.Children.Add(newNode);
		return (false, newNode);
	}
}
