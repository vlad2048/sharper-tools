using System.Reactive.Disposables;
using PowRxVar;

namespace ExploreLib.Utils;

public static class RxExt
{
	public static IDisposable PipeTo<T>(this IObservable<T> src, IRwVar<T>? dst)
	{
		if (dst == null)
			return Disposable.Empty;
		var d = new Disp();
		src.Subscribe(e => dst.V = e).D(d);
		return d;
	}
}