/*

abstract class Expr {

    // try_simplify(): Every addition can be a sum of some more primitive terms, 
    // this is handled by try_simplify method
    // 2*x+x*sin(x) => {x: 2, mult(x, sin(x)): 1}, more generally:
    // c1*e1 + ... + cn*en => { e1: c1, ..., en: cn }
    // robust way to do this, but a lot of CASs like Mathematica
    // are proprietary, so I don't know, how civilized people are doing this

    // try_factor(): has a similar purpose to try_simplify() --
    // e1^c1 * ... * en^cn => { e1: c1, ..., en: cn },
    // again, there is probabaly a way to do it safer and in a
    // way it is more scalable, but I did not find any, and for
    // the purposes of this project it is an overkill in my opinion

    public abstract Expr evaluate();
    public abstract Dictionary<Expr, double> try_simplify();
    public abstract Dictionary<Expr, double> try_factor();
    public abstract string print();

    // Needed for dictionary to find an appropriate bucket and comparison, 
    // used to check if structure of some terms is same, and, say, 
    // merge two terms, adding coefficients

    public abstract override int GetHashCode();
    public abstract override bool Equals(object? obj);

    // Decides, if the child expression must have brackets when printing
    // by the operator precedence. Note that functions, variables and constants
    // all have precedence of 4, the highest, since we never want them to be
    // in brackets, ex: (2) + (sin((x))). 
    // General ex: + has precedence of 1, * has precedence of 2
    // so ((x*(y + z)) - ((u*v) + (w*k))) => x*(y + z) - u*v + w*k
    // Probably, the easiest way to do it, and an elegant one, though my first,
    // naive approach was to check every single type of structure, like 
    // "if you are multiplying two Add instances, put them in the brackets",
    // but then I found this way

    protected string decide_brackets(Expr child) {
        if (child.Precedence < this.Precedence) {
            return $"({child.print()})";
        }
        else {
            return child.print();
        }
    }

    public virtual int Precedence => 0;

    // Converts the dictionary of coeffitients back to an expression
    // Ex: { e1: c1, ..., en: cn } => c1*e1 + ... + cn*en

    protected static Expr coeff_dict_to_expr(Dictionary<Expr, double> dict_to_interpret) {
        if (dict_to_interpret.Count == 0)
            return new ConstantExpr(0);

        Expr? result = null;

        foreach (var (key, coeff) in dict_to_interpret) {
            if (coeff == 0) continue;

            double abs_coeff = Math.Abs(coeff);
            Expr term;
            if (abs_coeff != 1) {
                if (key is ConstantExpr) {
                    term = new ConstantExpr(abs_coeff);
                }
                else {
                    term = new Multiply(new ConstantExpr(abs_coeff), key);
                }
            }
            else {
                term = key;
            }

            if (coeff > 0) {
                if (result == null) {
                    result = term;
                }
                else {
                    result = new Add(result, term);
                }
            }
            else if (coeff < 0) {
                if (result == null) {
                    result = new Multiply(new ConstantExpr(-1), term);
                }
                else {
                    result = new Subtract(result, term);
                }
            }
        }
        return result ?? new ConstantExpr(0);
    }

    // Similarly, { e1: c1, ..., en: cn } => e1^c1 * ... * en^cn

    protected static Expr factor_dict_to_expr(Dictionary<Expr, double> dict_to_interpret) {
        Expr? result = null;
        foreach (var (key, exp) in dict_to_interpret) {
            Expr term;
            if (exp == 1.0) {
                term = key;
            }
            else if (exp == 0.0) {
                term = new ConstantExpr(1);
            }
            else {
                term = new Power(key, new ConstantExpr(exp));
            }
            result = result == null ? term : new Multiply(result, term);
        }
        return result!;
    }
}

class ConstantExpr : Expr {

    public double Value { get; }

    public override int Precedence => 4;

    public ConstantExpr(double val) {
        Value = val;
    }

    public override Dictionary<Expr, double> try_simplify() {
        return new Dictionary<Expr, double> { [new ConstantExpr(1.0)] = Value };
    }

    public override Dictionary<Expr, double> try_factor() {
        return new Dictionary<Expr, double> { [this] = 1.0 };
    }

    public override Expr evaluate() {
        return this;
    }

    public override int GetHashCode() =>
        Value.GetHashCode();

    public override bool Equals(object? obj) {
        return obj is ConstantExpr c && Value == c.Value;
    }

    // I decided not to complicate the system even further and just use
    // approximation
    public override string print() => Value.ToString("G4");

}

class VariableExpr : Expr {

    public string Name { get; }

    public override int Precedence => 4;

    public VariableExpr(string name) {
        Name = name;
    }

    public override Dictionary<Expr, double> try_simplify() {
        return new Dictionary<Expr, double> { [this] = 1.0 };
    }

    public override Dictionary<Expr, double> try_factor() {
        return new Dictionary<Expr, double> { [this] = 1.0 };
    }

    public override Expr evaluate() {
        return this;
    }

    public override int GetHashCode() =>
        Name.GetHashCode();

    public override bool Equals(object? obj) {
        return obj is VariableExpr v && Name == v.Name;
    }

    public override string print() => Name;

}

class Function : Expr {

    private string _name;
    private Expr _arg;

    public override int Precedence => 4;

    public Function(string name, Expr arg) {
        _name = name;
        _arg = arg;
    }

    public string Name => _name;
    public Expr Arg => _arg;

    public override Dictionary<Expr, double> try_simplify() {
        return new Dictionary<Expr, double> { [new Function(_name, _arg.evaluate())] = 1.0 };
    }

    public override Dictionary<Expr, double> try_factor() {
        return new Dictionary<Expr, double> { [new Function(_name, _arg.evaluate())] = 1.0 };
    }

    public override Expr evaluate() {
        return new Function(_name, _arg.evaluate());
    }

    public override int GetHashCode() {
        return HashCode.Combine(_name, _arg.GetHashCode());
    }

    public override bool Equals(object? obj) {
        return obj is Function f && (Name == f.Name && _arg.Equals(f.Arg));
    }

    public override string print() {
        return $"{_name}({_arg.print()})";
    }

}

class Add : Expr {

    private Expr _left;
    private Expr _right;

    public override int Precedence => 1;

    public Add(Expr left, Expr right) {
        _left = left;
        _right = right;
    }

    public override Dictionary<Expr, double> try_simplify() {
        var left = _left.try_simplify();
        var right = _right.try_simplify();
        var result = new Dictionary<Expr, double>(left);

        // Adds up terms of similar structure, ie same key, or create new 
        // term, if such key does not exist on the left
        foreach (var (key, coeff) in right) {
            if (result.ContainsKey(key)) {
                if ((result[key] + coeff) != 0) {
                    result[key] += coeff;
                }
                else {
                    result.Remove(key);
                }
            }
            else {
                result[key] = coeff;
            }
        }
        return result;
    }

    public override Dictionary<Expr, double> try_factor() {
        return new Dictionary<Expr, double> { [this] = 1.0 };
    }

    public override Expr evaluate() {
        var coeff_dict = try_simplify();
        Expr result = coeff_dict_to_expr(coeff_dict);
        return result;
    }

    // A trick to make x*y == y*x, HashCode.Combine is not commutative
    public override int GetHashCode() {
        return HashCode.Combine(typeof(Add), _left.GetHashCode() + _right.GetHashCode());
    }

    public override bool Equals(object? obj) {
        return obj is Add a
               && ((_left.Equals(a._left) && _right.Equals(a._right))
               || (_right.Equals(a._left) && _left.Equals(a._right)));
    }

    public override string print() => $"{decide_brackets(_left)} + {decide_brackets(_right)}";

}

class Subtract : Expr {

    private Expr _left;
    private Expr _right;

    public override int Precedence => 1;

    public Subtract(Expr left, Expr right) {
        _left = left;
        _right = right;
    }

    public override Dictionary<Expr, double> try_simplify() {
        var left = _left.try_simplify();
        var right = _right.try_simplify();
        var result = new Dictionary<Expr, double>(left);

        // Subtract up terms of similar structure, ie same key, or create new 
        // term, if such key does not exist on the left, but with an opposite sign
        // Here, custom GetHashCode comes into play, and since for Add and Multiply
        // it is computed as 
        // HashCode.Combine(typeof(Add/Multiply), _left.GetHashCode() + _right.GetHashCode()),
        // Add(VariableExpr(x), VariableExpr(y)) and Add(VariableExpr(y), VariableExpr(x))
        // are considered to be the same expression by the dictionary, ei be in the same bucket
        foreach (var (key, coeff) in right) {
            if (result.ContainsKey(key)) {
                if ((result[key] - coeff) != 0) {
                    result[key] -= coeff;
                }
                else {
                    result.Remove(key);
                }
            }
            else {
                result[key] = -coeff;
            }
        }
        return result;
    }

    public override Dictionary<Expr, double> try_factor() {
        return new Dictionary<Expr, double> { [this] = 1.0 };
    }

    public override Expr evaluate() {
        var coeff_dict = try_simplify();
        Expr result = coeff_dict_to_expr(coeff_dict);
        return result;
    }

    public override int GetHashCode() {
        return HashCode.Combine(_left, _right);
    }

    public override bool Equals(object? obj) {
        return obj is Subtract a && _left.Equals(a._left) && _right.Equals(a._right);
    }

    public override string print() => $"{decide_brackets(_left)} - {decide_brackets(_right)}";

}


class Multiply : Expr {

    private Expr _left;
    private Expr _right;

    public override int Precedence => 2;

    public Multiply(Expr left, Expr right) {
        _left = left;
        _right = right;
    }

    public override Dictionary<Expr, double> try_simplify() {
        var left = _left.try_simplify();
        var right = _right.try_simplify();
        var result = new Dictionary<Expr, double>();

        // Performs standard polynomial multiplication
        foreach (var (key1, coeff1) in left) {
            foreach (var (key2, coeff2) in right) {

                Expr res_key;
                if (key1 is ConstantExpr) {
                    res_key = key2;
                }
                else if (key2 is ConstantExpr) {
                    res_key = key1;
                }
                else {
                    res_key = new Multiply(key1, key2);
                }

                double res_coeff = coeff1 * coeff2;
                
                // TODO: decide if this check is needed
                // This seems to be a piece of dead code, this functionality
                // seems to be fully handled in Multiply.evaluate()
                if (result.ContainsKey(res_key)) {
                    if ((result[res_key] + res_coeff) != 0) {
                        result[res_key] += res_coeff;
                    }
                    else {
                        result.Remove(res_key);
                    }
                }
                else {
                    result[res_key] = res_coeff;
                }

            }
        }
        return result;
    }

    public override Dictionary<Expr, double> try_factor() {
        var left = _left.try_factor();
        var right = _right.try_factor();

        var result = new Dictionary<Expr, double>(left);

        // Same as Add.try_simplify(), but adds up exponents,
        // not coefficients
        foreach (var (key, exp) in right) {
            if (result.ContainsKey(key)) {
                result[key] += exp;
            }
            else {
                result[key] = exp;
            }
        }

        return result;
    }

    public override Expr evaluate() {
        var coeff_dict = try_simplify();
        var factored_dict = new Dictionary<Expr, double>();

        // After polynomial multiplication tries to simplify 
        // term-by term and add up
        foreach (var (key, coeff) in coeff_dict) {
            var factored = key.try_factor();
            Expr factored_expr = factor_dict_to_expr(factored);
            if (factored_dict.ContainsKey(factored_expr)) {
                factored_dict[factored_expr] += coeff;
            }
            else {
                factored_dict[factored_expr] = coeff;
            }
        }
        return coeff_dict_to_expr(factored_dict);
    }

    public override int GetHashCode() {
        return HashCode.Combine(typeof(Multiply), _left.GetHashCode() + _right.GetHashCode());
    }

    public override bool Equals(object? obj) {
        return obj is Multiply m
               && ((_left.Equals(m._left) && _right.Equals(m._right))
               || (_right.Equals(m._left) && _left.Equals(m._right)));
    }

    public override string print() {
        bool needStar = _left is ConstantExpr && _right is ConstantExpr
                 || _right is Add
             || _right is Subtract;

        if (_left is ConstantExpr c1 && c1.Value == -1) {
            return $"-{decide_brackets(_right)}";
        }

        if (_right is ConstantExpr c2 && c2.Value == -1) {
            return $"-{decide_brackets(_left)}";
        }

        string op = needStar ? "*" : "";
        return $"{decide_brackets(_left)}{op}{decide_brackets(_right)}";
    }

}

class Divide : Expr {

    public Expr _left { get; }
    public Expr _right { get; }

    public override int Precedence => 2;

    public Divide(Expr left, Expr right) {
        _left = left;
        _right = right;
    }

    // Not only returns trivial coefficient dictionary, but also
    // normalizes the coefficients
    public override Dictionary<Expr, double> try_simplify() {
        Expr left = _left.evaluate();
        Expr right = _right.evaluate();

        if (right is ConstantExpr c && c.Value != 0) {
            var numerator_terms = left.try_simplify();
            var result = new Dictionary<Expr, double>();
            foreach (var (key, coeff) in numerator_terms) {
                result[key] = coeff / c.Value;
            }
            return result;
        }

        return new Dictionary<Expr, double> { [new Divide(left, right)] = 1.0 };
    }

    public override Dictionary<Expr, double> try_factor() {

        // Give up, if one of the terms is Add
        if (_left is Add || _right is Add || _left is Subtract || _right is Subtract) {
            if (_right.Equals(_left)) {
                return new Dictionary<Expr, double> { [new ConstantExpr(1)] = 1.0 };
            }
            return new Dictionary<Expr, double> { [this] = 1.0 };
        }

        var left = _left.try_factor();
        var right = _right.try_factor();

        var result = new Dictionary<Expr, double>(left);

        // Simplify to 1 properly
        if (left.Count == 1 && right.Count == 1) {
            var (key1, exp1) = left.Single();
            var (key2, exp2) = right.Single();
            if (key1 is ConstantExpr c1 && key2 is ConstantExpr c2) {
                double val = c1.Value / c2.Value;
                return new Dictionary<Expr, double> { [new ConstantExpr(val)] = 1.0 };
            }
        }

        // Similarly to adding up exponents in Multiply.try_factor(), subtract
        foreach (var (key, exp) in right) {
            if (result.ContainsKey(key)) {
                result[key] -= exp;
                if (result[key] == 0) {
                    result.Remove(key);
                }
            }
            else {
                result[key] = -exp;
            }
        }

        if (result.Count == 0) {
            return new Dictionary<Expr, double> { [new ConstantExpr(1)] = 1.0 };
        }
        return result;
    }

    public override Expr evaluate() {
        Expr simplified = new Divide(_left.evaluate(), _right.evaluate());
        var factor_dict = simplified.try_factor();
        if (factor_dict_to_expr(factor_dict).Equals(simplified)) {
            return simplified;
        }
        // Ugly trick, division is not always evaluated fully, so 
        // I had to put two evaluates
        Expr to_simplify_more = factor_dict_to_expr(factor_dict).evaluate().evaluate();
        return to_simplify_more;
    }

    public override int GetHashCode() {
        return HashCode.Combine(_left, _right);
    }

    public override bool Equals(object? obj) {
        return obj is Divide a && _left.Equals(a._left) && _right.Equals(a._right);
    }

    public override string print() => $"{decide_brackets(_left)}/{decide_brackets(_right)}";

}

class Power : Expr {

    private Expr _left;
    private Expr _right;

    public override int Precedence => 3;

    public Power(Expr left, Expr right) {
        _left = left;
        _right = right;
    }

    public override Dictionary<Expr, double> try_simplify() {
        return new Dictionary<Expr, double> { [new Power(_left.evaluate(), _right.evaluate())] = 1.0 };
    }

    // x => { x : 1 }
    // x^3 => { x : 3 }
    // (x + y)^3 => { (x + y) : 3}
    public override Dictionary<Expr, double> try_factor() {

        var left = _left.try_factor();
        var right = _right.try_factor();

        if (left.Count == 1 && right.Count == 1) {
            var (key1, exp1) = left.Single();
            var (key2, exp2) = right.Single();
            if (key1 is ConstantExpr c1 && key2 is ConstantExpr c2) {
                double val = Math.Pow(c1.Value, c2.Value);
                return new Dictionary<Expr, double> { [new ConstantExpr(val)] = 1.0 };
            }
            if (key2 is ConstantExpr c) { // Without this check it falls through to the default case
                return new Dictionary<Expr, double> { [key1] = exp1 * c.Value };
            }
        }
        else if (right.Count == 1) {
            var (key, exp) = right.Single();
            if (key is ConstantExpr c) {
                var result = new Dictionary<Expr, double>(left);
                double exp_multiplier = c.Value;
                var keys = result.Keys.ToList();
                foreach (var key_base in keys) {
                    result[key_base] *= exp_multiplier;
                }
                return result;
            }
        }
        return new Dictionary<Expr, double> { [this] = 1.0 };
    }

    public override Expr evaluate() {
        var factored = try_factor();
        return factor_dict_to_expr(factored);
    }

    public override int GetHashCode() {
        return HashCode.Combine(_left, _right);
    }

    public override bool Equals(object? obj) {
        return obj is Power a && _left.Equals(a._left) && _right.Equals(a._right);
    }

    public override string print() => $"{decide_brackets(_left)}^{decide_brackets(_right)}";

}

enum TokenType {
    
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

class Token {

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

class Lexer {

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

class Parser {

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
/*
class Top {

    static void Main() {

        Lexer lexer = new Lexer();
        List<Token> tokens = lexer.lex("");
        Parser parser = new Parser();
        Expr result = parser.Parse(tokens);
        Console.WriteLine(result.evaluate().print());

    }

}
*/
