<Query Kind="Program">
  <Namespace>System.Dynamic</Namespace>
  <Namespace>System.ComponentModel</Namespace>
</Query>

void Main()
{
	var rec = new Rec("Vlad", 123);
	var an = new
	{
		Key = "Key"
	};

	var res = TypeUtils.Combine(an, rec);

	new DumpContainer(res).Dump();
}

record Rec(string Name, int Val);


public static class TypeUtils
{
	public static dynamic Combine(object item1, object item2, params string[] exceptions)
	{
		var dictionary1 = (IDictionary<string, object>)item1.ToDynamic()!;
		var dictionary2 = (IDictionary<string, object>)item2.ToDynamic()!;
		var result = new ExpandoObject();
		var d = result as IDictionary<string, object>; //work with the Expando as a Dictionary

		foreach (var pair in dictionary1.Concat(dictionary2))
		{
			if (!exceptions.Contains(pair.Key))
				d[pair.Key] = pair.Value;
		}

		return result;
	}

	private static dynamic? ToDynamic(this object value)
	{
		IDictionary<string, object> expando = new ExpandoObject()!;

		foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(value.GetType()))
			expando.Add(property.Name, property.GetValue(value)!);

		return expando as ExpandoObject;
	}
}