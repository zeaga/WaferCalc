# Wafer

**Wafer** is a scriptable stack-based calculator for the terminal. It supports custom scripting, user-defined commands, and a variety of mathematical operations. Wafer is designed to be lightweight, extensible, and powerful for quick calculations or complex scripts.

## Features

- **Stack-based Operations**: Perform operations directly on a stack of values.
- **Scriptable**: Define custom commands and scripts to extend functionality.
- **Built-in Math**: Includes a wide range of mathematical functions like trigonometry, logarithms, and power operations.
- **Custom Configuration**: Load and save default scripts for personalized setups.
- **Interactive REPL**: Use Wafer in an interactive terminal session.

## Installation

You can clone the repository and build using the dotnet CLI
```bash
git clone https://github.com/zeaga/Wafer.git
cd Wafer
dotnet build
```

## Usage

### Interactive Mode
Run Wafer without arguments to enter an interactive REPL mode:
```bash
Wafer
```
After that you can type commands and see results in real time:
```text
 :: 3 4 +
7
```

### Script Mode
Pass commands as arguments:
```bash
Wafer 3 4 +
```

### Default Configuration
Wafer automatically loads a default script (`Wafer.conf`) located in the executable's directory. Modify this file to customize your environment.

## Core Commands

- **Arithmetic**: `+`, `-`, `*`, `/`, `**` (power)
- **Logical**: `and`, `or`, `not`, `xor`
- **Stack Manipulation**: `dup`, `swap`, `rot`, `drop`, `empty`, `count`
- **Control Flow**: `{ ... }` for loops
- **Trigonometry**: `sin`, `cos`, `tan`
- **Utilities**: `help`, `exit`, `cls`

## Extensibility

You can define your own commands or modify the default script for custom functionality:
```text
square: dup *
 :: 5 square .
25
```

## Development

Wafer is written in C# and leverages the .NET runtime. Contributions and suggestions are welcome!

### Folder Structure

- `Engine.cs`: Core functionality, including stack operations and command processing.
- `Extensions.cs`: Utility extensions for stack operations.
- `Program.cs`: Entry point and REPL implementation.

## Contributing

1. Fork the repository.
2. Create a feature branch.
3. Submit a pull request.

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.

## Contact

For questions or suggestions, feel free to open an issue.