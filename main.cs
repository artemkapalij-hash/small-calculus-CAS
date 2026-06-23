class Calculator {

    private Lexer lexer = new Lexer();
    private Parser parser = new Parser();

    private void display_help() {
        Console.WriteLine(
@"Calculator for symbolic differentiation and integration.

Usage:
  Enter a mathematical expression at the prompt, then choose
  whether to differentiate or integrate and specify the variable.
  Note, that explicit brackets for functions must be used,
  i.e. sin(x), not sinx (in the latter case behaviour of the
  program is undefined)

Supported operations:
  +, -        addition, subtraction
  *, /        multiplication, division
  ^           exponentiation (right-associative)
  (...)       grouping

Built-in functions:
  sin, cos, tan       (e.g. sin(x), tan(x^2 + 1))

Constants:
  Numeric literals (integers and decimals)

Examples:
  >>> 3x^2 + 1
  >>> sin(x)cos(x)
  >>> 3*2^x

Commands:
  help        Show this message
  exit        Quit the program");
    }

    public void mainloop() {
        
        Console.WriteLine(
@"Input the expression you want to find derivative of or integrate, 
input ""help"" to display help, ""exit"" to exit");
        string? input;
        Expr in_expr;

        while (true) {

            Console.Write(">>> "); 
            input = Console.ReadLine();
            
            if (input == "help") {
                display_help();
                continue;
            }
            else if (input == "exit") {
                return;
            }
            else if (input == "") {
                continue;
            }
            
            try {
                List<Token> tokens = lexer.lex(input!);
                in_expr = parser.Parse(tokens!);
            }
            catch (Exception) {
                Console.WriteLine("Invalid expression");
                continue;
            }

            Console.Write("Do you want to find the derivative or integrate? (d/i) ");
            string? choice = Console.ReadLine();
            Console.Write("By what variable? ");
            string? variable = Console.ReadLine();
            
            // choice.ToLower()
            if (choice == "d" || choice == "D") {
                try {
                    Console.WriteLine($"in  -> {in_expr.simplify().print()}");
                    Console.WriteLine($"out -> {in_expr.simplify().derivative(variable!).simplify().simplify().print()}");
                }
                catch (Exception) {
                    Console.WriteLine("Cannot compute such derivatives");
                }
            }
            else if (choice == "i" || choice == "I") {
                try {
                    Expr to_integrate;
                    if (in_expr.simplify() is Add || in_expr.simplify() is Subtract) {
                        to_integrate = in_expr.simplify();
                    }
                    else {
                        to_integrate = new Multiply(new ConstantExpr(1), in_expr.simplify());
                    }
                    Console.WriteLine($"in  -> {to_integrate.simplify().print()}");
                    Console.WriteLine($"out -> {to_integrate.integral(variable!).simplify().simplify().simplify().print()} + C");
                }
                catch (Exception) {
                    Console.WriteLine("Cannot compute such integrals");
                }
            }
            else {
                Console.WriteLine("Invalid argument");
            }

        }

    }

}

class Top {

    static void Main() {
        
        Calculator calculator = new Calculator();
        calculator.mainloop();

    }

}
