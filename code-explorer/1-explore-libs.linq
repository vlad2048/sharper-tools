<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>LINQPadExtras.Utils</Namespace>
  <Namespace>LINQPadExtras.Utils.Exts</Namespace>
  <Namespace>NuGet.Packaging.Core</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

#load ".\libs\common"
#load ".\libs\rx-dbg"
#load ".\libs\api-nuget"
#load ".\libs\todump"


void Main()
{
	Css.Init();
	V.Init(true);
	var d = new Disp();
	
	var selLibs = V.Make(Array.Empty<Lib>(), "selLibs").D(d);
	
	LibExplorer(selLibs).D(d).Dump();
}

internal enum Ord
{
	Name,
	Date,
	Size,
	Framework
}

(object, IDisposable) LibExplorer(IRwVar<Lib[]> selLibs)
{
	var d = new Disp();

	var libs = ApiNuget.ListLibs();
	
	var libSearchText = V.Make(string.Empty, "libSearchText").D(d);
	var ord = V.Make(Ord.Name, "ord").D(d);
	var hideMicrosoft = V.Make(true, "hideMicrosoft").D(d);

	var focusLib = V.Make(May.None<Lib>(), "focusLib").D(d);
	var curSelLibs = V.Make(Array.Empty<Lib>(), "curSelLibs").D(d);
	var focusDepLibs = Var.Expr(() => focusLib.V.FindDeps(libs));
	
	
	var ui =
		new StackPanel(false,
			new StackPanel(true,
				Html.TextBox("", libSearchText),
				Html.SelectBox(ord),
				Html.CheckBox("hide Microsoft", hideMicrosoft),
				new Button("Clear", _ => curSelLibs.V = Array.Empty<Lib>()),
				new Button("View", _ =>
				{
					selLibs.V = curSelLibs.V;
				})
			),
			
			Observable.Merge(
				libSearchText.ToUnit(),
				ord.ToUnit(),
				hideMicrosoft.ToUnit(),
				focusLib.ToUnit(),
				focusDepLibs.ToUnit()
			).Display(_ =>
				libs
					.OrderAccordingTo(ord.V)
					.Where(e => hideMicrosoft.V switch
					{
						false => true,
						true => !e.Name.StartsWith("microsoft", StringComparison.InvariantCultureIgnoreCase),
					})
					.Where(lib => StrUtils.IsMatch(lib.Name, libSearchText.V))
					.Select(lib =>
						new
						{
							Include = new CheckBox(onClick: cb => {
								//void LL(string msg) => $"[{curSelLibs.V.Length}]: {msg}".Dump();
								if (cb.Checked)
									curSelLibs.V = curSelLibs.V.Append(lib).Distinct().ToArray();
								else
									curSelLibs.V = curSelLibs.V.Where(e => e != lib).Distinct().ToArray();
							}),
							Package = new Hyperlink(lib.Name, _ => focusLib.V = May.Some(lib)).SetBackColorIf(focusDepLibs.V.Contains(lib), "#141e52"),
							Version = lib.Version,
							Framework = lib.Framework,
							Size = lib.DllFileSize.FmtSize(),
							
							/*Dif = Util.Dif($"{lib.Framework.Framework}", lib.Framework.GetShortFolderName()),
							FwPlat = lib.Framework.PlatformVersion,
							FwVer = lib.Framework.Version,
							GetShortFolderName = lib.Framework.GetShortFolderName(),
							lib.Framework.IsSpecificFramework,
							lib.Framework.DotNetFrameworkName,
							lib.Framework.AllFrameworkVersions,
							lib.Framework.IsAny,
							lib.Framework.IsAgnostic,
							lib.Framework.IsUnsupported,
							lib.Framework.IsPCL,
							lib.Framework.IsPackageBased,
							lib.Framework.HasPlatform,
							lib.Framework.HasProfile,
							lib.Framework.Platform,
							lib.Framework.Profile,*/
						
							/*Pkg = lib.Pkg.WithClick(() =>
							{
								selLib.V = May.Some(lib);
							}).SetBackIf(selDepLibs.V.Contains(lib), "#141e52"),
							Framework = lib.Framework,
							DllFileSize = lib.DllFileSize.FmtSize(),*/
						}
			)).D(d)
		);
	
	return (ui, d);
}




static class LibsUtils
{
	public static Lib[] FindDeps(this Maybe<Lib> mayLib, Lib[] libs) => mayLib.IsSome(out var lib) switch
	{
		true => lib.FindDeps(libs),
		false => Array.Empty<Lib>()
	};
	
	
	private static Lib[] FindDeps(this Lib lib, Lib[] libs)
	{
		var list = new List<Lib>();
		void Recurse(Lib l)
		{
			list.Add(l);
			foreach (var dep in l.Deps)
			{
				var libDep = libs.FirstOrDefault(e => string.Compare(e.Name, dep.Id, StringComparison.InvariantCultureIgnoreCase) == 0);
				if (libDep != null)
					Recurse(libDep);
			}
		}
		Recurse(lib);
		return list.ToArray();
	}
}



static class DisplayUtils
{
	public static Div WithClick(this object obj, Action click)
	{
		var dc = new DumpContainer(obj);
		var div = new Div(dc);
		div.Click += (_, _) => click();
		return div;
	}
	
	public static DumpContainer SetBackIf(this object obj, bool cond, string col)
	{
		var dc = new DumpContainer(obj);
		if (cond)
			dc.SetBackColor(col);
		return dc;
	}
	
	public static DumpContainer GetInTask(Func<string> fun)
	{
		var dc = new DumpContainer("_");
		Task.Run(() => dc.UpdateContent(fun()));
		return dc;
	}
	
	public static DumpContainer ShowWhen(this object obj, IRoVar<bool> predicate)
	{
		var dc = new DumpContainer(obj);
		predicate.Subscribe(e => dc.Style = e ? "" : "display:none").D(predicate);
		return dc;
	}
}

static class MiscUtils
{
	public static IEnumerable<Lib> OrderAccordingTo(this IEnumerable<Lib> source, Ord ord) => ord switch
	{
		Ord.Name => source.OrderBy(e => e.Name),
		Ord.Date => source.OrderByDescending(e => e.Time),
		Ord.Size => source.OrderByDescending(e => e.DllFileSize),
		Ord.Framework => source.OrderByDescending(e => e.Framework.Version),
		_ => throw new ArgumentException()
	};
	
	public static IEnumerable<T> OrderByDescendingIf<T, K>(this IEnumerable<T> source, bool cond, Func<T, K> fun) => cond switch
	{
		false => source,
		true => source.OrderByDescending(fun)
	};
}

