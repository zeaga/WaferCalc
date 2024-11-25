using Number = float;
using MathN = System.MathF;
using System.Diagnostics;
using System.Reflection;

namespace Wafer;
internal class Engine {

	static readonly string defaultFile = Assembly.GetEntryAssembly()?.GetName().Name + ".conf";
	static readonly string defaultDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? "";
	static readonly string defaultPath = Path.Combine(defaultDirectory, defaultFile);
	const string defaultScript = """
sqrt:   0.5 **
pi:     3.14159265358979323846
e:      2.7182818284590452354
sqrt2:  1.41421356237309504880

log:    ln swap ln /

over:   swap dup rot swap

2push:  swap push push
2pop:   pop pop swap

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

		public Value(Number value) {
			num = value;
			str = value.ToString();
			type = ValueType.Number;
		}

		public Value(string value) {
			num = value.Length;
			str = value;
			type = ValueType.String;
		}

		public override string? ToString() => str;
	}

	private readonly struct Word {
		public readonly string name;
		public readonly Action<Engine>? action;
		public readonly string? str;
		public readonly WordType type;

		public Word(string name, Action<Engine> definition) {
			this.name = name;
			action = definition;
			type = WordType.Builtin;
		}

		public Word(string name, string definition) {
			this.name = name;
			str = definition;
			type = WordType.Defined;
		}

		public Error Fire(Engine engine) {
			if (action is not null) {
				try {
					action(engine);
					return Error.None;
				} catch (Exception) {
					return Error.StackFault;
				}
			} else {
				return engine.Process(str ?? "", true);
			}
		}
	}

	struct State {
		public Stack<Value> Stack;
		public Stack<Value> Stash;
		public Dictionary<string, Word> Words;
	}

	public enum Error {
		Unknown = -1,
		None,
		UnknownWord,
		StackFault,
		EndOfInput,
		NoSuchFile,
	}

	private Stack<Value> Stack = new();
	private Stack<Value> Stash = new();
	private Dictionary<string, Word> Words = [];

	private void AddWord(string name, Action<Engine> action) => Words[name] = new Word(name, action);
	private void AddWord(string name, string definition) => Words[name] = new Word(name, definition);

	private static void Print() => Console.WriteLine();
	private static void Print(object obj) => Console.WriteLine(obj.ToString());

	public void Push(Number value) => Stack.Push(new Value(value));
	public void Push(string value) => Stack.Push(new Value(value));
	public void Push(Value value) => Stack.Push(value);
	public void Push(bool value) => Push(value ? 1 : 0);

	public Number Pop() => Stack.Pop().num;
	public string PopS() => Stack.Pop().str;
	public Value PopV() => Stack.Pop();
	public bool PopB() => Stack.Pop().num != 0;
	public int PopI() => (int)Stack.Pop().num;

	public void LoadDefaultScript() {
		if (!File.Exists(defaultPath)) File.WriteAllText(defaultPath, defaultScript);
		LoadWordsFromFile(defaultPath);
	}

	public void LoadWordsFromFile(string path) {
		foreach (string line in File.ReadLines(path)) SafeProcess(line, true);
	}

	public Error LoadScript(string path) {
		if (!File.Exists(path)) {
			return Error.NoSuchFile;
		}
		string contents = File.ReadAllText(path);
		SafeProcess(contents, true);
		return Error.None;
	}

	public void DefaultWords() {
		AddWord("help", e => {
			var keys = Words.Keys;
			var builtins = keys.Where(k => Words[k].type == WordType.Builtin).ToList();
			Print($"BUILTINS: {string.Join(", ", builtins)}");
			Print($"DEFINED:");
			var defines = keys.Where(k => Words[k].type == WordType.Defined).ToList();
			//defines.Sort( );
			foreach (string name in defines) {
				Print($"\t{name}: {Words[name].str}");
			}
		});
		AddWord("exit", e => Environment.Exit(0));
		AddWord(".", e => Print(Pop()));
		AddWord("cr", e => Print());
		AddWord("+", e => Push(Pop() + Pop()));
		AddWord("-", e => Push(-Pop() + Pop()));
		AddWord("*", e => Push(Pop() * Pop()));
		AddWord("/", e => {
			var a = Pop();
			Push(Pop() / a);
		});
		AddWord("//", e => {
			var a = Pop();
			Push((int)(Pop() / a));
		});
		AddWord("%", e => {
			var a = Pop();
			Push(Pop() % a);
		});
		AddWord("**", e => {
			var a = Pop();
			Push(MathN.Pow(Pop(), a));
		});
		AddWord("ln", e => Push(MathN.Log(Pop())));
		AddWord("log10", e => Push(MathN.Log10(Pop())));
		AddWord("log2", e => Push(MathN.Log2(Pop())));
		AddWord("floor", e => Push(MathN.Floor(Pop())));
		AddWord("ceil", e => Push(MathN.Ceiling(Pop())));

		AddWord("not", e => Push(!PopB()));
		AddWord("or", e => {
			var a = PopB();
			var b = PopB();
			Push(a || b);
		});
		AddWord("and", e => {
			var a = PopB();
			var b = PopB();
			Push(a && b);
		});
		AddWord("xor", e => Push(PopB() != PopB()));
		AddWord("~", e => Push(~PopI()));
		AddWord("|", e => Push(PopI() | PopI()));
		AddWord("&", e => Push(PopI() & PopI()));
		AddWord("^", e => Push(PopI() ^ PopI()));
		AddWord("empty", e => Stack.Clear());
		AddWord("count", e => Push(Stack.Count));
		AddWord("dup", e => Push(Stack.Peek().num));
		AddWord("drop", e => Pop());
		AddWord("swap", e => {
			var a = Pop();
			var b = Pop();
			Push(a);
			Push(b);
		});
		AddWord("rot", e => {
			var a = Pop();
			var b = Pop();
			var c = Pop();
			Push(b);
			Push(a);
			Push(c);
		});
		AddWord("sin", e => Push(MathN.Sin(Pop())));
		AddWord("cos", e => Push(MathN.Cos(Pop())));
		AddWord("tan", e => Push(MathN.Tan(Pop())));
		AddWord(">", e => Push(!(Pop() <= Pop())));
		AddWord(">=", e => Push(!(Pop() < Pop())));
		AddWord("==", e => Push(Pop() == Pop()));
		AddWord("!=", e => Push(Pop() != Pop()));
		AddWord("<", e => Push(Pop() >= Pop()));
		AddWord("<=", e => Push(Pop() > Pop()));
		AddWord("cls", e => Console.Clear());
		AddWord("sum", e => {
			Number sum = 0;
			while (Stack.TryPop(out var value)) {
				sum += value.num;
			}
			Push(sum);
		});
		AddWord("prod", e => {
			Number prod = 1;
			while (Stack.TryPop(out var value)) {
				prod *= value.num;
			}
			Push(prod);
		});
		AddWord("push", e => Stash.Push(Stack.Pop()));
		AddWord("pop", e => Stack.Push(Stash.Pop()));
		AddWord("reload", e => LoadDefaultScript());
		AddWord("default", e => {
			File.WriteAllText(defaultPath, defaultScript);
			LoadDefaultScript();
		});
		AddWord("conf", e => {
			using Process fileopener = new();
			fileopener.StartInfo.FileName = defaultPath;
			fileopener.StartInfo.UseShellExecute = true;
			fileopener.Start();
		});
	}

	public Engine() {
		DefaultWords();
	}

	public void PrintStack() {
		if (Stack.Count > 0)
			Print(string.Join(' ', Stack.Reverse()));
	}

	public Error SafeProcess(string input, bool subroutine = false) {
		var stack = Stack.FastClone();
		var stash = Stash.FastClone();
		var words = new Dictionary<string, Word>(Words);
		Error error = Process(input, subroutine);
		if (error == Error.None)
			return error;
		Stack = stack;
		Stash = stash;
		Words = words;
		Print("Error: " + error);
		PrintStack();
		return error;
	}

	private Error Process(string input, bool subroutine = false) {
		if (input == null) {
			PrintStack();
			return Error.None;
		}

		// Format input and split into array
		var wordstr = input.Trim().ToLower();
		var cmtidx = wordstr.IndexOf('#');
		if (cmtidx != -1)
			wordstr = wordstr[..cmtidx];
		var words = wordstr.Split();

		for (int i = 0; i < words.Length; i++) {
			string here = words[i];
			if (here.Length == 0) continue;

			// Define word
			if (i == 0 && here[^1] == ':') {
				string name = here[..^1];
				string definition = string.Join(' ', words.Skip(1));
				AddWord(name, definition);
				break;
			}

			// Load
			if (i == 0 && here == "load") {
				string path = string.Join(' ', words.Skip(1));
				Error err = LoadScript(path);
				if (err != Error.None) return err;
				break;
			}

			// If word, fire and continue
			if (Words.TryGetValue(here, out Word word)) {
				Error error = word.Fire(this);
				if (error != Error.None) return error;
				continue;
			}

			// Control flow
			if (here == "{") {
				int start = i + 1;
				int end = i;
				int balance = 1;
				while (balance > 0) {
					if (++end >= words.Length) return Error.EndOfInput;
					if (words[end] == "{") balance++;
					if (words[end] == "}") balance--;
				}
				string def = string.Join(' ', words[start..end]);
				while (Stack.TryPop(out var cond) && cond.num != 0) {
					Push(cond);
					Error err = Process(def ?? "", true);
					if (err != Error.None) return err;
				}
				i = end + 1;
				continue;
			}

			//if ( here[0] == '*' ) {

			//}

			// If number, push to stack and continue
			if (Number.TryParse(here, out Number result)) {
				Stack.Push(new Value(result));
				continue;
			}

			// We don't recognize this word
			return Error.UnknownWord;

		}
		if (!subroutine) PrintStack();
		return Error.None;
	}

}