using System.Text.RegularExpressions;
using ExploreLib.NugetLogic.Structs;
using ExploreLib.NugetLogic.Structs.Refs;
using Microsoft.Build.Construction;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace ExploreLib.NugetLogic.Utils;

static class PrjUtils
{
	public static Prj Load(string file)
	{
		var root = ProjectRootElement.Open(file)!;
		var itemGrps = root.ItemGroups.ToArray();
	
		var targetFramework = (
			from propGrp in root.PropertyGroups
			from elt in propGrp.Children
			where elt is ProjectPropertyElement
			let propElt = (ProjectPropertyElement)elt
			where propElt.ElementName == "TargetFramework"
			select NuGetFramework.Parse(propElt.Value)
		).First();
	
		var pkgRefs = (
			from itemGrp in itemGrps
			from elt in itemGrp.Children
			where elt is ProjectItemElement
			let prjElt = (ProjectItemElement)elt
			where prjElt.ElementName == "PackageReference"
			select new PkgRef(
				prjElt.Include,
				prjElt.OuterElement.ExtractVersionRange()
			)
		).ToArray();

		var prjRefs = (
			from itemGrp in itemGrps
			from elt in itemGrp.Children
			where elt is ProjectItemElement
			let prjElt = (ProjectItemElement)elt
			where prjElt.ElementName == "ProjectReference"
			select new PrjRef(
				MakeAbsolute(Path.GetDirectoryName(file), prjElt.Include)
			)
		).ToArray();

		return new Prj(
			file,
			targetFramework,
			prjRefs,
			pkgRefs
		);
	}

	private static string MakeAbsolute(string rootFolder, string relFile)
	{
		var folder = Path.Combine(rootFolder, relFile);
		return Path.GetFullPath(folder);
	}


	public static TNod<IRef> BuildTree(Prj rootPrj)
	{
		var root = Nod.Make<IRef>(new PrjRef(rootPrj.File));

		void Recurse(TNod<IRef> node, Prj prj)
		{
			foreach (var prjRef in prj.PrjRefs)
			{
				var childPrj = Load(prjRef.File);
				var childPrjRefNod = Nod.Make<IRef>(new PrjRef(childPrj.File));
				node.AddChild(childPrjRefNod);
				Recurse(childPrjRefNod, childPrj);
			}

			foreach (var pkgRef in prj.PkgRefs)
			{
				var childPkgRefNod = Nod.Make<IRef>(pkgRef);
				node.AddChild(childPkgRefNod);
			}
		}

		Recurse(root, rootPrj);
		return root;
	}



	private static readonly Regex verRangeRegex = new("""(?<=Version=").*(?=")""");

	private static VersionRange ExtractVersionRange(this string s)
	{
		var match = verRangeRegex.Match(s);
		if (!match.Success) throw new ArgumentException();
		var str = match.Value;
		return VersionRange.Parse(str);
	}
}