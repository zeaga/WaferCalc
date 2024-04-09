using Number = float;
using MathN = System.MathF;
using System.Diagnostics;
using System.Reflection;

namespace Wafer;
internal class Engine {

	static readonly string defaultFile = Assembly.GetEntryAssembly( )?.GetName( ).Name + ".conf";
	static readonly string defaultDirectory = Path.GetDirectoryName( Assembly.GetEntryAssembly( )?.Location ) ?? "";
	static readonly string defaultPath = Path.Combine( defaultDirectory, defaultFile );
	const string defaultScript = """
sqrt:   0.5 **
pi:     3.14159265358979323846
e:      2.7182818284590452354
sqrt2:  1.41421356237309504880

log:    ln swap ln /

over:   swap dup rot swap

2push:  push push
2pop:   pop pop

2swap:  rot push rot pop
2dup:   swap dup rot dup rot swap
2over:  2swap 2dup 2push 2swap 2pop
2drop:  drop drop
""";

	public enum ValueType {
		Number,
		String,
	}

	public enum WordType {
		Builtin,
		Defined,
	}

	public readonly struct Value {
		public readonly Number num;
		public readonly string str;
		public readonly ValueType type;

		public Value( Number value ) {
			num = value;
			str = value.ToString( );
			type = ValueType.Number;
		}

		public Value( string value ) {
			num = value.Length;
			str = value;
			type = ValueType.String;
		}

		public override string? ToString( ) => str;
	}

	private readonly struct Word {
		public readonly string name;
		public readonly Action<Engine>? action;
		public readonly string? str;
		public readonly WordType type;

		public Word( string name, Action<Engine> definition ) {
			this.name = name;
			action = definition;
			type = WordType.Builtin;
		}

		public Word( string name, string definition ) {
			this.name = name;
			str = definition;
			type = WordType.Defined;
		}

		public bool Fire( Engine engine ) {
			if ( action is not null ) {
				try {
					action( engine );
					return true;
				} catch ( Exception ) {
					return false;
				}
			} else {
				return engine.Process( str ?? "", true );
			}
		}
	}

	struct State {
		public Stack<Value> Stack;
		public Stack<Value> Stash;
		public Dictionary<string, Word> Words;
	}

	private const int HistoryCapacity = 50;
	private readonly Queue<State> History = new( );

	private Stack<Value> Stack => History.Peek( ).Stack;
	private Stack<Value> Stash => History.Peek( ).Stash;
	private Dictionary<string, Word> Words => History.Peek( ).Words;

	private void AddWord( string name, Action<Engine> action ) => Words[name] = new Word( name, action );
	private void AddWord( string name, string definition ) => Words[name] = new Word( name, definition );

	private static void Print( ) => Console.WriteLine( );
	private static void Print( object obj ) => Console.WriteLine( obj.ToString( ) );

	public void Push( Number value ) => Stack.Push( new Value( value ) );
	public void Push( string value ) => Stack.Push( new Value( value ) );
	public void Push( Value value ) => Stack.Push( value );
	public void Push( bool value ) => Push( value ? 1 : 0 );

	public Number Pop( ) => Stack.Pop( ).num;
	public string PopS( ) => Stack.Pop( ).str;
	public Value PopV( ) => Stack.Pop( );
	public bool PopB( ) => Stack.Pop( ).num != 0;
	public int PopI( ) => (int)Stack.Pop( ).num;

	private void HistoryStep( ) {
		State next;
		if ( History.Count == 0 ) {
			next.Stack = new( );
			next.Stash = new( );
			next.Words = [];
			DefaultWords( );
		} else {
			next.Stack = Stack.FastClone( );
			next.Stash = Stash.FastClone( );
			next.Words = new Dictionary<string, Word>( Words );

		}
		History.Enqueue( next );
		if ( History.Count > HistoryCapacity )
			History.Dequeue( );
	}

	public void HistoryUndo( ) {
		State next;
		if ( History.Count == 0 ) {
			next.Stack = new( );
			next.Stash = new( );
			next.Words = [];
			DefaultWords( );
		}
		History.Dequeue( );
	}

	public void LoadDefaultScript( ) {
		if ( !File.Exists( defaultPath ) ) File.WriteAllText( defaultPath, defaultScript );
		LoadWordsFromFile( defaultPath );
	}

	public void LoadWordsFromFile( string path ) {
		foreach ( string line in File.ReadLines( path ) ) Process( line, true );
	}

	public void DefaultWords( ) {
		AddWord( "help", e => {
			var keys = Words.Keys;
			var builtins = keys.Where( k => Words[k].type == WordType.Builtin ).ToList( );
			Console.WriteLine( $"BUILTINS: {string.Join( ", ", builtins )}" );
			Console.WriteLine( $"DEFINED:" );
			var defines = keys.Where( k => Words[k].type == WordType.Defined ).ToList( );
			//defines.Sort( );
			foreach ( string name in defines ) {
				Console.WriteLine( $"\t{name}: {Words[name].str}" );
			}
		} );
		AddWord( "exit", e => Environment.Exit( 0 ) );
		AddWord( ".", e => Print( Pop( ) ) );
		AddWord( "CR", e => Print( ) );
		AddWord( "+", e => Push( Pop( ) + Pop( ) ) );
		AddWord( "-", e => Push( -Pop( ) + Pop( ) ) );
		AddWord( "*", e => Push( Pop( ) * Pop( ) ) );
		AddWord( "/", e => {
			var a = Pop( );
			Push( Pop( ) / a );
		} );
		AddWord( "//", e => {
			var a = Pop( );
			Push( (int)( Pop( ) / a ) );
		} );
		AddWord( "%", e => {
			var a = Pop( );
			Push( Pop( ) % a );
		} );
		AddWord( "**", e => {
			var a = Pop( );
			Push( MathN.Pow( Pop( ), a ) );
		} );
		AddWord( "ln", e => Push( MathN.Log( Pop( ) ) ) );
		AddWord( "log10", e => Push( MathN.Log10( Pop( ) ) ) );
		AddWord( "log2", e => Push( MathN.Log2( Pop( ) ) ) );
		AddWord( "floor", e => Push( MathN.Floor( Pop( ) ) ) );
		AddWord( "ceil", e => Push( MathN.Ceiling( Pop( ) ) ) );
		AddWord( "ceil", e => Push( MathN.Ceiling( Pop( ) ) ) );

		AddWord( "not", e => Push( !PopB( ) ) );
		AddWord( "or", e => {
			var a = PopB( );
			var b = PopB( );
			Push( a || b );
		} );
		AddWord( "and", e => {
			var a = PopB( );
			var b = PopB( );
			Push( a && b );
		} );
		AddWord( "xor", e => Push( PopB( ) != PopB( ) ) );
		AddWord( "~", e => Push( ~PopI( ) ) );
		AddWord( "|", e => Push( PopI( ) | PopI( ) ) );
		AddWord( "&", e => Push( PopI( ) & PopI( ) ) );
		AddWord( "^", e => Push( PopI( ) ^ PopI( ) ) );
		AddWord( "empty", e => Stack.Clear( ) );
		AddWord( "count", e => Push( Stack.Count ) );
		AddWord( "dup", e => Push( Stack.Peek( ).num ) );
		AddWord( "drop", e => Pop( ) );
		AddWord( "swap", e => {
			var a = Pop( );
			var b = Pop( );
			Push( a );
			Push( b );
		} );
		AddWord( "rot", e => {
			var a = Pop( );
			var b = Pop( );
			var c = Pop( );
			Push( b );
			Push( a );
			Push( c );
		} );
		AddWord( "sin", e => Push( MathN.Sin( Pop( ) ) ) );
		AddWord( "cos", e => Push( MathN.Cos( Pop( ) ) ) );
		AddWord( "tan", e => Push( MathN.Tan( Pop( ) ) ) );
		AddWord( ">", e => Push( !( Pop( ) <= Pop( ) ) ) );
		AddWord( ">=", e => Push( !( Pop( ) < Pop( ) ) ) );
		AddWord( "==", e => Push( Pop( ) == Pop( ) ) );
		AddWord( "!=", e => Push( Pop( ) != Pop( ) ) );
		AddWord( "<", e => Push( Pop( ) >= Pop( ) ) );
		AddWord( "<=", e => Push( Pop( ) > Pop( ) ) );
		AddWord( "cls", e => Console.Clear( ) );
		AddWord( "push", e => Stash.Push( Stack.Pop( ) ) );
		AddWord( "pop", e => Stack.Push( Stash.Pop( ) ) );
		AddWord( "reload", e => LoadDefaultScript( ) );
		AddWord( "default", e => {
			File.WriteAllText( defaultPath, defaultScript );
			LoadDefaultScript( );
		} );
		AddWord( "conf", e => {
			using Process fileopener = new( );
			fileopener.StartInfo.FileName = defaultPath;
			fileopener.StartInfo.UseShellExecute = true;
			fileopener.Start( );
		} );
	}

	public Engine( ) {
		DefaultWords( );
		HistoryStep( );
	}

	public void PrintStack( ) {
		if ( Stack.Count > 0 )
			Console.WriteLine( string.Join( ' ', Stack.Reverse( ) ) );
	}

	public bool SafeProcess( string input ) {
		HistoryStep( );
		if ( Process( input ) )
			return true;
		HistoryUndo( );
		return false;
	}

	private bool Process( string input, bool subroutine = false ) {
		if ( input == null ) {
			PrintStack( );
			return true;
		}

		// Format input and split into array
		var wordstr = input.Trim( ).ToLower( );
		var cmtidx = wordstr.IndexOf( '#' );
		if ( cmtidx != -1 )
			wordstr = wordstr[..cmtidx];
		var words = wordstr.Split( );

		for ( int i = 0; i < words.Length; i++ ) {
			string here = words[i];
			if ( here.Length == 0 ) continue;

			// Define word
			if ( i == 0 && here[^1] == ':' ) {
				string name = here[..^1];
				string definition = string.Join( ' ', words.Skip( 1 ) );
				AddWord( name, definition );
				break;
			}

			// If word, fire and continue
			if ( Words.TryGetValue( here, out Word word ) ) {
				return word.Fire( this );
			}

			if ( here[0] == '*' ) {
				
			}

			// If number, push to stack and continue
			if ( Number.TryParse( here, out Number result ) ) {
				Stack.Push( new Value( result ) );
				continue;
			}

			// We don't recognize this word
			Console.WriteLine( "Unknown word: " + here );
			return false;

		}
		if ( !subroutine ) PrintStack( );
		return true;
	}

}