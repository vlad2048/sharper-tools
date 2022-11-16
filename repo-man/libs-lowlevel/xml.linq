<Query Kind="Program" />

void Main()
{
	
}

public enum XmlFlag
{
	IsPackable,
	GenerateDocumentationFile
}

public static class XmlPaths
{
	public const string Version = "Project.PropertyGroup.Version";
}

internal static class XmlFlagConsts
{
	public const string PropsPath = "Project.PropertyGroup";

	public static string FlagPath(XmlFlag flag) => $"{PropsPath}.{flag}";
	
	public static readonly Dictionary<XmlFlag, bool> DefaultValues = new()
	{
		{ XmlFlag.IsPackable, true },
		{ XmlFlag.GenerateDocumentationFile, false },
	};
}


public interface IXmlMod
{
	void ForceSave(string? fileOut = null);
	bool Has(string path);
	string Get(string path);
	string? GetOpt(string path);
	void RemoveFlagsSetToDefaultValues();
	bool GetFlag(XmlFlag flag);
	void SetFlag(XmlFlag flag, bool val);
	void Set(string path, string val);
	string[] GetChildrenNames(string path);
	void RemoveChild(string path);
	void RemoveChildren(string path, string[] childrenNames);
}

static class Xml
{
	public static void Fmt(string file, string? fileOut = null) => Mod(file, mod => mod.ForceSave(fileOut));
	public static string Get(string file, string path) => ModGet(file, mod => mod.Get(path));
	public static bool GetFlag(string file, XmlFlag flag) => ModGet(file, mod => mod.GetFlag(flag));
	public static void SetFlag(string file, XmlFlag flag, bool val) => Mod(file, mod => mod.SetFlag(flag, val));
	public static bool IsSetTo(string file, string path, string expVal) => ModGet(file, mod => mod.GetOpt(path) == expVal);
	public static void Set(string file, string path, string val) => Mod(file, mod => mod.Set(path, val));

	public static void Mod(string file, Action<IXmlMod> action)
	{
		var mod = new XmlMod(file);
		action(mod);
		mod.SaveIFN();
	}

	public static T ModGet<T>(string file, Func<IXmlMod, T> action)
	{
		var mod = new XmlMod(file);
		var res = action(mod);
		mod.SaveIFN();
		return res;
	}

	public static T ModGetFromString<T>(string str, Func<IXmlMod, T> action)
	{
		var mod = new XmlMod(str, true);
		return action(mod);
	}

	public static string ModSaveToString(string file, Action<IXmlMod> action)
	{
		var mod = new XmlMod(file);
		action(mod);
		return mod.GetSaveString();
	}

	class XmlMod : IXmlMod
	{
		private static readonly XmlWriterSettings xmlOpt = new()
		{
			OmitXmlDeclaration = true,
			Indent = true,
			IndentChars = "\t",
		};
		private readonly bool isString;
		private readonly XDocument xDoc;
		private readonly XElement root;
		private string file;
		private bool hasChanged;
		public void SaveIFN()
		{
			if (isString) throw new ArgumentException();
			if (!hasChanged) return;
			var str = GetSaveString();
			File.WriteAllText(file, str);
		}
		public string GetSaveString()
		{
			if (isString) throw new ArgumentException();
			using var sw = new StringWriter();
			using var xw = XmlWriter.Create(sw, xmlOpt);
			xDoc.Save(xw);
			xw.Flush();
			var str = sw.ToString();
			return str.InsertLinesInXml();
		}

		public XmlMod(string file, bool isString = false)
		{
			this.isString = isString;
			this.file = file;
			xDoc = isString switch
			{
				true => XDocument.Parse(file),
				false => XDocument.Load(file)
			};
			root = xDoc.Root!;
		}

		public void ForceSave(string? fileOut)
		{
			hasChanged = true;
			if (fileOut != null) file = fileOut;
		}

		public bool Has(string path) => GetOpt(path) != null;

		public string Get(string path)
		{
			var val = GetOpt(path);
			if (val == null) throw new ArgumentException();
			return val;
		}

		public string? GetOpt(string path) => GetNode(path.ToXPath())?.Value;
		
		public bool GetFlag(XmlFlag flag)
		{
			var val = GetOpt($"{XmlFlagConsts.PropsPath}.{flag}");
			if (val == null) return XmlFlagConsts.DefaultValues[flag];
			return bool.Parse(val);
		}

		public void SetFlag(XmlFlag flag, bool val)
		{
			if (val == XmlFlagConsts.DefaultValues[flag])
			{
				var node = GetNode(XmlFlagConsts.FlagPath(flag));
				if (node != null)
					node.Remove();
			}
			else
			{
				Set(XmlFlagConsts.FlagPath(flag), $"{val}");
			}
		}
		
		public void RemoveFlagsSetToDefaultValues()
		{
			foreach (var (key, defVal) in XmlFlagConsts.DefaultValues)
			{
				var val = GetOpt(XmlFlagConsts.FlagPath(key));
				if (val != null && bool.Parse(val) == defVal)
					RemoveChild(XmlFlagConsts.FlagPath(key));
			}
		}

		public void Set(string path, string val)
		{
			var xPath = path.ToXPath();
			var node = GetNode(xPath);
			if (node != null)
			{
				if (val != node.Value)
				{
					node.Value = val;
					hasChanged = true;
				}
			}
			else
			{
				var xPathParent = xPath.GetParent();
				var xPathLeaf = xPath.GetLeaf();
				var parentNode = GetNode(xPathParent);
				if (parentNode == null) throw new ArgumentException();
				parentNode.Add(new XElement(xPathLeaf)
				{
					Value = val
				});
				hasChanged = true;
			}
		}

		public string[] GetChildrenNames(string path)
		{
			var xPath = path.ToXPath();
			var nodes = GetNodes(xPath);
			return nodes.Select(e => e.Name.LocalName).ToArray();
		}
		
		public void RemoveChild(string path)
		{
			var node = GetNode(path);
			if (node != null)
				node.Remove();
		}
		
		public void RemoveChildren(string path, string[] childrenNames)
		{
			foreach (var childName in childrenNames)
			{
				var childPath = $"{path}.{childName}";
				RemoveChild(childPath);
			}
		}


		private XElement? GetNode(string path)
		{
			var xPath = "/" + path.Replace(".", "/");
			return root.XPathSelectElement(xPath);
		}
		
		private XElement[] GetNodes(string path)
		{
			var node = GetNode(path)!;
			return node.Elements().ToArray();
		}
	}

	private static string InsertLinesInXml(this string str)
	{
		var lines = str.ToLines();

		var indices = lines
			.Select((line, idx) =>
			{
				var indent = GetLeadingIndentCount(line);
				var isClosingTag = IsClosingTag(line);
				return (ShouldInsertLineBefore(indent, isClosingTag), idx);
			})
			.Where(t => t.Item1)
			.Select(t => t.Item2)
			.Reverse()
			.ToArray();

		var lineList = lines.ToList();
		foreach (var idx in indices)
			lineList.Insert(idx, string.Empty);

		return lineList.FromLines();
	}

	private static bool ShouldInsertLineBefore(int indent, bool isClosingTag) => (indent, isClosingTag) switch
	{
		(1, false) => true,
		(0, true) => true,
		_ => false
	};

	private static bool IsClosingTag(this string s) => s.Trim().StartsWith("</");

	private static int GetLeadingIndentCount(this string s)
	{
		var i = 0;
		while (i < s.Length && s[i] == '\t')
			i++;
		return i;
	}

	private static string[] ToLines(this string str) => str.Split(Environment.NewLine);
	private static string FromLines(this IEnumerable<string> strs) => string.Join(Environment.NewLine, strs);
}


static class XmlUtils
{
	public static string ToXPath(this string s) => "/" + s.Replace(".", "/");
	public static string GetParent(this string x) => Path.GetDirectoryName(x)!.Replace(@"\", "/");
	public static string GetLeaf(this string x) => Path.GetFileName(x);
}
