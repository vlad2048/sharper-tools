<Query Kind="Program">
  <Namespace>LINQPad.Controls</Namespace>
</Query>

#load ".\01_structs"

void Main()
{

}

public enum ElType
{
	Attr,
	Node
}

public static class XmlUtils
{
	public static XmlNode? SelNode(this XmlNode root, string xpath)
	{
		var node = root.SelectSingleNode(xpath);
		return node;
	}

	public static XmlNode[] SelNodes(this Xml xml, string xpath)
	{
		var nodes = xml.Root.SelectNodes(xpath);
		if (nodes == null) return Array.Empty<XmlNode>();
		var list = new List<XmlNode>();
		foreach (XmlNode node in nodes)
			list.Add(node);
		return list.ToArray();
	}

	public static XmlAttribute[] GetAttrs(this XmlNode node)
	{
		var list = new List<XmlAttribute>();
		foreach (XmlAttribute xmlAttr in node.Attributes!)
			list.Add(xmlAttr);
		return list.ToArray();
	}

	public static void WriteEl(XmlNode node, string elName, ElType elType, string val)
	{
		switch (elType)
		{
			case ElType.Attr:
				var attrs = node.GetAttrs();
				var elAttr = attrs.First(e => e.Name == elName);
				elAttr.Value = val;
				break;

			case ElType.Node:
				var elNode = node.SelNode(elName)!;
				elNode.InnerText = val;
				break;

			default:
				throw new ArgumentException();
		}
	}

	public static (string, ElType) ReadEl(XmlNode node, string elName)
	{
		var attrs = node.GetAttrs();
		var elAttr = attrs.FirstOrDefault(e => e.Name == elName);
		if (elAttr != null) return (elAttr.Value, ElType.Attr);
		var elNode = node.SelNode(elName);
		if (elNode == null) throw new ArgumentException();
		return (elNode.InnerText, ElType.Node);
	}
}

public class Xml
{
	private static readonly Regex nsRegex = new(@"(?<=xmlns="")[\w\./:]+(?="")");
	private readonly string file;
	private readonly XmlDocument doc;
	private readonly string firstLine;

	public XmlElement Root { get; }

	public Xml(Proj proj)
	{
		this.file = proj.File;
		var lines = File.ReadAllLines(file);
		firstLine = lines[0];
		var (fixedFirstLine, _) = FirstLineSplit(firstLine);
		lines[0] = fixedFirstLine;
		var str = string.Join(Environment.NewLine, lines);
		doc = new XmlDocument();
		doc.LoadXml(str);
		Root = doc.DocumentElement!;
	}

	public void Save(string? fileOut = null)
	{
		using var sw = new StringWriter();
		doc.Save(sw);
		sw.Flush();
		var str = sw.GetStringBuilder().ToString();
		var lines = str.Split(Environment.NewLine);
		if (lines[0].StartsWith("<?xml ")) lines = lines.Skip(1).ToArray();
		lines[0] = firstLine;
		var finalStr = string.Join(Environment.NewLine, lines);
		File.WriteAllText(fileOut ?? file, finalStr);
	}

	private static (string, string?) FirstLineSplit(string line)
	{
		if (!line.Contains("xmlns=")) return (line, null);
		var match = nsRegex.Match(line);
		if (match == null) return (line, null);
		var strBefore = line[..(match.Index - 7)];
		var strAfter = line[(match.Index + match.Length + 2)..];
		return (strBefore + strAfter, match.Value);
	}

	private static string FirstLineMerge(string line, string? ns)
	{
		if (ns == null) return line;
		var index = line.IndexOf(' ');
		return line[..index] + $@" xmlns=""{ns}""" + line[index..];
	}
}


public interface IDC
{
	void Update(string s);
}


public static class ConUtils
{
	private class DC : IDC
	{
		private readonly DumpContainer dc = new();
		public DC()
		{
			dc.Dump();
		}
		public void Update(string s)
		{
			var span = new Span(s);
			var div = new Div(span);
			div.Styles["font-family"] = "Consolas";
			div.Styles["font-size"] = "12px";
			div.Styles["font-weight"] = "bold";
			div.Styles["padding"] = "5px";
			div.Styles["white-space"] = "nowrap";
			dc.UpdateContent(div);
		}
	}
	
	public static IDC GetDC() => new DC();

	public static void Print(this string s)
	{
		var span = new Span(s);
		var div = new Div(span);
		div.Styles["font-family"] = "Consolas";
		div.Styles["font-size"] = "12px";
		div.Styles["font-weight"] = "bold";
		div.Styles["padding"] = "5px";
		div.Styles["white-space"] = "nowrap";
		div.Dump();
	}
}




public static class Files
{
	public static string[] FindRecursively(string folder, string pattern)
	{
		var list = new List<string>();
		void Recurse(string curFolder)
		{
			list.AddRange(Directory.GetFiles(curFolder, pattern));
			var subFolders = Directory.GetDirectories(curFolder);
			foreach (var subFolder in subFolders)
				Recurse(subFolder);
		}
		Recurse(folder);
		return list.ToArray();
	}
}




public static class EnumExt
{
	public static U[] SelectToArray<T, U>(this IEnumerable<T> source, Func<T, U> mapFun) => source.Select(mapFun).ToArray();
	public static void ForEach<T>(this IEnumerable<T> source, Action<T> action) { foreach (var elt in source) action(elt); }
	public static IEnumerable<T> WhereNot<T>(this IEnumerable<T> source, Func<T, bool> predicate) => source.Where(e => !predicate(e));
}
