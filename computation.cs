public abstract class Expr {

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

    public abstract Expr simplify();
    public abstract Dictionary<Expr, double> try_simplify();
    public abstract Dictionary<Expr, double> try_factor();
    public abstract string print();
    public abstract Expr derivative(string diff_var);
    public abstract Expr integral(string int_var);

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

    public override Expr simplify() {
        return this;
    }

    public override Expr derivative(string diff_var) {
        return new ConstantExpr(0);
    }
    
    public override Expr integral(string int_var) {
        return new Multiply(this, new VariableExpr(int_var));
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

    public override Expr simplify() {
        return this;
    }

    public override Expr derivative(string diff_var) {
        if (diff_var == Name) {
            return new ConstantExpr(1);
        }
        else {
            return new ConstantExpr(0);
        }
    } 
    
    public override Expr integral(string int_var) {
        if (int_var != Name) {
            return new Multiply(this, new VariableExpr(int_var));
        } else {
            return new Multiply(new ConstantExpr(0.5), new Power(this, new ConstantExpr(2)));
        }
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
        return new Dictionary<Expr, double> { [new Function(_name, _arg.simplify())] = 1.0 };
    }

    public override Dictionary<Expr, double> try_factor() {
        return new Dictionary<Expr, double> { [new Function(_name, _arg.simplify())] = 1.0 };
    }

    public override Expr simplify() {
        return new Function(_name, _arg.simplify());
    }

    public override Expr derivative(string diff_var) {
        switch (_name) {
            case "sin": return new Multiply(
                                   new Function("cos", _arg), 
                                   _arg.derivative(diff_var));
            case "cos": return new Multiply(
                                   new Multiply(
                                       new ConstantExpr(-1), 
                                       new Function("sin", _arg)), 
                                   _arg.derivative(diff_var));
            case "tan": return new Multiply(
                                   new Power(
                                       new Function("cos", _arg), 
                                       new ConstantExpr(-2)),
                                   _arg.derivative(diff_var));
            case "ln": if (_arg.derivative(diff_var).simplify() is ConstantExpr c && c.Value == 0) return new ConstantExpr(0);
                       else goto default;
            default: throw new Exception($"Unrecognized function {_name}({_arg.print()})");
        }
    }
    
    public override Expr integral(string int_var) {
        Expr first_derivative_arg = _arg.derivative(int_var);
        try {
            bool arg_is_linear = first_derivative_arg.derivative(int_var) is ConstantExpr c && c.Value == 0;
        } 
        catch (Exception) {
            return this;
        }

        switch (_name) {
            case "sin": return new Multiply(
                                   new Function("cos", _arg), 
                                    new ConstantExpr(-1));
            case "cos": return new Multiply(
                                   new Function("sin", _arg), 
                                   new ConstantExpr(1)); 
            case "tan": return new Multiply(
                                    new ConstantExpr(-1),
                                    new Function(
                                        "ln", 
                                        new Function(
                                            "cos",
                                            _arg))); 
            default: throw new Exception("Cannot compute this integral");
        }
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

    public override Expr simplify() {
        var coeff_dict = try_simplify();
        Expr result = coeff_dict_to_expr(coeff_dict);
        return result;
    }

    public override Expr derivative(string diff_var) {
        return new Add(
                       _left.derivative(diff_var), 
                       _right.derivative(diff_var));
    } 

    // A trick to make x*y == y*x, HashCode.Combine is not commutative
    public override int GetHashCode() {
        return HashCode.Combine(typeof(Add), _left.GetHashCode() + _right.GetHashCode());
    }
    
    public override Expr integral(string int_var) {

        Expr right_to_integrate; 
        Expr left_to_integrate;  
        if (_left is Add || _left is Subtract) {
            left_to_integrate = _left;
        }
        else {
            left_to_integrate = new Multiply(new ConstantExpr(1), _left);
        }
        
        if (_right is Add || _right is Subtract) {
            right_to_integrate = _right;
        }
        else {
            right_to_integrate = new Multiply(new ConstantExpr(1), _right);
        }

        return new Add(
                left_to_integrate.integral(int_var), 
                right_to_integrate.integral(int_var));
    
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

    public override Expr simplify() {
        var coeff_dict = try_simplify();
        Expr result = coeff_dict_to_expr(coeff_dict);
        return result;
    }

    public override Expr derivative(string diff_var) {
        return new Subtract(
                       _left.derivative(diff_var), 
                       _right.derivative(diff_var));
    }
    
    public override Expr integral(string int_var) {
        
        Expr left_to_integrate;
        Expr right_to_integrate;

        if (_left is Add || _left is Subtract) {
            left_to_integrate = _left;
        }
        else {
            left_to_integrate = new Multiply(new ConstantExpr(1), _left);
        }
        
        if (_right is Add || _right is Subtract) {
            right_to_integrate = _right;
        }
        else {
            right_to_integrate = new Multiply(new ConstantExpr(1), _right);
        }

        return new Subtract(
                left_to_integrate.integral(int_var), 
                right_to_integrate.integral(int_var));
    
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

    public override Expr simplify() {
        var coeff_dict = try_simplify();
        var factored_dict = new Dictionary<Expr, double>();
       
        if (_left is ConstantExpr c1 && _right is ConstantExpr c2) { 
            ConstantExpr result = new(c1.Value * c2.Value);
            return result;
        }

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
 
    public override Expr derivative(string diff_var) {
        return new Add(
                       new Multiply(
                           _left.derivative(diff_var), 
                           _right),
                       new Multiply(
                           _left, 
                           _right.derivative(diff_var)));
    } 
    
    private static (Expr, Expr) factor_out_constants(Expr expr_in, string int_var) {

        var terms = expr_in.try_factor();
        Dictionary<Expr, double> to_factor = new();
        to_factor[new ConstantExpr(1)] = 1;
        
        foreach (var (term, exp) in terms) {
            if (term.derivative(int_var).simplify().simplify() is ConstantExpr c && c.Value == 0) {
                to_factor[term] = exp;
            }
        }

        foreach (var (term, exp) in to_factor) {
            terms.Remove(term);
        }

        Expr base_expr = factor_dict_to_expr(terms);
        Expr factor = factor_dict_to_expr(to_factor);
        
        return (base_expr, factor);

    }

    private static Expr build_power_integral(Expr base_expr, double exp, Expr factor_der, Expr factor) {
        
        var exp_plus_1 = new Add(new ConstantExpr(exp), new ConstantExpr(1));
        if (exp_plus_1.simplify() is ConstantExpr c && c.Value == 0) {    
            return new Multiply(
                factor,
                new Divide(
                    new Function("ln", base_expr),
                    factor_der));
        }
        
        else {
            return new Multiply(
                factor,
                new Divide(
                    new Power(base_expr, exp_plus_1),
                    new Multiply(factor_der, exp_plus_1)));
        }
    
    }

    private static Expr build_chain_integral(Expr base_expr, Expr factor_der, Expr factor, string int_var) {
        return new Multiply(factor, new Multiply(factor_der, base_expr.integral(int_var)));
    }

    private static Expr integrate_by_parts(Expr u, Expr dv, Expr factor, string int_var) {
        
        Expr v = dv.integral(int_var);
        Expr du = u.derivative(int_var);
        Expr uv = new Multiply(u, v);
        Expr vdu = new Multiply(v, du);
        Expr result = new Subtract(uv, vdu.simplify().integral(int_var));
        return new Multiply(factor, result);
                   
    }

    private static bool try_integrate_by_derivative_match(Expr base1, Expr base2, 
                                                          double exp1, double exp2, 
                                                          Expr factor, string int_var) {
        
        var (base1_der, factor1_der) = factor_out_constants(base1.derivative(int_var).simplify(), int_var);
        var (base2_der, factor2_der) = factor_out_constants(base2.derivative(int_var).simplify(), int_var);

        if (base1_der == null) {
            base1_der = new ConstantExpr(1);
        }
        
        if (base2_der == null) {
            base2_der = new ConstantExpr(1);
        }
        
        if (base1_der.Equals(base2)) {
            
            if (exp2 == 1) {
                return true;
            }

        }

        return false;
    
    }

    public override Expr integral(string int_var) {
       
        if (this.simplify().derivative(int_var).simplify().simplify() is ConstantExpr c && c.Value == 0) {
            return new Multiply(this, new VariableExpr(int_var));
        }

        (Expr base_expr, Expr factor) = factor_out_constants(this, int_var);
        var base_expr_dict = base_expr.try_factor();
        
        if (base_expr_dict.Count == 1) {

            var (base1, exp1) = base_expr_dict.First();

            if (base1 is Function f) {
                if (try_integrate_by_derivative_match(f.Arg, new ConstantExpr(1), exp1, 1, factor, int_var)) {
                    Expr substitutuion_factor = new Divide(new ConstantExpr(1), f.Arg.derivative(int_var));
                    return build_chain_integral(base1, substitutuion_factor, factor, int_var);
                }
            }

            if (base1 is VariableExpr) {
                return new Multiply(factor, new Power(base1, new ConstantExpr(exp1)).integral(int_var));
            }
            
            if (base1.simplify() is Power p) {
                if (p.right().derivative(int_var).simplify() is ConstantExpr ce && ce.Value == 0) {
                    return new Multiply(factor, p.integral(int_var));
                }
            }
        
        }

        var terms = base_expr_dict.ToArray();
        
        for (int i = 0; i < terms.Length; i++) { 
            
            var (base_i, exp_i) = (terms[i].Key, terms[i].Value);
            var (base_i_der, factor_i_der) = factor_out_constants(base_i.derivative(int_var).simplify(), int_var);
            var rest = new Dictionary<Expr, double>();
            
            for (int j = 0; j < terms.Length; j++) {
                if (j != i) rest[terms[j].Key] = terms[j].Value;
            }
            
            Expr rest_expr = factor_dict_to_expr(rest); 
            
            if (rest_expr == null) {
                rest_expr = new ConstantExpr(1);
            }
            
            if (base_i is VariableExpr ve && ve.Name == int_var && exp_i % 1 == 0) {
                Expr u = new Power(base_i, new ConstantExpr(exp_i));
                return integrate_by_parts(u, rest_expr, factor, int_var);
            }

            if (try_integrate_by_derivative_match(base_i, rest_expr, exp_i, 1, factor, int_var)) {
                return build_power_integral(base_i, exp_i, factor_i_der, factor);
            }

            if (base_i is Function f_i && exp_i == 1) {
                if (try_integrate_by_derivative_match(f_i.Arg, rest_expr, exp_i, 1, factor, int_var)) {
                    Expr substitution_factor = new Divide(new ConstantExpr(1), factor_i_der);
                    return build_chain_integral(base_i, substitution_factor, factor, int_var);
                }
            }
            
            if (base_i is Power p_i
                && p_i.left().derivative(int_var).simplify() is ConstantExpr ce 
                && ce.Value == 0) {
                if (try_integrate_by_derivative_match(p_i.right(), rest_expr, exp_i, 1, factor, int_var)) {
                    Expr substitution_factor = new Divide(new ConstantExpr(1), 
                                                   new Divide(factor_i_der, new Function("ln", p_i.left())));
                    return build_chain_integral(base_i, substitution_factor, factor, int_var);
                }
            }

        }

        throw new Exception("Cannot find such integrals"); 
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
    
        string left_str = decide_brackets(_left);
        string right_str = decide_brackets(_right);
        
        if (_left is ConstantExpr c1 && c1.Value == -1) {
            return $"-{decide_brackets(_right)}";
        }

        if (_right is ConstantExpr c2 && c2.Value == -1) {
            return $"-{decide_brackets(_left)}";
        }

        bool need_star = _left is ConstantExpr && _right is ConstantExpr
                      || _right is Add
                      || _right is Subtract;
        
        if (!need_star) {
            char last_of_left = left_str[left_str.Length - 1];
            char first_of_right = right_str[0];
            need_star = char.IsDigit(last_of_left) && (char.IsDigit(first_of_right) || first_of_right == '.');
        }

        string op = need_star ? "*" : "";
        return $"{left_str}{op}{right_str}";
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
        Expr left = _left.simplify();
        Expr right = _right.simplify();

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

    public override Expr simplify() {
        Expr simplified = new Divide(_left.simplify(), _right.simplify());
        var factor_dict = simplified.try_factor();
        if (factor_dict_to_expr(factor_dict).Equals(simplified)) {
            return simplified;
        }
        // Ugly trick, division is not always evaluated fully, so 
        // I had to put two evaluates
        Expr to_simplify_more = factor_dict_to_expr(factor_dict).simplify().simplify();
        return to_simplify_more;
    }
 
    public override Expr derivative(string diff_var) {
        return new Divide( 
                       new Subtract(
                           new Multiply(
                               _left.derivative(diff_var), 
                               _right),
                           new Multiply(
                               _left, 
                               _right.derivative(diff_var))),
                       new Power(
                           _right,
                           new ConstantExpr(2)));
    } 
    
    public override Expr integral(string int_var) {
        return this;
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
    
    public Expr left() {
        return _left;
    }

    public Expr right() {
        return _right;
    }

    public override int Precedence => 3;

    public Power(Expr left, Expr right) {
        _left = left;
        _right = right;
    }

    public override Dictionary<Expr, double> try_simplify() {
        return new Dictionary<Expr, double> { [new Power(_left.simplify(), _right.simplify())] = 1.0 };
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

    public override Expr simplify() {
        
        _right = _right.simplify();
         
        if (_right is ConstantExpr c && c.Value % 1 == 0 && c.Value > 1) {
            if (_left is Add a) {
                Expr temporary = _left;
                for (int i = 1; i < c.Value; ++i) {
                    Expr step = new Multiply(temporary, _left);
                    temporary = step.simplify();
                }
                return factor_dict_to_expr(temporary.try_factor());
            }
        }

        var factored = try_factor();
        return factor_dict_to_expr(factored);
    
    }

    public override Expr derivative(string diff_var) {
        if (_left.derivative(diff_var).simplify() is ConstantExpr cl && cl.Value == 0) {
            return new Multiply(
                       this,
                       new Multiply (
                           new Function(
                               "ln",
                               _left),
                           _right.derivative(diff_var)));
        }
        if (_right.derivative(diff_var).simplify() is ConstantExpr cr && cr.Value == 0) {
            return new Multiply(
                       _right,
                       new Multiply(
                           new Power(
                               _left,
                               new Subtract(_right, new ConstantExpr(1))),
                           _left.derivative(diff_var)));
        }
        throw new Exception("Cannot find such derivatives");
    }
    
    public override Expr integral(string int_var) {
        if (_right.derivative(int_var) is ConstantExpr c && c.Value == 0) {
            if (_right is ConstantExpr r && r.Value == -1) {
                return new Function("ln", new VariableExpr(int_var));
            } else {
                return new Divide(
                               new Power(
                                   _left,
                                   new Add(
                                       _right,
                                       new ConstantExpr(1))),
                               new Add(
                                   _right,
                                   new ConstantExpr(1)));
            }
        }

        else if (_left.derivative(int_var) is ConstantExpr c2 && c2.Value == 0) {
            return new Divide(
                           this,
                           new Function(
                               "ln", 
                               _left));
        }
        
        return this;
    }

    public override int GetHashCode() {
        return HashCode.Combine(_left, _right);
    }

    public override bool Equals(object? obj) {
        return obj is Power a && _left.Equals(a._left) && _right.Equals(a._right);
    }

    public override string print() => $"{decide_brackets(_left)}^{decide_brackets(_right)}";

}

