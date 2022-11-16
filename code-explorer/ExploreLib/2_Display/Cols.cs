using System.Runtime.CompilerServices;
using ExploreLib._1_Structs.Enum;
using ExploreLib._2_Display.Utils;
using PowBasics.Geom;

namespace ExploreLib._2_Display;

static class DispConsts
{
	//public static readonly Sz GutterSz = new(3, 0);

	public const string ArrowName = "arrowhead";

	public static readonly string ArrowDef = $"""
		<marker
			id="{ArrowName}"
			markerWidth ="10"
			markerHeight="10"
			refX="10"
			refY="5"
			orient="auto"
		>
			<polygon
				points = "4 3, 10 5, 4 7"
				stroke = {Cols.TreeArrowHeadStroke}
				stroke-width = {Cols.TreeArrowHeadStrokeWidth}
				fill = {Cols.TreeArrowHeadFill}
			/>
		</marker>
		""";
}


public static class Cols
{
	private static readonly string @interface = "#e8fc50";
	public static string Interface => R(@interface);

	private static readonly string @class = "#8cbde4";
	public static string Class => R(@class);

	private static readonly string @struct = "#9367e2";
	public static string Struct => R(@struct);
	
	private static readonly string @enum = "#e64cde";
	public static string Enum => R(@enum);

	private static readonly string inactiveTypeBrightness = "brightness(40%)";
	public static string InactiveTypeBrightness => R(inactiveTypeBrightness);

	
	
	private static readonly string treeGridBorderStyle = "1px dashed #FFFFFF10";
	public static string TreeGridBorderStyle => R(treeGridBorderStyle);

	/*private static readonly string treeArrowLineStyle = "stroke: #145d99; stroke-width: 1px";
	public static string TreeArrowLineStyle => R(treeArrowLineStyle);

	private static readonly string treeArrowEndStyle = "stroke: #145d99; stroke-width: 1px; fill: #178ceb";
	public static string TreeArrowEndStyle => R(treeArrowEndStyle);*/

	private static readonly string treeArrowLineStroke = "#145d99";
	public static string TreeArrowLineStroke => R(treeArrowLineStroke);

	private static readonly string treeArrowLineStrokeWidth = "1px";
	public static string TreeArrowLineStrokeWidth => R(treeArrowLineStrokeWidth);

	private static readonly string treeArrowHeadStroke = "#145d99";
	public static string TreeArrowHeadStroke => R(treeArrowHeadStroke);

	private static readonly string treeArrowHeadStrokeWidth = "1px";
	public static string TreeArrowHeadStrokeWidth => R(treeArrowHeadStrokeWidth);

	private static readonly string treeArrowHeadFill = "#178ceb";
	public static string TreeArrowHeadFill => R(treeArrowHeadFill);




	private static readonly string methRet = "#C14071";
	public static string MethRet => R(methRet);

	private static readonly string methName = "#52FE7F";
	public static string MethName => R(methName);

	private static readonly string methOff = "#8F8F8F";
	public static string MethOff => R(methOff);
	
	private static readonly string methParamType = "#5EAAF5";
	public static string MethParamType => R(methParamType);
	
	private static readonly string methParamName = "#BDBDBD";
	public static string MethParamName => R(methParamName);




	//private static bool isInit;
	//public static void Reset() => isInit = false;

	private static string R(string colVal, [CallerArgumentExpression("colVal")] string? colName = null)
	{
		/*if (!isInit)
		{
			CssVarSetup.Setup(typeof(Cols));
			isInit = true;
		}*/
		CssVarSetup.Setup(typeof(Cols));
		return CssVarSetup.GetCol(colName!);
	}
}

static class ColsUtils
{
	public static string GetColForTypKind(TypKind kind) => kind switch
	{
		TypKind.Interface => Cols.Interface,
		TypKind.Class => Cols.Class,
		TypKind.Struct => Cols.Struct,
		TypKind.Enum => Cols.Enum,
		_ => throw new ArgumentException()
	};
}