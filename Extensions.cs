namespace Wafer;

public static class Extensions {
	public static Stack<T> FastClone<T>( this Stack<T> original ) {
		var arr = new T[original.Count];
		original.CopyTo( arr, 0 );
		Array.Reverse( arr );
		return new Stack<T>( arr );
	}
}
