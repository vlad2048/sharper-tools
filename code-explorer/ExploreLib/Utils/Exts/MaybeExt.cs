using System.Reactive;
using System.Reactive.Linq;
using PowMaybe;
using PowRxVar;

namespace ExploreLib.Utils.Exts;

static class MaybeExt
{
	public static IObservable<T> WhenSome<T>(this IObservable<Maybe<T>> obs) => obs.Where(e => e.IsSome()).Select(e => e.Ensure());
	public static IObservable<Unit> WhenNone<T>(this IObservable<Maybe<T>> obs) => obs.Where(e => e.IsNone()).ToUnit();
}