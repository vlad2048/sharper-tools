<Query Kind="Program">
  <NuGetReference>ColorPickerCtrlLib</NuGetReference>
  <NuGetReference>FntPickerCtrlLib</NuGetReference>
  <Namespace>BaseRxLib</Namespace>
  <Namespace>BaseRxLib.Extensions</Namespace>
  <Namespace>BaseRxLib.Vars</Namespace>
  <Namespace>ColorPickerCtrlLib</Namespace>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Reactive</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
  <Namespace>FntPickerCtrlLib</Namespace>
</Query>

void Main()
{
	/*
	var pp = new Panel
	{
		Dock = DockStyle.Fill,
		Margin = new Padding(5),
		BackColor = Color.Red,
	}.Dump();
	return;
	*/


	var ui = new UI();
	
	new Pen(Color.Blue, 3)
	{
		
	};
	
	var sz = 9;
	var style = FontStyle.Bold;
	
	var cBack = ui.MkCol(0x020244, "back");
	var cCmd = ui.MkCol(0x030382, "back-cmd");
	var cCmdHead = ui.MkCol(0x0606E5, "back-cmd-head");
	
	var fCmdHeadArrow = ui.MkFnt(new Fnt(C(0xB0B0B0), sz, style), "fnt-cmd-head-arrow");
	var fCmdHeadTarget = ui.MkFnt(new Fnt(C(0x7AF918), sz, style), "fnt-cmd-head-target");
	var fCmdHeadArgs = ui.MkFnt(new Fnt(C(0x3EFFE5), sz, style), "fnt-cmd-head-args");
	var fCmdOut = ui.MkFnt(new Fnt(C(0x3699F4), sz, style), "fnt-cmd-out");
	var fCmdErr = ui.MkFnt(new Fnt(C(0xFC4238), sz, style), "fnt-cmd-err");


	var pTop = ui.MainPanel.AddCtrl(new Panel
	{
		//Dock = DockStyle.Fill,
	});
	
	var rtb = pTop.AddCtrl(RTB.Make());
	
	ui.MainPanel.Events().Resize.Skip(1).Take(1).Subscribe(_ =>
	{
		//ui.MainPanel.ClientRectangle.Dump("mainPanel");
		//pTop.ClientRectangle.Dump("pTop");
		//rtb.ClientRectangle.Dump("rtb");
		
		pTop.SetR(10);
		rtb.SetR(10);
		
		//ui.MainPanel.ClientRectangle.Dump("mainPanel");
		//pTop.ClientRectangle.Dump("pTop");
		//rtb.ClientRectangle.Dump("rtb");
	});
	
	ui.WhenChange.Subscribe(_ =>
	{
		ui.MainPanel.BackColor = cBack.V;
		pTop.BackColor = cCmd.V;
		rtb.BackColor = cCmd.V;
		rtb.Clear();
		
		rtb.Append(@"▼ ", fCmdHeadArrow, cCmdHead);
		rtb.Append(@"C:\Users\vlad\Documents\LINQPad Queries\nuget-releaser\tools\Cmder\Cmder\bin\Debug\net6.0\Cmder.exe ", fCmdHeadTarget, cCmdHead);
		rtb.AppendLine(@"1 0 ""C:\folder space\file.txt""", fCmdHeadArgs, cCmdHead);
		
		rtb.AppendLine("Hello", fCmdOut);
		rtb.AppendLine("there", fCmdOut);
		rtb.AppendLine("let's get started:", fCmdOut);
		rtb.AppendLine("Same line ... continues ... again ... and now finishes", fCmdOut);

		rtb.AppendLine("Hello", fCmdErr);
		rtb.AppendLine("there", fCmdErr);
		rtb.AppendLine("let's get started:", fCmdErr);
		rtb.AppendLine("Same line ... continues ... again ... and now finishes", fCmdErr);
		
		ui.MainPanel.Update();
	});
	
	ui.MainPanel.WhenDraw(gfx =>
	{
		//"MainPanel.WhenDraw".Dump();
	});
	
	ui.Show();
}



internal class UI
{
	private readonly List<IObservable<Unit>> listObs = new();
	private readonly Panel rootPanel = new Panel();
	private readonly FlowLayoutPanel topPanel = new FlowLayoutPanel
	{
		Dock = DockStyle.Top,
		AutoSize = true,
	};
	public Panel MainPanel { get; } = new Panel
	{
		Dock = DockStyle.Fill,
	};
	public IObservable<Unit> WhenInit => rootPanel.Events().HandleCreated.ToUnit();
	public IObservable<Unit> WhenChange => listObs.Merge();

	public UI()
	{
		rootPanel.Controls.AddRange(new Control[] { MainPanel, topPanel });
	}

	public void Show()
	{
		rootPanel.Dump();
		/*rootPanel.Events().HandleCreated.Subscribe(_ =>
		{
			WhenChange.Subscribe(_ =>
			{
				rootPanel.Update();
			});
		});*/
		WhenChange.Subscribe(_ =>
		{
			rootPanel.Update();
		});
	}


	// ********
	// * Vars *
	// ********
	public IRoVar<Fnt> MkFnt(Fnt val, string name)
	{
		var ctrl = new FntPicker();
		ctrl.Fnt.V = val;
		topPanel.Controls.Add(ctrl);
		listObs.Add(ctrl.Fnt.ToUnit());
		return ctrl.Fnt;
	}

	public IRoVar<Color> MkCol(uint val, string name) => MkCol(C(val), name);
	public IRoVar<Color> MkCol(Color val, string name)
	{
		var ctrl = new ColorPicker();
		ctrl.Title = name;
		ctrl.Color.V = val;
		topPanel.Controls.Add(ctrl);
		listObs.Add(ctrl.Color.ToUnit());
		return ctrl.Color;
	}
}


static class RTB
{
	public static RichTextBox Make() => new RichTextBox
	{
		BackColor = Color.Black,
		ForeColor = Color.White,
		Font = new Font("Consolas", 12),
		Multiline = true,
		WordWrap = false,
		AcceptsTab = true,
		ReadOnly = true,
		ScrollBars = RichTextBoxScrollBars.None,
		BorderStyle = BorderStyle.None,
	};

	public static void AppendLine(this RichTextBox rtb, string str, Action<RichTextBox> fmtFun)
	{
		str += Environment.NewLine;
		rtb.Append(str, fmtFun);
	}

	public static void Append(this RichTextBox rtb, string str, Action<RichTextBox> fmtFun)
	{
		var start = rtb.TextLength;
		rtb.SelectionLength = 0;
		fmtFun(rtb);
		rtb.AppendText(str);
	}

	public static void AppendLine(this RichTextBox rtb, string str, IRoVar<Fnt> fnt, IRoVar<Color>? bkCol = null) =>
		rtb.AppendLine(str, rtb.ConvFnt(fnt.V, bkCol));

	public static void Append(this RichTextBox rtb, string str, IRoVar<Fnt> fnt, IRoVar<Color>? bkCol = null) =>
		rtb.Append(str, rtb.ConvFnt(fnt.V, bkCol));

	private static Action<RichTextBox> ConvFnt(this RichTextBox rtb, Fnt fnt, IRoVar<Color>? bkCol) => e =>
	{
		rtb.SelectionFont = new Font("Consolas", fnt.Size, fnt.Style);
		rtb.SelectionColor = fnt.Color;
		if (bkCol != null)
			rtb.SelectionBackColor = bkCol.V;
	};
}

static class Ext
{
	public static T AddCtrl<T>(this Control parentCtrl, T ctrl) where T : Control
	{
		parentCtrl.Controls.Add(ctrl);
		return ctrl;
	}
	
	public static void WhenDraw(this Control ctrl, Action<Graphics> action)
	{
		ctrl.Events().Paint.Subscribe(e =>
		{
			var gfx = e.Graphics;
			action(gfx);
		});
	}
	
	public static void SetR(this Control ctrl, int margin)
	{
		var par = ctrl.Parent;
		ctrl.Left = margin;
		ctrl.Top = margin;
		ctrl.Width = par.Width - 2 * margin;
		ctrl.Height = par.Height - 2 * margin;
	}
}


static Color C(uint v) => Color.FromArgb((int)(v | 0xFF000000));



public static object ToDump(object o) => o switch
{
	Rectangle e => $"{e.X},{e.Y} {e.Width}x{e.Height}",
	_ => o
};




