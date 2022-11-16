<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0\LINQPadExtras.dll</Reference>
  <Namespace>LINQPadExtras</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
</Query>

#load "..\cfg"
#load "..\libs-lowlevel\xml"
#load ".\api-common"

const string OrigSlnFolder = @"C:\Dev_Nuget\Libs\LINQPadExtras";
const string WorkFolder = @"C:\tmp\sln-work";
static readonly string WorkPrevFolder = Path.Combine(WorkFolder, "prev");
static readonly string WorkNextFolder = Path.Combine(WorkFolder, "next");

void Main()
{
	/*(
		from slnFolder in Cfg.Solutions
		let norm = ApiSolution.GetNorm(slnFolder)
		from line in norm.GetLogChangeLines().Prepend($"\n{slnFolder}\n{new string('=', slnFolder.Length)}")
		select line
	)
		.ForEach(line => line.Dump());*/
	
	var slnFolder = @"C:\Dev_Nuget\Libs\PowWeb";
	var norm = ApiSolution.GetNorm(slnFolder);
	var lines = norm.GetLogChangeLines();
	lines.Dump();
	
}


public record Norm(
	string SlnFolder,
	IReadOnlyDictionary<string, string> FilesToWrite,
	string[] FilesToDelete
)
{
	public bool IsEmpty =>
		FilesToDelete.Length == 0 &&
		FilesToWrite.All(kv => !File.Exists(kv.Key) || kv.Value == File.ReadAllText(kv.Key));

	public string[] GetLogChangeLines() =>
		FilesToDelete.Select(e => $"deleting: {e}")
		.Concat(FilesToWrite.Select(kv => $"changing: {kv.Key}"))
		.ToArray();
	
	public void Apply()
	{
		foreach (var fileToDelete in FilesToDelete) File.Delete(fileToDelete);
		foreach (var (file, str) in FilesToWrite) File.WriteAllText(file, str);
	}
}


public static class ApiSolution
{
	private static readonly Dictionary<string, string> frameworkUpgradeMap = new()
	{
		{ "net6.0", "net7.0" },
		{ "net6.0-windows", "net7.0-windows" },
	};
	
	public static Norm GetNorm(string slnFolder)
	{
		var writer = new SlnNormWriter(slnFolder);
		var propFile = Path.Combine(slnFolder, "Directory.Build.props");
		
		FixReadme(writer);
		
		var propNfo = ReadPropFile(propFile);
		var propFileContent = GenPropFile(propNfo, slnFolder);
		writer.Write(propFile, propFileContent);
		var baseProps = Xml.ModGetFromString(propFileContent, mod => mod.GetChildrenNames("Project.PropertyGroup"));
		var prjFiles = Files.FindRecursively(slnFolder, "*.csproj");
		foreach (var prjFile in prjFiles)
			NormalizePrj(writer, prjFile, baseProps);
		
		return writer.GetNorm();
	}

	private static void NormalizePrj(SlnNormWriter writer, string prjFile, string[] baseProps)
	{
		var str = Xml.ModSaveToString(prjFile, mod =>
		{
			string P(string? s = null) => s switch
			{
				null => "Project.PropertyGroup",
				not null => $"Project.PropertyGroup.{s}"
			};
			mod.ForceSave();
			mod.RemoveChildren(P(), baseProps);
			mod.RemoveFlagsSetToDefaultValues();
			var isPackable = mod.GetFlag(XmlFlag.IsPackable);
			if (isPackable) mod.SetFlag(XmlFlag.GenerateDocumentationFile, true);
			var frameworkSrc = mod.GetOpt(P("TargetFramework"));
			if (frameworkSrc != null)
				if (frameworkUpgradeMap.TryGetValue(frameworkSrc, out var frameworkDst))
					mod.Set(P("TargetFramework"), frameworkDst);
		});
		if (str != null)
			writer.Write(prjFile, str);
	}

	private static void FixReadme(SlnNormWriter writer)
	{
		var slnName = Path.GetFileName(writer.SlnFolder);
		var fileDst = Path.Combine(writer.SlnFolder, "README.md");
		var fileSrcs = Directory.GetFiles(writer.SlnFolder, "README.md");
		switch (fileSrcs.Length)
		{
			case 0:
				writer.Write(fileDst, $"# {slnName}");
				break;

			case 1:
				var fileSrc = fileSrcs[0];
				if (fileSrc != fileDst)
				{
					var str = File.ReadAllText(fileSrc);
					writer.Delete(fileSrc);
					writer.Write(fileDst, str);
				}
				break;

			default:
				throw new ArgumentException();
		}
	}

	private record PropNfo(
		string? Version,
		string? PackageLicenseExpression,
		string? Authors,
		string? Description,
		string? PackageTags,
		string? PackageProjectUrl
	);
	
	private static PropNfo ReadPropFile(string file) => Xml.ModGet(file, mod => new PropNfo(
		mod.GetOpt("Project.PropertyGroup.Version"),
		mod.GetOpt("Project.PropertyGroup.PackageLicenseExpression"),
		mod.GetOpt("Project.PropertyGroup.Authors"),
		mod.GetOpt("Project.PropertyGroup.Description"),
		mod.GetOpt("Project.PropertyGroup.PackageTags"),
		mod.GetOpt("Project.PropertyGroup.PackageProjectUrl")
	));

	private static string GenPropFile(PropNfo nfo, string slnFolder) => $"""
		<Project>
		
			<PropertyGroup>
				<LangVersion>preview</LangVersion>
				<EnablePreviewFeatures>True</EnablePreviewFeatures>
				<Nullable>enable</Nullable>
				<ImplicitUsings>enable</ImplicitUsings>
				<DebugType>embedded</DebugType>
				<Version>{nfo.Version}</Version>
				<PackageLicenseExpression>{nfo.PackageLicenseExpression}</PackageLicenseExpression>
				<Authors>{nfo.Authors}</Authors>
				<Description>{nfo.Description}</Description>
				<PackageTags>{nfo.PackageTags}</PackageTags>
				<PackageReadmeFile>README.md</PackageReadmeFile>
				<PackageProjectUrl>{nfo.PackageProjectUrl ?? $"https://github.com/vlad2048/{Path.GetFileName(slnFolder)}"}</PackageProjectUrl>
			</PropertyGroup>

			<ItemGroup>
				<None Include="$(SolutionDir)\README.md" Pack="true" PackagePath="\" />
			</ItemGroup>

		</Project>
		""";
		
	public static DateTime GetPrjTime(PrjNfo prj) => ApiCommon.GetFolderLastTimestamp(prj.Folder, "bin", "obj");
}




static class SlnUtils
{
	public static void Prepare()
	{
		if (!Directory.Exists(WorkPrevFolder))
			CopySlnFiles(OrigSlnFolder, WorkPrevFolder);
		DeleteFolder(WorkNextFolder);
		CopySlnFiles(OrigSlnFolder, WorkNextFolder);
	}

	private static void DeleteFolder(string folder)
	{
		if (!Directory.Exists(folder)) return;
		Directory.Delete(folder, true);
	}

	private static void CopySlnFiles(string srcFolder, string dstFolder)
	{
		Directory.CreateDirectory(dstFolder);
		var srcFiles = Files.FindRecursively(srcFolder, "*.csproj").Concat(new[] { Path.Combine(srcFolder, "Directory.Build.props") }).ToArray();
		foreach (var srcFile in srcFiles)
		{
			var dstFile = srcFile.Replace(srcFolder, dstFolder);
			Directory.CreateDirectory(Path.GetDirectoryName(dstFile)!);
			File.Copy(srcFile, dstFile);
		}
	}
}


class SlnNormWriter
{
	private readonly Dictionary<string, string> filesToWrite = new();
	private readonly List<string> filesToDelete = new();

	public string SlnFolder { get; }
	public Norm GetNorm() => new(SlnFolder, filesToWrite.AsReadOnly(), filesToDelete.ToArray());
	
	public SlnNormWriter(string slnFolder)
	{
		this.SlnFolder = slnFolder;
	}
	
	public void Write(string file, string str) => filesToWrite[file] = str;
	public void Delete(string file) => filesToDelete.Add(file);
}









