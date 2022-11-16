<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <NuGetReference>DynamicData</NuGetReference>
  <NuGetReference>NuGet.Protocol</NuGetReference>
  <NuGetReference>Octokit</NuGetReference>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>LINQPadExtras</Namespace>
  <Namespace>LINQPadExtras.Styling</Namespace>
  <Namespace>NuGet.Common</Namespace>
  <Namespace>NuGet.Configuration</Namespace>
  <Namespace>NuGet.Protocol</Namespace>
  <Namespace>NuGet.Protocol.Core.Types</Namespace>
  <Namespace>Octokit</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>PowRxVar.Utils</Namespace>
  <Namespace>System.Reactive</Namespace>
  <Namespace>System.Reactive.Concurrency</Namespace>
  <Namespace>System.Reactive.Disposables</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Windows.Threading</Namespace>
  <Namespace>Windows.UI.Core</Namespace>
</Query>

#load ".\cfg"
#load ".\libs-lowlevel\type-utils"
#load ".\libs-lowlevel\xml"
#load ".\libs-lowlevel\watcher"
#load ".\libs\api-common"
#load ".\libs\api-nuget"
#load ".\libs\api-github"
#load ".\libs\api-git"
#load ".\libs\api-solution"
#load ".\libs\structs"

// TODOs:
//   - C:\Dev_Nuget\Libs\WinFormsCtrlLibs\_Tools\ColorPicker
//     wrong nuget package found: https://www.nuget.org/packages/ColorPicker/

public static readonly Disp mainD = new();


Func<Maybe<Sln>, Prj[]> LoadPrjs(IRwVar<double> progress) => maySln =>
	(maySln.IsSome(out var sln) switch
	{
		true => sln.Computed.Prjs,
		false => Array.Empty<PrjNfo>()
	})
		.SelectWithProgress(progress, prjNfo => new Prj(prjNfo, sln));



void Main()
{
	//var a = new SourceCache<SlnNfo, SlnNfo>();
	Util.CreateSynchronizationContext();
	Dbg.Init();

	var (whenRefreshSubj, whenRefresh) = RxEventMaker.Make<Unit>().D(mainD);
	var progress = ProgressBarMaker.Make("Loading Projects").D(mainD);
	var dryRun = Var.Make(false).D(mainD);
	var solutions = Cfg.Solutions.SelectToArray(e => new SlnNfo(e));

	var slnFolder = Var.Make(May.None<SlnNfo>());
	var sln = slnFolder.SelectVarMayWithRefresh(e => new Sln(e, whenRefreshSubj), whenRefresh);
	var prjs = sln.SelectVar(LoadPrjs(progress));

	sln.WhenSome().Select(e => e.WhenRefresh).Switch().ToUnit().Subscribe(whenRefreshSubj).D(mainD);


	UI_SlnBrowser(solutions, slnFolder, progress)
		.ShowWhen(slnFolder.WhenVarNone()).Dump();
	

	sln.DisplayMay(_sln => new StackPanel(true, ".3em",
		new DumpContainer(Util.VerticalRun(
			new Button("Back", _ => slnFolder.V = May.None<SlnNfo>()),
			UI_Sln(_sln, prjs, dryRun)
		)).Set("width", Con.IsInCmd.SelectVar(e => e ? "200px" : "auto")),
		Con.Root
	).StyleSideBySide()).Dump();
	
	sln.DisposeManyMay().D(mainD);
}

object UI_SlnBrowser(SlnNfo[] solutions, IRwVar<Maybe<SlnNfo>> slnFolder, IRwVar<double> progress) =>
	solutions
		.SelectWithProgress(progress, SlnComputed.Retrieve)
		.Select(e => TypeUtils.Combine(
			new { Solution = new Hyperlink(e.Nfo.Name, _ => slnFolder.V = May.Some(e.Nfo)) },
			UI_Sln_Computed(e)
		));


Control UI_Sln(Sln sln, IRoVar<Prj[]> prjsV, IRwVar<bool> dryRun) =>
	new DumpContainer(
		Util.VerticalRun(
			UI_Sln_Github(sln),
			UI_Sln_Solution(sln),
			UI_Sln_Version(sln),
			Html.FieldSet("Projects", new StackPanel(false,
				Html.CheckBox("Dry run", dryRun),
				prjsV.DisplayArr(prj => UI_Sln_Prj(prj, dryRun)))
			),
			Html.FieldSet("Ignored Projects",
				sln.Computed.IgnoredPrjs.Select(prj => new Hyperlink(prj.Name, _ => Xml.SetFlag(prj.File, XmlFlag.IsPackable, true)))
			)
		)
	);

object UI_Sln_Computed(SlnComputed sln) => new
{
	Version = sln.Version,
	Links = new StackPanel(true,
		UI.MkImgBtn(@"art\link-icons\icon-vs.png", () => Process.Start("explorer", sln.SolutionFile)),
		UI.MkImgBtn(@"art\link-icons\icon-fileexplorer.png", () => Process.Start("explorer", sln.Folder)),
		UI.MkImgBtn(@"art\link-icons\icon-cmd.png", () => Process.Start("wt", $@"-d ""{sln.Folder}""")),
		UI.MkImgBtn(@"art\link-icons\icon-vscode.png", () => Process.Start(Cfg.Tools.VSCode, $"\"{sln.Folder}\""))
	),
	GitStatus = UI.MkFlagLabel(sln.GitStatus.Fmt(), sln.GitStatus == GitStatus.Clean),
	GitTrackingStatus = UI.MkFlagLabel(sln.GitTrackingStatus.Fmt(), sln.GitTrackingStatus == GitTrackingStatus.Clean),
	Normalize = sln.Norm.IsEmpty switch
	{
		true => (object)UI.MkGreenLabel("normalized"),
		false => new StackPanel(true,
			new Button("Normalize", _ => sln.Norm.Apply())
				{ Enabled = GitEnumUtils.IsNormEnabled(sln.Norm.IsEmpty, sln.GitStatus, sln.GitTrackingStatus) },
			UI.MkRedLabel(GitEnumUtils.FmtNorm(sln.Norm.IsEmpty, sln.GitStatus, sln.GitTrackingStatus))
		)
	}
};


Control UI_Sln_Solution(Sln sln) => Html.FieldSet("Solution", Util.Pivot(UI_Sln_Computed(sln.Computed)));

object UI_Sln_Github(Sln sln) => Html.FieldSet("GitHub", sln.Computed.HasGitHub switch
{
	true => new Hyperlink(sln.Nfo.GitHubUrl, sln.Nfo.GitHubUrl),
	false => new Button("Create GitHub repo", _ => { ApiGithub.CreateRepo(sln.Nfo); sln.Refresh(); })
});

object UI_Sln_Version(Sln sln)
{
	var textBox = new TextBox(sln.Version);
	return Html.FieldSet("Version",
		textBox,
		new Button("Update", _ => Xml.Mod(sln.Nfo.DirectoryBuildPropsFile, mod => mod.Set(XmlPaths.Version, textBox.Text)))
	);
}

object UI_Sln_Prj(Prj prj, IRoVar<bool> dryRun) =>
	new
	{
		Project = prj.Name,
		Local = Util.HorizontalRun(true,
			new Button("Release Locally", _ => { ApiNuget.ReleaseLocally(prj.Sln.Folder, prj.Nfo, prj.Version, dryRun.V); prj.Refresh(); }),
			$"last: {prj.Computed.LastLocalVer}"
		),
		Remote = Util.HorizontalRun(true,
			new Button("Release to Nuget", _ => { ApiNuget.ReleaseToNuget(prj.Sln.Folder, prj.Nfo, prj.Version, prj.NugetUrl, dryRun.V); prj.Refresh(); })
			{ Enabled = !prj.Computed.IsVerOnNuget },
			$"last: {prj.Computed.LastRemoteVer}"
		),
		Nuget = prj.Computed.HasNuget switch
		{
			true => new Hyperlinq(prj.NugetUrl),
			false => null
		},
		Times = new
		{
			Project = UI.MkLabel(prj.Computed.TimePrj.FmtTime()).StylePrjTime(),
			Local = UI.MkFlagLabel(prj.Computed.TimeLocal.FmtTime(), prj.Computed.IsTimeLocalUpToDate),
			Remote = UI.MkFlagLabel(prj.Computed.TimeRemote.FmtTime(), prj.Computed.IsTimeRemoteUpToDate),
		},
		Ignore = new Button("Ignore", _ => Xml.SetFlag(prj.File, XmlFlag.IsPackable, false)),
	};



static class DisplayUtils
{
	public static DumpContainer DisplayArr<T>(this IRoVar<T[]> valVar, Func<T, object> dispFun)
	{
		var dc = new DumpContainer();
		valVar
			//.ObserveOnUIThread()
			.Subscribe(arr => dc.UpdateContent(arr.Select(dispFun))).D(valVar);
		return dc;
	}

	public static DumpContainer DisplayMay<T>(this IRoVar<Maybe<T>> valVar, Func<T, object> dispFun)
	{
		var dc = new DumpContainer();
		valVar
			.WhenSome()
			.Subscribe(val => dc.UpdateContent(dispFun(val))).D(valVar);
		return dc
			.ShowWhen(valVar.WhenVarSome());
	}
	
	public static DumpContainer ShowWhen(this object obj, IRoVar<bool> predicate)
	{
		var dc = new DumpContainer(obj);
		predicate.Subscribe(e => dc.Style = e ? "" : "display:none").D(predicate);
		return dc;
	}
	
	public static void DumpIfNot<T>(this object obj, IRoVar<Maybe<T>> mayVar)
	{
		var dc = new DumpContainer(obj).Dump();
		mayVar
			.Select(e => e.IsNone())
			.ObserveOnUIThread()
			.Subscribe(e => dc.Style = e ? "" : "display:none").D(mayVar);
	}
}



internal static class UI
{
	private const string RedCol = "#f55384";
	private const string GreenCol = "#51f086";
	public const string AccentCol = "#5192ed";
	
	public static Image MkImgBtn(string imgRelFile, Action action)
	{
		var baseFolder = Path.GetDirectoryName(Util.CurrentQueryPath)!;
		var file = Path.Combine(baseFolder, imgRelFile);
		var img = new Image(file)
			.Set("cursor", "pointer");
		img.Click += (_, _) => action();
		return img;
	}


	public static LINQPad.Controls.Label MkFlagLabel(string str, bool cond) => cond switch
	{
		true => MkGreenLabel(str),
		false => MkRedLabel(str),
	};
	public static LINQPad.Controls.Label MkGreenLabel(string str) => MkGreenLabelIf(str, true);
	public static LINQPad.Controls.Label MkRedLabel(string str) => MkRedLabelIf(str, true);
	public static LINQPad.Controls.Label MkGreenLabelIf(string str, bool cond)
	{
		var label = MkLabel(str);
		return cond switch
		{
			false => label,
			true => label.SetForeColor(GreenCol)
		};
	}
	public static LINQPad.Controls.Label MkRedLabelIf(string str, bool cond)
	{
		var label = MkLabel(str);
		return cond switch
		{
			false => label,
			true => label.SetForeColor(RedCol)
		};
	}
	
	public static LINQPad.Controls.Label MkLabel(string str) => new(str);
}

static class StyleExt
{
	public static DumpContainer Set(this DumpContainer dc, string key, IRoVar<string> val)
	{
		val.Subscribe(v => dc.Set(key, v)).D(mainD);
		return dc;
	}
	
	/*public static C Set<C>(this C ctrl, string key, IRoVar<string> val) where C : Control
	{
		val.Subscribe(v => ctrl.Set(key, v)).D(mainD);
		return ctrl;
	}*/
	
	public static Control StyleSideBySide(this Control ctrl) =>
		ctrl
			.Set("display", "flex");
			/*.Set("white-space", "auto")
			.Set("display", "grid")
			.Set("grid-template-columns", "1fr 3fr");*/
	
	public static Control MarginLeft(this Control ctrl) =>
		ctrl
			.Set("margin-left", "40px");
	
	public static Control StylePrjTime(this Control ctrl) =>
		ctrl
			.SetForeColor(UI.AccentCol)
			.Set("font-weight", "bold");

	public static string FmtTime(this DateTime time) => $"{time}";
	
	public static string FmtTime(this DateTime? time) => time switch
	{
		not null => time.Value.FmtTime(),
		null => "_",
	};
}



static class RxUtils
{
	public static U[] SelectWithProgress<T, U>(this T[] arr, IRwVar<double> progress, Func<T, U> mapFun)
	{
		progress.V = 0;
		var res = arr
			.Select((elt, idx) =>
			{
				progress.V = (double)(idx + 1) / arr.Length;
				return mapFun(elt);
			})
			.ToArray();
		progress.V = 1;
		return res;
	}
	

	public static IObservable<T> ObserveOnUIThread<T>(this IObservable<T> obs) => obs.ObserveOn(SynchronizationContext.Current!);


	public static IRoVar<T> ObserveVarOn<T>(this IRoVar<T> v, IScheduler scheduler) => Var.Make(
		v.V,
		v.ObserveOn(scheduler)
	).D(mainD);
	
	public static IRoVar<T> ObserveVarOn<T>(this IRoVar<T> v, SynchronizationContext synchronizationContext) => Var.Make(
		v.V,
		v.ObserveOn(synchronizationContext)
	).D(mainD);

	public static IRoVar<T> ObserveVarOnUIThread<T>(this IRoVar<T> v) => v.ObserveVarOn(SynchronizationContext.Current!);
	
	public static IRoVar<T> DelayVar<T>(this IRoVar<T> v, TimeSpan delay) => Var.Make(
		v.V,
		v.Delay(delay).Synchronize()
	).D(mainD);

	public static IRoVar<T> ThrottleVar<T>(this IRoVar<T> v, TimeSpan delay) => Var.Make(
		v.V,
		v.Throttle(delay)
	).D(mainD);

	public static IRoVar<T> SynchronizeVar<T>(this IRoVar<T> v) => Var.Make(
		v.V,
		v.Synchronize()
	).D(mainD);

	public static IRoVar<T> LogV<T>(this IRoVar<T> v, string msg) => Var.Make(
		v.V,
		v.Do(_ => Con.Log($"[{DateTime.Now:HH:mm:ss.fff}] {msg}"))
	).D(mainD);
}


static class Dbg
{
	private static DumpContainer dc0 = null!;
	private static DumpContainer dc1 = null!;
	public static void Init()
	{
		dc0 = new DumpContainer().Dump();
		dc1 = new DumpContainer().Dump();
	}
	public static void Log0(string s) => dc0.UpdateContent(s);
	public static void Log1(string s) => dc1.UpdateContent(s);
}







