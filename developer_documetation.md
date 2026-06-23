## Project Structure

The project is organized into three main source files:

```text
.
|-- computation.cs
|-- parser.cs
|-- main.cs
```

* **`computation.cs`** – Contains the implementation of computation-related functionality.
* **`parser.cs`** – Contains the implementation of the parser and input processing logic.
* **`main.cs`** – Contains the application's entry point and coordinates the execution of the program.

## Coding Standards

### Naming Conventions

* Variables and methods use `snake_case`.
* Class names use `PascalCase`.
* Constants are named using uppercase letters with underscores where appropriate.

### Formatting

* Opening braces are placed on the same line as declarations and control statements.
* Indentation uses consistent spacing throughout the project.
* Logic is implemented directly within the relevant classes rather than being split into additional layers or helper classes.

### Linting

* No automated linting or code formatting tools are used.
* Code style is maintained manually for consistency.

### Version Control

* Development was performed on a single branch.
* Commit messages briefly describe the implemented feature or fix.

## Implementation

* Parsing uses precedence climbing method and produces an AST.
* Derivatives are computed in a recursive way.
* Integrals are computed by trying all methods, until one works, or the program runs out of methods

### For I/O, dependencies and installation, refer to the README
