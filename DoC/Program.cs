// See https://aka.ms/new-console-template for more information

using Absyn;
using DoC.Absyn;
using DoC.Sema;
using DoC.Sema.Bind;
using DoC.Sema.Type;
using DoC.Syntax;
using DoC.Syntax.Parse;
using Lib;

namespace DoC;

public static class Program
{
	public static int Main(string[] args)
	{
		PassRegistry.Register(new BindPass());
		PassRegistry.Register(new ParsePass());
		PassRegistry.Register(new PrettyPass());
		PassRegistry.Register(new TypePass());

		// var passes = args.Where(e => e.StartsWith("--") && PassRegistry.Instance.FromArg(e) != null).ToArray();
		var files = args.Where(e => !(e.StartsWith("--")));


		Driver driver = new Driver();
		var passes = PassRegistry.MakePassChain(args);

		if (!passes)
		{
			Console.Error.WriteLine(passes.TheError!.Message);
			return 1;
		}

		int c = 0;
		foreach (var file in files)
		{
			int c1;
			if ((c1 = driver.Run(passes.Value.ToList(), file)) != 0)
			{
				c = c1;
			}
		}

		return c;
		/*const string big = """
		                   type a := string[][]

		                   fun main(i : i32, argv : a) : { I : i32, b : string } with NotZero
		                   {
		                     let a := 5 + 3 * 2 + (1 + 2 ? 3 : 4);
		                     a[5];
		                     if (i = 0)
		                     {
			                    a := 67 + 2 * a.b[5].c;
		                        return 67;
		                   }
		                     else
		                     {
				                    if (i = 1)
				                    {
				                    };

				                    print("67 !");
		                     };
		                    return 85;
		                   }

		                   fun aux(i : string) : string with notEmpty =>  i
		                   """;

		var input = "if a.b { print(\"hehe\") } ";
		var pattern = "if";

//Console.WriteLine(
//    Regex.IsMatch(input, "^" + pattern + "(?=\\s|$)"));

		//Console.WriteLine("Input: " + big);

		//	Token token;
		//	var scanner = new Lexer("caca.dc", big);

		//while ((token = scanner.Pop()).Type != TokenType.None) Console.WriteLine(token);

		var parser = new Parser("caca.dc", big);
		var res = parser.Program();

		Console.WriteLine("=== C# Pretty ===");
		Console.WriteLine(Pretty.PrintProgram(res));

		Console.WriteLine("\n=== F# Pretty ===");
		Console.WriteLine(FsPretty.PrintProgram(res));

	   var bounded = new Binder(res).Bind();*/




// Rule -> Returns a Token OR Run a procedure

// TopLexer         => Uses RegexMatcher(TopLexerRules)
// StringLexer      => Uses RegexMatcher()
	}

}