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
  <Namespace>DynamicData</Namespace>
</Query>

#load ".\cfg"
#load ".\libs-lowlevel\xml"
#load ".\libs-lowlevel\watcher"
#load ".\libs\api-common"
#load ".\libs\api-nuget"
#load ".\libs\api-github"
#load ".\libs\api-git"
#load ".\libs\api-solution"
#load ".\libs\structs"

// TODOs:
//   - Add GitHub link to SlnDetails
//   - C:\Dev_Nuget\Libs\WinFormsCtrlLibs\_Tools\ColorPicker
//     wrong nuget package found: https://www.nuget.org/packages/ColorPicker/

public static readonly Disp mainD = new();


void Main()
{
	DisplayUtils.Init();
	var progress = ProgressBarMaker.Make("Loading Projects").D(mainD);
	var dryRun = Var.Make(false).D(mainD);

	var solutions = Cfg.Solutions.SelectToArray(e => new Sln(new SlnNfo(e)).D(mainD));
	var slnWatchers = solutions.Select(sln => new FolderWatcher(sln.Nfo.Folder).D(mainD).WhenChange.Select(_ => sln)).Merge();
	var (refresh, whenRefresh) = RxUtils.MakeRefreshEvent();
	var selSln = Var.Make(May.None<Sln>()).D(mainD);

	solutions.Select(e => e.Details).Merge().ToUnit()
		.Display(_ => solutions.Select(sln => Util.Merge(
			new { Solution = Html.Hyperlink(sln.Nfo.Name, () => selSln.V = May.Some(sln), sln.Details.WhenVarSome()) },
			sln.Details.V.IsSome(out var slnDetails) switch
			{
				true => UI_Sln_Details(slnDetails),
				false => new { Version = "_" }
			}
		))).D(mainD)
		.ShowWhen(selSln.WhenVarNone())
		.Dump();
	
	selSln
		.WhenSome()
		.Select(sln => sln.Details.Select(e => sln))
		.Switch()
		.Where(sln => sln.Details.V.IsSome())
		.Display(sln => UI_Sln(sln.Nfo, sln.Details.V.Ensure(), sln.D, selSln, dryRun, () => refresh(sln))).D(mainD)
		.ShowWhen(selSln.WhenVarSome())
		.Dump();


	Task.Run(() => solutions.ForEach(sln => sln.Load()));
	selSln.WhenSome().Subscribe(sln =>
	{
		var slnDetails = sln.Details.V.Ensure();
		Task.Run(() => slnDetails.Prjs.ForEach(prj => prj.Load(slnDetails.Version)));
	}).D(mainD);
	slnWatchers.Merge(whenRefresh.Where(_ => !dryRun.V)).Subscribe(sln =>
	{
		sln.Load();
		var slnDetails = sln.Details.V.Ensure();
		Task.Run(() => slnDetails.Prjs.ForEach(prj => prj.Load(slnDetails.Version)));
	}).D(mainD);
}


object UI_Sln(SlnNfo nfo, SlnDetails details, IRoDispBase slnD, IRwVar<Maybe<Sln>> selSln, IRwVar<bool> dryRun, Action refresh) => new StackPanel(true, ".3em",
		new DumpContainer(Util.VerticalRun(
			new Button("Back", _ => selSln.V = May.None<Sln>()),
			UI_Sln_Github(details, refresh),
			UI_Sln_Solution(details),
			UI_Sln_Version(details),
			Html.FieldSet("Projects", new StackPanel(false,
				Html.CheckBox("Dry run", dryRun),
				details.Prjs.Select(e => e.Details).Merge().ToUnit()
					.Display(_ => details.Prjs.Select(prj => Util.Merge(
						new { Project = prj.Nfo.Name },
						prj.Details.V.IsSome(out var prjDetails) switch
						{
							true => UI_Sln_Prj(prjDetails, dryRun, refresh),
							false => new { Local = "_" }
						}
					))).D(slnD)
			)),
			Html.FieldSet("Ignored Projects",
				details.IgnoredPrjs.Select(prj => new Hyperlink(prj.Name, _ => Xml.Mod(prj.File, mod =>
				{
					mod.SetFlag(XmlFlag.IsPackable, true);
					mod.SetFlag(XmlFlag.GenerateDocumentationFile, true);
				})))
			)
		)),
		Con.Root
	).StyleSideBySide();




object UI_Sln_Prj(PrjDetails prjDetails, IRoVar<bool> dryRun, Action refresh) =>
	new
	{
		Local = Util.HorizontalRun(true,
			new Button("Release Locally", _ => { ApiNuget.ReleaseLocally(prjDetails.Sln.Folder, prjDetails.Nfo, prjDetails.Version, dryRun.V); refresh(); }),
			$"last: {prjDetails.LastLocalVer}"
		),
		Remote = Util.HorizontalRun(true,
			new Button("Release to Nuget", _ => { ApiNuget.ReleaseToNuget(prjDetails.Sln.Folder, prjDetails.Nfo, prjDetails.Version, prjDetails.Nfo.NugetUrl, dryRun.V); refresh(); })
			{ Enabled = !prjDetails.IsVerOnNuget },
			$"last: {prjDetails.LastRemoteVer}"
		),
		Nuget = prjDetails.HasNuget switch
		{
			true => new Hyperlinq(prjDetails.Nfo.NugetUrl, "nuget"),
			false => null
		},
		Times = new
		{
			Project = UI.MkLabel(prjDetails.TimePrj.FmtTime()).StylePrjTime(),
			Local = UI.MkFlagLabel(prjDetails.TimeLocal.FmtTime(), prjDetails.IsTimeLocalUpToDate),
			Remote = UI.MkFlagLabel(prjDetails.TimeRemote.FmtTime(), prjDetails.IsTimeRemoteUpToDate),
		},
		Ignore = new Button("Ignore", _ => Xml.Mod(prjDetails.Nfo.File, mod =>
		{
			mod.SetFlag(XmlFlag.IsPackable, false);
			mod.SetFlag(XmlFlag.GenerateDocumentationFile, false);
		})),
	};






object UI_Sln_Github(SlnDetails sln, Action refresh) => Html.FieldSet("GitHub", sln.HasGitHub switch
{
	true => new Hyperlink(sln.Nfo.GitHubUrl, sln.Nfo.GitHubUrl),
	false => new Button("Create GitHub repo", _ => { ApiGithub.CreateRepo(sln.Nfo); refresh(); })
});

Control UI_Sln_Solution(SlnDetails sln) => Html.FieldSet("Solution", Util.Pivot(UI_Sln_Details(sln)));

object UI_Sln_Version(SlnDetails sln)
{
	var textBox = new TextBox(sln.Version);
	return Html.FieldSet("Version",
		Util.HorizontalRun(true,
			textBox,
			new Button("Update", _ => Xml.Mod(sln.Nfo.DirectoryBuildPropsFile, mod => mod.Set(XmlPaths.Version, textBox.Text)))
		)
	);
}




object UI_Sln_Details(SlnDetails sln) => new
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







static class DisplayUtils
{
	public static void Init() => Util.HtmlHead.AddStyles("""
		thead>tr:first-of-type {
			display: none;
		}
		td {
			vertical-align: middle;
		}
		legend {
			background: transparent;
		}
		"""
	);

	public static (DumpContainer, IDisposable) Display<T>(this IObservable<T> valObs, Func<T, object> dispFun)
	{
		var dc = new DumpContainer();
		var d = new Disp();
		valObs
			.Subscribe(val => dc.UpdateContent(dispFun(val))).D(d);
		return (dc, d);
	}

	public static DumpContainer ShowWhen(this object obj, IRoVar<bool> predicate)
	{
		var dc = new DumpContainer(obj);
		predicate.Subscribe(e => dc.Style = e ? "" : "display:none").D(predicate);
		return dc;
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

	
	
	public static DumpContainer Set(this DumpContainer dc, string key, IRoVar<string> val) { val.Subscribe(v => dc.Set(key, v)).D(mainD); return dc; }
	
	public static Control StyleSideBySide(this Control ctrl) => ctrl
		.Set("display", "flex");
	
	public static Control MarginLeft(this Control ctrl) => ctrl
		.Set("margin-left", "40px");
	
	public static Control StylePrjTime(this Control ctrl) => ctrl
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
	public static (Action<Sln>, IObservable<Sln>) MakeRefreshEvent()
	{
		ISubject<Sln> whenRefreshSubj = new Subject<Sln>().D(mainD);
		return (
			sln => whenRefreshSubj.OnNext(sln),
			whenRefreshSubj.AsObservable()
		);
	}
}