using System.Text;
using ExploreLib._2_Display.TreeDisplay.DrawerLogic.Utils;
using ExploreLib._2_Display.Utils;
using LINQPad.Controls;
using PowBasics.CollectionsExt;
using PowBasics.Geom;

namespace ExploreLib._2_Display.TreeDisplay.DrawerLogic;

class Drawer : IDrawer
{
	private readonly List<Control> ctrls = new();
	private readonly StringBuilder svgSb = new();

	public Control[] Ctrls =>
		new[] { MakeSvg(svgSb.ToString(), Size) }
			.Concat(ctrls)
			.ToArray();
		
	public Sz Size { get; private set; } = Sz.Empty;

	public R UpdateDims(R r)
	{
		var right = r.Right + 1;
		var bottom = r.Bottom + 1;
		if (right > Size.Width) Size = new Sz(right, Size.Height);
		if (bottom > Size.Height) Size = new Sz(Size.Width, bottom);
		return r;
	}
	private void AddSvg(string str) => svgSb.AppendLine(str);
	private void AddSvgLine(VecPt src, VecPt dst, string? markerEnd) => AddSvg($"""
		<line
			x1="{src.X.h()}"
			y1="{src.Y.v()}"
			x2="{dst.X.h()}"
			y2="{dst.Y.v()}"
			stroke={Cols.TreeArrowLineStroke}
			stroke-width={Cols.TreeArrowLineStrokeWidth}
			{(markerEnd == null ? "" : $"""marker-end="url(#{markerEnd})" """)}
		/>
	""");

	public void Add(Control ctrl) => ctrls.Add(ctrl);

	public void Link(Pt pt, string text, Action onClick, Action<Control>? opt = null)
	{
		var r = new R(pt, new Sz(text.Length, 1));
		Add(
			new Hyperlink(text, _ => onClick())
				.SetR(UpdateDims(r))
				.SetWithAction(opt)
		);
	}
	
	public void DivText(R r, string text, Action<Control>? opt = null) => Add(
		new Span(text)
			.SetR(UpdateDims(r))
			.SetWithAction(opt)
	);

	public void Grid()
	{
		for (var i = 0; i <= Size.Width; i++) Add(
			new Span()
				.SetR(new R(i, 0, 1, Size.Height))
				.Set("border-left", Cols.TreeGridBorderStyle)
		);
		for (var i = 0; i <= Size.Height; i++) Add(
			new Span()
				.SetR(new R(0, i, Size.Width, 1))
				.Set("border-top", Cols.TreeGridBorderStyle)
		);
	}

	public void Arrows(TNod<R> root)
	{
		root.Where(e => e.Children.Count == 1).ForEach(e => DrawSingleArrow(e.V, e.Children[0].V));
		root.Where(e => e.Children.Count > 1).ForEach(e => DrawMultipleArrows(e.V, e.Children.Select(f => f.V).ToArray()));
	}

	private void DrawSingleArrow(R srcR, R dstR)
	{
		var src = srcR.ToVec();
		var dst = dstR.ToVec();
		var ptSrc = src.OnTheRight();
		var ptDstAct = dst.OnTheLeft();
		var ptDst = new VecPt(ptDstAct.X, ptSrc.Y);
		AddSvgLine(ptSrc, ptDst, DispConsts.ArrowName);
	}
	
	private void DrawMultipleArrows(R srcR, R[] dstRs)
	{
		var src = srcR.ToVec();
		var dsts = dstRs.Select(e => e.ToVec()).ToArray();
		var ptSrc = src.OnTheRight();
		var ptMid = new VecPt((ptSrc.X + dsts[0].Min.X) / 2, ptSrc.Y);
		var ptDsts = dsts.Select(e => e.OnTheLeft()).ToArray();
		AddSvgLine(ptSrc, ptMid, null);
		var ptConTop = new VecPt(ptMid.X, ptDsts[0].Y);
		var ptConBottom = new VecPt(ptMid.X, ptDsts[^1].Y);
		AddSvgLine(ptConTop, ptConBottom, null);
		foreach (var ptDst in ptDsts)
		{
			var ptCon = new VecPt(ptMid.X, ptDst.Y);
			AddSvgLine(ptCon, ptDst, DispConsts.ArrowName);
		}
	}

	private static Control MakeSvg(string str, Sz sz) => new Svg($"""
		<defs>
			{DispConsts.ArrowDef}
		</defs>
		{str}
		""",
			0,
			0,
			$"0 0 {sz.Width}ch {sz.Height}em"
		)
		.SetR(new R(Pt.Empty, sz));
}