namespace Wafer;

internal class Program {

	static readonly Engine engine = new();

	private static void Main(string[] args) {

		engine.LoadDefaultScript();

		if (args.Length > 0) {
			engine.SafeProcess(string.Join(' ', args));
			return;
		}

		Console.Write(" :: ");
		string? input = Console.ReadLine()?.Trim().ToLower();
		while (input is not null) {
			engine.SafeProcess(input);
			Console.Write(" :: ");
			input = Console.ReadLine()?.Trim().ToLower();
		}

	}

}