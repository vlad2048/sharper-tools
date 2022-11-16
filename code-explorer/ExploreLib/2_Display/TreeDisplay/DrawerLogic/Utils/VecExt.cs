using PowBasics.Geom;

namespace ExploreLib._2_Display.TreeDisplay.DrawerLogic.Utils;

static class VecExt
{
	public static VecPt ToVec(this Pt pt) => new(pt.X, pt.Y);
	public static VecR ToVec(this R r) => new(r.Pos.ToVec(), new VecPt(r.Right + 1, r.Bottom + 1));
	public static VecPt OnTheRight(this VecR r) => new(r.Min.X + r.Width, r.Min.Y + r.Height / 2);
	public static VecPt OnTheLeft(this VecR r) => new(r.Min.X , r.Min.Y + r.Height / 2);
}