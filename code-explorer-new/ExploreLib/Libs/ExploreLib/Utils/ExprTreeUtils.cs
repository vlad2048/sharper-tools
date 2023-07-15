using System.Linq.Expressions;

namespace ExploreLib.Utils;

public static class ExprTreeUtils
{
	public static string GetName<T>(Expression<Func<T, object>> expr)
	{
		var lambda = (LambdaExpression)expr;
		var p = lambda.Parameters.Single();

		var visitor = new NameVisitor(p);
		visitor.Visit(expr);

		if (visitor.Name == null) throw new ArgumentException("Expression name not found");

		return visitor.Name;
	}

	public static Func<T, object> CompileGetter<T>(Expression<Func<T, object>> expr) => expr.Compile();


	private class NameVisitor : ExpressionVisitor
	{
		private readonly ParameterExpression p;

		public string? Name { get; private set; }

		public NameVisitor(ParameterExpression p)
		{
			this.p = p;
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			if (node.Expression == p)
			{
				Name ??= node.Member.Name;
			}
			return base.VisitMember(node);
		}
	}
}