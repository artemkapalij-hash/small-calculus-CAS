<h1> A command-line symbolic calculator for differentiation and integration </h1>

<h3> Requirements </h3>

dotnet 10.X, no dependencies

<h3> Installation</h3>

```bash
git clone <repo-url>
cd <project-folder>
dotnet run
```

<h3> Syntax and limitations </h3>

The syntax can be observed in the examle run at the end of the readme, the only thing is that an expression like

```
tanx
```

Will give you an error. Arguments of the functions must be bracketed, right version:

```
tan(x)
```

<h3> Features </h3>

Differentiation:
- Power rule
- Product rule
- Chain rule

Integration:
- Direct integration
- Substitutuion
- Integration by parts

<h3> Example run: </h3>

Input the expression you want to find derivative of or integrate,
input "help" to display help, "exit" to exit 

\>>> sin(x)^(-2)cos(x)

Do you want to find the derivative or integrate? (d/i) i

By what variable? x

in  -> sin(x)^-2cos(x)

out -> -sin(x)^-1 + C

\>>> (x + 2)(2x^3 + x^2)

Do you want to find the derivative or integrate? (d/i) i

By what variable? x

in  -> 2x^4 + 5x^3 + 2x^2

out -> 0.4x^5 + 1.25x^4 + 0.6667x^3 + C

\>>> d^3sin(d)

Do you want to find the derivative or integrate? (d/i) i

By what variable? d

in  -> d^3sin(d)

out -> -d^3cos(d) + 3d^2sin(d) + 6dcos(d) - 6sin(d) + C

\>>> sin(cos(tan(x)))

Do you want to find the derivative or integrate? (d/i) d

By what variable? x

in  -> sin(cos(tan(x)))

out -> -cos(cos(tan(x)))sin(tan(x))cos(x)^-2

\>>> x^y

Do you want to find the derivative or integrate? (d/i) i

By what variable? x

in  -> x^y

out -> x^(y + 1)/(y + 1) + C

\>>> (x + y)^4

Do you want to find the derivative or integrate? (d/i) d

By what variable? y

in  -> x^4 + 4x^3y + 6x^2y^2 + 4xy^3 + y^4

out -> 4x^3 + 12x^2y^1 + 4y^3 + 12xy^2

\>>> (x + y)^4

Do you want to find the derivative or integrate? (d/i) i

By what variable? x

in  -> x^4 + 4x^3y + 6x^2y^2 + 4xy^3 + y^4

out -> 0.2x^5 + yx^4 + 2y^2x^3 + 2y^3x^2 + y^4x + C
