using LINQPad.Controls;
using PowBasics.Geom;

namespace ExploreLib._2_Display.Utils;

public static class CtrlPropExt
{
	public static Control SetR(this Control ctrl, R r) => ctrl
		.Set("position", "absolute")
		.Set("display", "block")
		.Set("left", $"{r.X.h()}")
		.Set("top", $"{r.Y.v()}")
		.Set("width", $"{r.Width.h()}")
		.Set("height", $"{r.Height.v()}")
		.Set("line-height", $"{r.Height.v()}")
		.Set("text-align", "center");

	public static Control Set(this Control ctrl, string propName, string propVal)
	{
		ctrl.Styles[propName] = propVal;
		return ctrl;
	}
	
	public static Control SetIf(this Control ctrl, bool condition, string propName, string propVal) => condition switch
	{
		true => ctrl.Set(propName, propVal),
		false => ctrl
	};

	public static Control SetWithAction(this Control ctrl, Action<Control>? optAction)
	{
		optAction?.Invoke(ctrl);
		return ctrl;
	}

	public static Control SetForeColor(this Control ctrl, string? foreColor) => ctrl
		.SetIf(foreColor != null, "color", foreColor!);

	public static Control SetBackColor(this Control ctrl, string? backColor) => ctrl
		.SetIf(backColor != null, "background-color", backColor!);

	public static Control SetColors(this Control ctrl, string? foreColor, string? backColor) => ctrl
		.SetForeColor(foreColor)
		.SetBackColor(backColor);
}