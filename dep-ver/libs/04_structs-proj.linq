<Query Kind="Program" />

#load ".\01_structs"
#load ".\02_lowlevel"
#load ".\03_resolver"

void Main()
{
}


public interface IRef : IEquatable<IRef>
{
	string Name { get; }
}

public interface IProjRef : IRef
{
	Proj Proj { get; }
}

public interface IPkgRef : IRef
{
	Version Ver { get; }
	Version? AssVer { get; }
}

public class RootRef : IRef
{
	public string Name => "Root";

	public override string ToString() => Name;
	public object ToDump() => $"{this}";

	public bool Equals(IRef? other) => Equals((object?)other);
	public override bool Equals(object? obj) => obj is RootRef;
	public override int GetHashCode() => 0;
	public static bool operator ==(RootRef? left, RootRef? right) => Equals(left, right);
	public static bool operator !=(RootRef? left, RootRef? right) => !Equals(left, right);
}

public class ProjRef : IProjRef
{
	public string Name { get; }
	public Proj Proj { get; }

	public string File { get; }
	
	public ProjRef(Proj proj, XmlNode node)
	{
		(var relFile, _) = XmlUtils.ReadEl(node, "Include");
		File = Path.GetFullPath(relFile, Path.GetDirectoryName(proj.File)!);
		Name = Path.GetFileNameWithoutExtension(File);
		Proj = new Proj(proj.Sln, File);
	}

	public override string ToString() => Name;
	public object ToDump() => $"{this}";

	public bool Equals(IRef? other) => Equals((object?)other);
	public override bool Equals(object? obj) => obj is ProjRef o && o.Name == Name;
	public override int GetHashCode() => Name.GetHashCode();
	public static bool operator ==(ProjRef? left, ProjRef? right) => Equals(left, right);
	public static bool operator !=(ProjRef? left, ProjRef? right) => Equals(left, right);
}

public class PkgRefInProj : IProjRef, IPkgRef
{
	private readonly XmlNode? node;
	private readonly ElType nameElType;
	private readonly ElType verElType;
	private Version ver;

	public string Name { get; }
	public Proj Proj { get; }
	public Version Ver { get => ver; set { ver = value; XmlUtils.WriteEl(node, "Version", verElType, value.ToString()); } }
	public Version? AssVer { get; }

	public Xml Xml { get; }

	public PkgRefInProj(Xml xml, Proj proj, XmlNode node)
	{
		Xml = xml;
		this.node = node;
		Proj = proj;
		(Name, nameElType) = XmlUtils.ReadEl(node, "Include");
		(var verStr, verElType) = XmlUtils.ReadEl(node, "Version");
		ver = Version.Parse(verStr);
		AssVer = CacheUtils.GetAssVer(Name, Ver);
	}

	public override string ToString() => $"{Name} @ {Ver}{AssVerStr}";
	private string AssVerStr => AssVer switch
	{
		null => string.Empty,
		not null => $" ({AssVer})"
	};
	public object ToDump() => $"{this}";

	public bool Equals(IRef? other) => Equals((object?)other);
	public override bool Equals(object? obj) => obj is PkgRefInProj o && o.Name == Name && o.Ver == Ver;
	public override int GetHashCode() => HashCode.Combine(Name, Ver);
	public static bool operator ==(PkgRefInProj? left, PkgRefInProj? right) => Equals(left, right);
	public static bool operator !=(PkgRefInProj? left, PkgRefInProj? right) => !Equals(left, right);
}

public class PkgRef : IPkgRef
{
	public string Name { get; }
	public Version Ver { get; }
	public Version? AssVer { get; }

	public PkgRef(string name, Version ver)
	{
		Name = name;
		Ver = ver;
		AssVer = CacheUtils.GetAssVer(Name, Ver);
	}

	public override string ToString() => $"{Name} @ {Ver}{AssVerStr}";
	private string AssVerStr => AssVer switch
	{
		null => string.Empty,
		not null => $" ({AssVer})"
	};
	public object ToDump() => $"{this}";

	public bool Equals(IRef? other) => Equals((object?)other);
	public override bool Equals(object? obj) => obj is PkgRef o && o.Name == Name && o.Ver == Ver;
	public override int GetHashCode() => HashCode.Combine(Name, Ver);
	public static bool operator ==(PkgRef? left, PkgRef? right) => Equals(left, right);
	public static bool operator !=(PkgRef? left, PkgRef? right) => !Equals(left, right);
}

