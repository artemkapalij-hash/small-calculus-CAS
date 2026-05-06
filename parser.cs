public enum TokenType {
    
    // All the token types organized in a neat
    // way, determined by the lexer and then 
    // parsed into an AST by the parser
    Number,
    Variable,

    Plus,
    Minus,
    Star,
    Slash,
    Caret,

    LeftBracket,
    RightBracket,

    Function,

    EOF

}

public class Token {

    public TokenType type { get; }
    public string text { get; }

    public Token(TokenType token_type, string token_text) {
        type = token_type;
        text = token_text;
    }

    public override string ToString() {
        return $"{type}: {text}";
    }

}

public class Lexer {

    private int pos;
    private List<Token> tokens = [];
    private string expression = "";

    private void lex_number() {
        string num = "";
        while (pos < expression.Length && char.IsDigit(expression[pos])) {
            num += expression[pos];
            pos++;
        }
        if (pos < expression.Length && expression[pos] == '.') {
            num += '.';
            pos++;
            while (pos < expression.Length && char.IsDigit(expression[pos])) {
                num += expression[pos];
                pos++;
            }
        }
        tokens.Add(new Token(TokenType.Number, num));
    }

    private void lex_identifier() {

        string identifier = "";
        while (pos < expression.Length && char.IsLetter(expression[pos])) {
            identifier += expression[pos];
            pos++;
        }

        switch (identifier) {
            case "sin": tokens.Add(new Token(TokenType.Function, identifier.ToString())); break;
            case "cos": tokens.Add(new Token(TokenType.Function, identifier.ToString())); break;
            case "tan": tokens.Add(new Token(TokenType.Function, identifier.ToString())); break;
            default:
                if (identifier.Length == 1) tokens.Add(new Token(TokenType.Variable, identifier));
                else throw new Exception($"unexpected character: {identifier}");
                break;
        }

    }

    public List<Token> lex(string expr_in) {

        pos = 0;
        tokens = new List<Token>();
        expression = expr_in;

        while (pos < expression.Length) {
            switch (expression[pos]) {
                case var c when char.IsDigit(expression[pos]): lex_number(); break;
                case var c when char.IsLetter(expression[pos]): lex_identifier(); break;
                case ' ': pos++; break;
                case '+': tokens.Add(new Token(TokenType.Plus, "+")); pos++; break;
                case '-': tokens.Add(new Token(TokenType.Minus, "-")); pos++; break;
                case '*': tokens.Add(new Token(TokenType.Star, "*")); pos++; break;
                case '/': tokens.Add(new Token(TokenType.Slash, "/")); pos++; break;
                case '^': tokens.Add(new Token(TokenType.Caret, "^")); pos++; break;
                case '(': tokens.Add(new Token(TokenType.LeftBracket, "(")); pos++; break;
                case ')': tokens.Add(new Token(TokenType.RightBracket, ")")); pos++; break;
                default: throw new Exception($"unexpected character: {expression[pos]}");
            }
        }

        return tokens;
    }

}

public class Parser {

    private int pos = 0;
    private List<Token> tokens = new List<Token>();

    private Token? peek() {
        return pos < tokens.Count ? tokens[pos] : null;
    }

    private int precedence(Token t) => t.type switch {
        TokenType.Plus => 1,
        TokenType.Minus => 1,
        TokenType.Star => 2,
        TokenType.Slash => 2,
        TokenType.Caret => 3,
        _ => -1
    };

    private Token consume() {
        Token t = tokens[pos];
        pos++;
        return t;
    }

    private Expr parse_function(string name) {
        Token t = consume();
        if (t.type != TokenType.LeftBracket) {
            throw new Exception("Expected (");
        }
        Expr result = new Function(name, parse(0));
        consume();
        return result;
    }

    private Expr parse_unary_minus() {
        // After minus there is supposed to be either a function, a variable,
        // bracket or a constant, so parse the next thing and multiply by -1
        Expr operand = parse_atomic(); 
        return new Multiply(new ConstantExpr(-1), operand);        
    }

    private Expr parse_brackets() {
        // ')' has precedence of -1, so parse is guaranteed to return on it,
        // thereby ending bracketed expression
        Expr inner = parse(0);
        Token closing = consume(); 
        if (closing.type != TokenType.RightBracket) {
            throw new Exception("Expected )");
        }
        return inner;
    }

    // This parses the number or an opening bracket. If it is 
    // a number, it will pass it into the loop, if a bracket,
    // will recurse and restart th process from the lowest precedence,
    // in a way, it deals with the bracketed expression as a
    // separate thing
    private Expr parse_atomic() {

        Token t = consume();
        switch (t.type) {
            case TokenType.Number: return new ConstantExpr(double.Parse(t.text));
            case TokenType.Variable: return new VariableExpr(t.text);
            case TokenType.Function: return parse_function(t.text);
            case TokenType.Minus: return parse_unary_minus();
            case TokenType.LeftBracket: return parse_brackets();
            default: throw new Exception($"Unexpected token: {t.text}");
        }

    }

    public Expr parse(int min_prec = 0) {
        
        if (tokens.Count == 0) {
            return new ConstantExpr(0);
        }

        Expr left = parse_atomic();

        while (peek() != null && precedence(peek()!) >= min_prec) {
            Token op = consume();

            // Affects how exactly the tree is nested. 2^3^4 => 2^(3^4),
            // whereas 2/3/4 => (2/3)/4 and 2-3-4 => (2-3)-4. This is called
            // left-associativity and right-associativity accordingly.
            // If we recurse with same precedence level, it nests it
            // in a right-associative way, higher will cause left-associativity.
            // '+' and '*' are associative, so they are can be parsed in any way,
            // my algorith does it intutitively left-associatively, 
            // ex:  3+4+5 => (3+4)+5 instead of 3+(4+5)
            Expr right;
            if (op.type == TokenType.Caret) {
                right = parse(precedence(op));
            }
            else {
                right = parse(precedence(op) + 1);
            }

            left = op.type switch {
                TokenType.Plus => new Add(left, right),
                TokenType.Minus => new Subtract(left, right),
                TokenType.Star => new Multiply(left, right),
                TokenType.Slash => new Divide(left, right),
                TokenType.Caret => new Power(left, right),
                _ => throw new Exception($"Unknown operator: {op.text}")
            };
        }
        return left;
    }

    public Expr Parse(List<Token> token_input) {
        pos = 0;
        tokens = token_input;
        return parse();
    }

}
