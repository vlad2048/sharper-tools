using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using LINQPad;

namespace ExploreLib._2_Display.Utils;

static class CssVarSetup
{
	private const string DummyVarName = "dummy";
	private static readonly Dictionary<string, string> csName2CssUseName = new();

	public static string GetCol(string colName) => csName2CssUseName[colName.Replace("@", "")];

	public static void Setup(Type t)
	{
		if (GetVar(DummyVarName) != null) return;

		var fields = t.GetFields(BindingFlags.Static | BindingFlags.NonPublic)
			.Where(e => e.FieldType == typeof(string))
			.ToArray();

		var sb = new StringBuilder();
		sb.AppendLine($"document.documentElement.style.setProperty('--{DummyVarName}', 'white')");
		foreach (var field in fields)
		{
			var fieldName = field.Name;
			var cssName = $"--{field.Name}";
			var cssUseName = $"var(--{field.Name})";
			var val = (string)field.GetValue(null)!;
			csName2CssUseName[fieldName] = cssUseName;
			sb.AppendLine($"document.documentElement.style.setProperty('{cssName}', '{val}')");
		}

		Util.InvokeScript(false, "eval", sb.ToString());
	}

	private static string? GetVar(string name) { var res = Util.InvokeScript(true, "eval", $"document.documentElement.style.getPropertyValue('--{name}')") as string; if (res == string.Empty) return null; return res; }
}