<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <NuGetReference>DynamicData</NuGetReference>
  <NuGetReference>NuGet.Protocol</NuGetReference>
  <NuGetReference>Octokit</NuGetReference>
  <Namespace>DynamicData</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>LINQPadExtras.Utils</Namespace>
  <Namespace>LINQPadExtras.Utils.Exts</Namespace>
  <Namespace>NuGet.Common</Namespace>
  <Namespace>NuGet.Configuration</Namespace>
  <Namespace>NuGet.Protocol</Namespace>
  <Namespace>NuGet.Protocol.Core.Types</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowMaybeErr</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>PowRxVar.Utils</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Reactive</Namespace>
  <Namespace>System.Reactive.Concurrency</Namespace>
  <Namespace>System.Reactive.Disposables</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Windows.Threading</Namespace>
  <Namespace>Windows.UI.Core</Namespace>
  <Namespace>LINQPadExtras.PageServing</Namespace>
  <CopyLocal>true</CopyLocal>
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
#load ".\libs\release-logic"

// TODOs:
//   - Add GitHub link to SlnDetails
//   - C:\Dev_Nuget\Libs\WinFormsCtrlLibs\_Tools\ColorPicker
//     wrong nuget package found: https://www.nuget.org/packages/ColorPicker/

public static readonly Disp mainD = new();

static SlnDetails[] GetAllSlns(Sln[] slns) => slns.Select(e => e.Details.V).WhereSome().ToArray();

void Main1() { Util.ReadLine(); Main(); }

[STAThread]
void Main()
{
	LINQPadServer.Start(opt => { opt.HtmlEditFolder = @"C:\Dev_Nuget\Libs\LINQPadExtras\_infos\design"; });
	Css.Init();
	
	var showDependencies = Var.MakeBnd(false).D(mainD);
	var dryRun = Var.MakeBnd(false).D(mainD);
	var solutions = Cfg.Solutions.SelectToArray(e => new Sln(new SlnNfo(e)).D(mainD));
	var slnWatchers = solutions.Select(sln => new FolderWatcher(sln.Nfo.Folder).D(mainD).WhenChange.Select(_ => sln)).Merge();
	var (refresh, whenRefresh) = RxUtils.MakeRefreshEvent();
	var selSln = Var.Make(May.None<Sln>()).D(mainD);
	
	solutions.Select(e => e.Details).Merge().ToUnit().Merge(showDependencies.ToUnit())
		.Display(_ =>
			new StackPanel(false, 
				new StackPanel(true,
					Ctrl.CheckBox("Show dependencies", showDependencies),
					Ctrl.CheckBox("Dry run", dryRun)
				),
				new DumpContainer(solutions.Select(sln => Util.Merge(
					new {
						Solution = Ctrl.LinkButton(sln.Nfo.Name, () => selSln.V = May.Some(sln), sln.Details.WhenVarSome()),
						Basics = new StackPanel(true,
							UI.MkImgBtn(@"art\link-icons\icon-fileexplorer.png", () => Process.Start("explorer", sln.Nfo.Folder)),
							UI.MkImgBtn(@"art\link-icons\icon-cmd.png", () => Process.Start("wt", $@"-d ""{sln.Nfo.Folder}""")),
							UI.MkImgBtn(@"art\link-icons\icon-vscode.png", () => Process.Start(Cfg.Tools.VSCode, $"\"{sln.Nfo.Folder}\""))
						)
					},
					sln.Details.V.IsSome(out var slnDetails, out var slnDetailsErr) switch
					{
						true => UI_Sln_Details(sln.GitState.V, slnDetails, showDependencies, () => GetAllSlns(solutions), () => refresh(sln), dryRun),
						false => UtilExtra.MergeOpt(
							new
							{
								Version = UI.MkRedLabel(slnDetailsErr),
							},
							sln.GitState.V.IsSome(out var gitState) switch
							{
								true => new
								{
									GitStatus = UI.MkFlagLabel(gitState.FileState.Fmt(), gitState.FileState == GitFileState.Clean),
									GitSync = UI.MkFlagLabel(gitState.SyncState.Fmt(), gitState.SyncState == GitSyncState.Clean),
								},
								false => null,
							}
						)							
					}
				))))
		).D(mainD)
		.ShowWhen(selSln.WhenVarNone())
		.Dump();
	
	selSln
		.WhenSome()
		.Select(sln => sln.Details.Select(e => sln))
		.Switch()
		.Where(sln => sln.Details.V.IsSome())
		.Display(sln => UI_Sln(sln.Nfo, sln.GitState.V, sln.Details.V.Ensure(), sln.D, selSln, () => GetAllSlns(solutions), () => refresh(sln), showDependencies, dryRun)).D(mainD)
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
		if (sln.Details.V.IsSome(out var slnDetails))
			Task.Run(() => slnDetails.Prjs.ForEach(prj => prj.Load(slnDetails.Version)));
	}).D(mainD);
}


object UI_Sln(SlnNfo nfo, Maybe<GitState> mayGitState, SlnDetails details, IRoDispBase slnD, IRwVar<Maybe<Sln>> selSln, Func<SlnDetails[]> allSlnsFun, Action refresh, IRoVar<bool> showDependencies, IRwVar<bool> dryRun) =>
		new DumpContainer(Util.VerticalRun(
			Ctrl.Button("Back", () => selSln.V = May.None<Sln>()),
			UI_Sln_Solution(mayGitState, details, showDependencies, allSlnsFun, refresh, dryRun),
			UI_Sln_Version(details),
			Html.FieldSet("Projects",
				details.Prjs.Select(e => e.Details).Merge().ToUnit()
					.Display(_ => details.Prjs.Select(prj => Util.Merge(
						new { Project = prj.Nfo.Name },
						prj.Details.V.IsSome(out var prjDetails) switch
						{
							true => UI_Sln_Prj(prjDetails, dryRun, refresh),
							false => new { Local = "_" }
						}
					))).D(slnD)
			),
			Html.FieldSet("Ignored Projects",
				details.IgnoredPrjs.Select(prj => new Hyperlink(prj.Name, _ => Xml.Mod(prj.File, mod =>
				{
					mod.SetFlag(XmlFlag.IsPackable, true);
					//mod.SetFlag(XmlFlag.GenerateDocumentationFile, true);
				})))
			)
		));




object UI_Sln_Prj(PrjDetails prjDetails, IRoVar<bool> dryRun, Action refresh) =>
	new
	{
		Local = UI.DivVert(
			$"{prjDetails.LastLocalVer} ➞ {prjDetails.Version}".Ver(),
			new Button("Release Locally", _ => { ApiNuget.Release(NugetSource.Local, prjDetails.Nfo, prjDetails.Version, false, dryRun.V); refresh(); })
		),
		Remote = UI.DivVert(
			$"{prjDetails.LastRemoteVer} ➞ {prjDetails.Version}".Ver().SetForeColorIf(prjDetails.LastRemoteVer == prjDetails.Version, UI.RedCol),
			new Button("Release to NuGet", _ => { ApiNuget.Release(NugetSource.Remote, prjDetails.Nfo, prjDetails.Version, false, dryRun.V); refresh(); }) { Enabled = !prjDetails.IsVerOnNuget }
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
		})),
	};






Control UI_Sln_Solution(Maybe<GitState> mayGitState, SlnDetails sln, IRoVar<bool> showDependencies, Func<SlnDetails[]> allSlnsFun, Action refresh, IRoVar<bool> dryRun) =>
	Html.FieldSet("Solution", Util.Pivot(
		UI_Sln_Details(mayGitState, sln, showDependencies, allSlnsFun, refresh, dryRun)
	));

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




object UI_Sln_Details(Maybe<GitState> mayGitState, SlnDetails sln, IRoVar<bool> showDependencies, Func<SlnDetails[]> allSlnsFun, Action refresh, IRoVar<bool> dryRun)
{
	var releaseNfo = ReleaseLogic.CanRelease(mayGitState, sln);
	return UtilExtra.MergeOpt(
		new
		{
			Version = sln.Version.Ver(),
			Links = new StackPanel(true,
				UI.MkImgBtn(@"art\link-icons\icon-vs.png", () => Process.Start("explorer", sln.SolutionFile)),
				mayGitState.IsSome(out var gitState) ? UI.MkImgBtn(@"art\link-icons\icon-github.png", () => Process.Start(new ProcessStartInfo(gitState.Url) { UseShellExecute = true })) : new Label("")
			),
			Dependencies = showDependencies.V switch
			{
				false => (object)$"{sln.PkgRefs.Length} dependencies",
				true => sln.PkgRefs
					.GroupBy(e => e.Name)
					.Select(grp => new
					{
						Pkg = grp.Key,
						Vers = grp.Select(e => e.Version).OrderBy(e => e).JoinText(", "),
						Latest = DisplayUtils.GetInTask(() => ApiNuget.GetVers(NugetSource.Remote, grp.Key).FirstOr("_")),
					}),
			},
		},
		mayGitState.IsSome(out var git) switch
		{
			false => new
			{
				GitStatus = new Button("Create GitHub repo", _ => ApiGithub.CreateRepo(sln.Nfo)),
			},
			true => UtilExtra.MergeOpt(
				new
				{
					GitStatus = UI.MkFlagLabel(git.FileState.Fmt(), git.FileState == GitFileState.Clean),
					GitSync = UI.MkFlagLabel(git.SyncState.Fmt(), git.SyncState == GitSyncState.Clean),
					Normalize = sln.Norm.IsEmpty switch
					{
						true => (object)UI.MkGreenLabel("normalized"),
						false => GitEnumUtils.IsNormEnabled(sln.Norm.IsEmpty, git.FileState, git.SyncState) switch
						{
							true => (Control)new Button("Normalize", _ => sln.Norm.Apply()),
							false => UI.MkRedLabel(GitEnumUtils.FmtNorm(sln.Norm.IsEmpty, git.FileState, git.SyncState)),
						}
					},
				},
				(sln.Nfo.Name == "GitTest") switch
				{
					false => null!,
					true => new Button("Delete Repo", _ => ApiGithub.DeleteRepoAndGitFolder(sln.Nfo.Name, sln.Folder))
				}
			)
		},
		new
		{
			Release = releaseNfo.CanRelease switch
			{
				true => (Control)new Button($"Release {releaseNfo.ReleaseVersion}", _ => { ReleaseLogic.ReleaseSln(sln, releaseNfo.ReleaseVersion, dryRun.V); refresh(); }),
				false => UI.MkFlagLabel(releaseNfo.ErrorMessage!, releaseNfo.ErrorMessage == "no changes"),
			},
			UpdateOthers = PkgRefUpdater.DoesPkgNeedUpdating(sln, allSlnsFun()) switch
			{
				false => (Control)UI.MkGreenLabel("others are up to date"),
				true => new Button("Update others", _ => PkgRefUpdater.UpdateOthers(sln, allSlnsFun()))
			}
		}
	);
}




static class Css
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
		div:has(> fieldset) {
			display: flex;
		}
		
		fieldset {
			border: 1pt solid #797979ba;
			border-radius: 5px;
			background-color: #323232;
			box-shadow: 0px 0px 4px 2px #2662ad57;
		}
		"""
	);
}

static class EnumUtils
{
	public static string FirstOr(this IEnumerable<string> source, string orVal)
	{
		var val = source.FirstOrDefault();
		return val switch
		{
			not null => val,
			null => orVal
		};
	}
}


static class UtilExtra
{
	public static object MergeOpt(params object?[] arr) => Util.Merge(arr.Where(e => e != null).ToArray());
}

static class DisplayUtils
{
	public static DumpContainer GetInTask(Func<string> fun)
	{
		var dc = new DumpContainer("_");
		Task.Run(() => dc.UpdateContent(fun()));
		return dc;
	}
	
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
	public const string RedCol = "#f55384";
	private const string GreenCol = "#51f086";
	private const string AccentCol = "#5192ed";
	
	public static Span Ver(this string ver) => new Span(ver).Set("font-weight", "bold");
	
	public static Div DivVert(params Control[] ctrls) => new Div(ctrls)
		.Set("display", "flex")
		.Set("flex-direction", "column")
		.Set("align-items", "center")
		.Set("row-gap", "10px");
	
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
	public static IRoVar<bool> WhenVarSome<T>(this IRoVar<MaybeErr<T>> v) =>
		v.SelectVar(e => e.IsSome());

	public static (Action<Sln>, IObservable<Sln>) MakeRefreshEvent()
	{
		ISubject<Sln> whenRefreshSubj = new Subject<Sln>().D(mainD);
		return (
			sln => whenRefreshSubj.OnNext(sln),
			whenRefreshSubj.AsObservable()
		);
	}
}
