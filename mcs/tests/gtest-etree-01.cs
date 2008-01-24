using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

public struct InverseLogicalOperator
{
	bool value;
	public InverseLogicalOperator (bool value)
	{
		this.value = value;
	}

	public static bool operator true (InverseLogicalOperator u)
	{
		return u.value;
	}

	public static bool operator false (InverseLogicalOperator u)
	{
		return u.value;
	}
}

/*
public struct MyTypeMyType<T>
{
	T value;

	public MyType (T value)
	{
		this.value = value;
	}

	public static implicit operator T (MyType<T> o)
	{
		return o.value;
	}
}
*/

class Tester
{
	static void AssertNodeType (LambdaExpression e, ExpressionType et)
	{
		if (e.Body.NodeType != et)
			throw new ApplicationException (e.Body.NodeType + " != " + et);
	}

	static void Assert<T> (T expected, T value)
	{
		if (!EqualityComparer<T>.Default.Equals (expected, value))
			throw new ApplicationException (expected + " != " + value);
	}

	static void Assert<T> (T[] expected, T[] value)
	{
		if (expected.Length != value.Length)
			throw new ApplicationException ("Array length does not match " + expected.Length + " != " + value.Length);

		for (int i = 0; i < expected.Length; ++i) {
			if (!EqualityComparer<T>.Default.Equals (expected [i], value [i]))
				throw new ApplicationException ("Index " + i + ": " + expected [i] + " != " + value [i]);
		}
	}

	void AddTest ()
	{
		Expression<Func<int, int, int>> e = (int a, int b) => a + b;
		AssertNodeType (e, ExpressionType.Add);
		Assert (50, e.Compile ().Invoke (20, 30));

		Expression<Func<int?, int?, int?>> e2 = (a, b) => a + b;
		AssertNodeType (e2, ExpressionType.Add);
		Assert (null, e2.Compile ().Invoke (null, 3));
	}

	void AddCheckedTest ()
	{
		checked {
			Expression<Func<int, int, int>> e = (int a, int b) => a + b;

			AssertNodeType (e, ExpressionType.AddChecked);
			Assert (-10, e.Compile ().Invoke (20, -30));
		}
	}

	void AndTest ()
	{
		Expression<Func<bool, bool, bool>> e = (bool a, bool b) => a & b;

		AssertNodeType (e, ExpressionType.And);
		Func<bool,bool,bool> c = e.Compile ();
		
		Assert (true,  c (true, true));
		Assert (false, c (true, false));
		Assert (false, c (false, true));
		Assert (false, c (false, false));
	}
	
	void AndNullableTest ()
	{
		Expression<Func<bool?, bool?, bool?>> e = (bool? a, bool? b) => a & b;

		AssertNodeType (e, ExpressionType.And);
		Func<bool?,bool?,bool?> c = e.Compile ();
		
		Assert (true,  c (true, true));
		Assert (false, c (true, false));
		Assert (false, c (false, true));
		Assert (false, c (false, false));

		Assert (null,  c (true, null));
		Assert (false, c (false, null));
		Assert (false, c (null, false));
		Assert (null,  c (true, null));
		Assert (null, c (null, null));
	}
	
	void AndAlsoTest ()
	{
		Expression<Func<bool, bool, bool>> e = (bool a, bool b) => a && b;

		AssertNodeType (e, ExpressionType.AndAlso);
		Assert (false, e.Compile ().Invoke (true, false));
	}

	void ArrayIndexTest ()
	{
		Expression<Func<string[], long, string>> e = (string[] a, long i) => a [i];
		AssertNodeType (e, ExpressionType.ArrayIndex);
		Assert ("b", e.Compile ().Invoke (new string [] { "a", "b", "c" }, 1));

		Expression<Func<string [], string>> e2 = (string [] a) => a [0];
		AssertNodeType (e2, ExpressionType.ArrayIndex);
		Assert ("a", e2.Compile ().Invoke (new string [] { "a", "b" }));

		Expression<Func<object [,], int, int, object>> e3 = (object [,] a, int i, int j) => a [i, j];
		AssertNodeType (e3, ExpressionType.Call);
		
		Assert ("z", e3.Compile ().Invoke (
			new object [,] { { 1, 2 }, { "x", "z" } }, 1, 1));

		Expression<Func<decimal [][], byte, decimal>> e4 = (decimal [][] a, byte b) => a [b][1];
		AssertNodeType (e4, ExpressionType.ArrayIndex);

		decimal [] [] array = { new decimal [] { 1, 9 }, new decimal [] { 10, 90 } };
		Assert (90, e4.Compile ().Invoke (array, 1));
	}
	
	void ArrayLengthTest ()
	{
		int o = new int [0].Length;

		Expression<Func<double [], int>> e = (double [] a) => a.Length;
		AssertNodeType (e, ExpressionType.ArrayLength);
		Assert (0, e.Compile ().Invoke (new double [0]));
		Assert (9, e.Compile ().Invoke (new double [9]));

		// TODO: implement
		//Expression<Func<string [,], int>> e2 = (string [,] a) => a.Length;
		//AssertNodeType (e2, ExpressionType.MemberAccess);
		//Assert (0, e2.Compile ().Invoke (new string [0, 0]));
	}	
	
	void CallTest ()
	{
		Expression<Func<int, int>> e = (int a) => Math.Max (a, 5);
		AssertNodeType (e, ExpressionType.Call);
		Assert (5, e.Compile ().Invoke (2));
		Assert (9, e.Compile ().Invoke (9));
		
		Expression<Func<string, string>> e2 = (string a) => InstanceMethod (a);
		AssertNodeType (e2, ExpressionType.Call);
		Assert ("abc", e2.Compile ().Invoke ("abc"));

		Expression<Func<int, string, int, object>> e3 = (int index, string a, int b) => InstanceParamsMethod (index, a, b);
		AssertNodeType (e3, ExpressionType.Call);
		Assert<object> (4, e3.Compile ().Invoke (1, "a", 4));

		Expression<Func<object>> e4 = () => InstanceParamsMethod (0);
		AssertNodeType (e4, ExpressionType.Call);
		Assert<object> ("<empty>", e4.Compile ().Invoke ());

		Expression<Func<int, int>> e5 = (int a) => GenericMethod (a);
		AssertNodeType (e5, ExpressionType.Call);
		Assert (5, e5.Compile ().Invoke (5));
	}

	void CoalesceTest ()
	{
		Expression<Func<uint?, uint>> e = (uint? a) => a ?? 99;
		AssertNodeType (e, ExpressionType.Coalesce);
		var r = e.Compile ();
		Assert ((uint)5, r.Invoke (5));
		Assert ((uint)99, r.Invoke (null));

// TODO: implement
//		Expression<Func<MyType<bool>?, bool>> e2 = (MyType<bool>? a) => a ?? false;
//		AssertNodeType (e2, ExpressionType.Coalesce);
//		var r2 = e2.Compile ();
//		Assert (true, r2.Invoke (new MyType<bool> (true)));
//		Assert (false, r2.Invoke (null));
	}

	void ConditionTest ()
	{
		Expression<Func<bool, byte, int, int>> e = (bool a, byte b, int c) => (a ? b : c);
		AssertNodeType (e, ExpressionType.Conditional);
		var r = e.Compile ();
		Assert (3, r.Invoke (true, 3, 999999));
		Assert (999999, r.Invoke (false, 3, 999999));

		Expression<Func<int, decimal, decimal?>> e2 = (int a, decimal d) => (a > 0 ? d : a < 0 ? -d : (decimal?)null);
		AssertNodeType (e2, ExpressionType.Conditional);
		var r2 = e2.Compile ();
		Assert (null, r2.Invoke (0, 10));
		Assert (50, r2.Invoke (1, 50));
		Assert (30, r2.Invoke (-7, -30));

		Expression<Func<bool?, int?>> e3 = (bool? a) => ((bool)a ? 3 : -2);
		AssertNodeType (e3, ExpressionType.Convert);
		var r3 = e3.Compile ();
		Assert (3, r3.Invoke (true));
		Assert (-2, r3.Invoke (false));

		Expression<Func<InverseLogicalOperator, byte, byte, byte>> e4 = (InverseLogicalOperator a, byte b, byte c) => (a ? b : c);
		AssertNodeType (e4, ExpressionType.Conditional);
		var r4 = e4.Compile ();
		Assert (3, r4.Invoke (new InverseLogicalOperator (true), 3, 4));
		Assert (4, r4.Invoke (new InverseLogicalOperator (false), 3, 4));
	}

	void NewArrayInitTest ()
	{
		Expression<Func<int[]>> e = () => new int[0];
		AssertNodeType (e, ExpressionType.NewArrayInit);
		Assert (new int[0], e.Compile ().Invoke ());
		
		e = () => new int [] {};
		AssertNodeType (e, ExpressionType.NewArrayInit);
		Assert (new int [0], e.Compile ().Invoke ());		

		Expression<Func<ushort, ulong? []>> e2 = (ushort a) => new ulong? [] { a };
		AssertNodeType (e2, ExpressionType.NewArrayInit);
		Assert (new ulong? [1] { ushort.MaxValue }, e2.Compile ().Invoke (ushort.MaxValue));

		Expression<Func<char [] []>> e3 = () => new char [] [] { new char [] { 'a' } };
		AssertNodeType (e3, ExpressionType.NewArrayInit); 
		Assert (new char[] { 'a' }, e3.Compile ().Invoke ()[0]);
	}

	void OrTest ()
	{
		Expression<Func<bool, bool, bool>> e = (bool a, bool b) => a | b;

		AssertNodeType (e, ExpressionType.Or);
		Func<bool, bool, bool> c = e.Compile ();

		Assert (true, c (true, true));
		Assert (true, c (true, false));
		Assert (true, c (false, true));
		Assert (false, c (false, false));
	}

	void OrNullableTest ()
	{
		Expression<Func<bool?, bool?, bool?>> e = (bool? a, bool? b) => a | b;

		AssertNodeType (e, ExpressionType.Or);
		Func<bool?, bool?, bool?> c = e.Compile ();

		Assert (true, c (true, true));
		Assert (true, c (true, false));
		Assert (true, c (false, true));
		Assert (false, c (false, false));

		Assert (true, c (true, null));
		Assert (null, c (false, null));
		Assert (null, c (null, false));
		Assert (true, c (true, null));
		Assert (null, c (null, null));
	}
	
	//
	// Test helpers
	//
	string InstanceMethod (string arg)
	{
		return arg;
	}
	
	object InstanceParamsMethod (int index, params object[] args)
	{
		if (args == null)
			return "<null>";
		if (args.Length == 0)
			return "<empty>";
		return args [index];
	}
	
	T GenericMethod<T> (T t)
	{
		return t;
	}	


	public static int Main ()
	{
		Tester e = new Tester ();
		e.AddTest ();
		e.AndNullableTest ();
		e.AddCheckedTest ();
		e.AndTest ();
		e.AndAlsoTest ();
		e.ArrayIndexTest ();
		e.ArrayLengthTest ();
		e.CallTest ();
		e.CoalesceTest ();
		e.ConditionTest ();
		e.NewArrayInitTest ();
		e.OrTest ();
		e.OrNullableTest ();
		
		return 0;
	}
}




