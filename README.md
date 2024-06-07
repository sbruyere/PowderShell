# PowderShell

**PowderShell** is a powerful tool designed to deobfuscate PowerShell scripts. It quickly and effectively removes layers of obfuscation, revealing the original code for easier analysis and understanding. Ideal for cybersecurity professionals and malware analysts, PowderShell simplifies the process of dissecting and interpreting complex, obfuscated scripts, saving time and improving accuracy in threat detection and response.

## Features

- **AST Parsing**: Leverage the official [PowerShell source code](https://github.com/PowerShell/PowerShell) parser to generate an Abstract Syntax Tree (AST) from obfuscated scripts.
- **Node Analysis**: Traverses the AST to identify and address common obfuscation patterns such as string encoding, concatenation, and variable renaming.
- **Partial evaluation & semantic simplification**: Current techniques include:
  - **String Decryption**: Decodes obfuscated strings and replaces them with their cleartext equivalents.
  - **Variable Resolution**: Resolves obfuscated variable names to their original or more meaningful names.
  - **Expression Simplification**: Simplifies complex expressions to their basic forms.
  - **Command Resolution**: Replaces encoded or obfuscated command invocations with their original commands.
- **Reconstruction**: Reconstructs the script from the modified AST, producing a cleaned and readable version of the original script.
- **Output**: Provides the deobfuscated script in a readable format, ready for analysis.

## Command Line Interface (CLI) Options

PowderShell can be run from the command line with the following options:

### Options

- `-p, --path`: Specifies the path of the directory or script file(s) to be deobfuscated. You can provide multiple paths.
- `--stdin`: Indicates that the input should be read from the standard input (stdin).
- `-o, --output`: Specifies the output file or directory where the deobfuscated script(s) will be saved. If not provided, the output will be displayed in the console.

### Example Usage

#### Deobfuscate a Single Script File

To deobfuscate a single PowerShell script file and print the output to the console:

```sh
powdershell -p "path\to\obfuscated\script.ps1" -o "path\to\output\directory\result.ps1"
```

## Upcoming Features

### Partial Evaluation
PowderShell will soon incorporate partial evaluation features. This advanced technique involves executing parts of the script that are safe to run during the deobfuscation process. By evaluating static expressions and known values, the tool can further simplify the script, making it even more readable and accurate in revealing the script's true intent.

## How It Works

1. **AST Parsing**: The tool parses the obfuscated PowerShell script using the PowerShell SDK parser, generating an AST. This tree structure represents the script's syntax and semantics in a hierarchical manner.
   
2. **Node Analysis**: PowderShell traverses the AST nodes, identifying common obfuscation patterns such as string encoding, concatenation, and variable renaming.

3. ** Partial Evaluation, Semantic Simplification, Code Emulation **:
   - **String Decryption**: Identifies and decodes obfuscated strings, replacing them with their cleartext equivalents.
   - **Variable Resolution**: Resolves and replaces obfuscated variable names with their original or more meaningful names.
   - **Expression Simplification**: Simplifies complex expressions, often used to hide malicious intent, to their basic forms.
   - **Command Resolution**: Replaces encoded or obfuscated command invocations with their original commands.

4. **Reconstruction**: Once the obfuscation is removed, PowderShell reconstructs the script from the modified AST, producing a cleaned and readable version of the original script.

5. **Output**: The deobfuscated script is then outputted in a readable format, ready for analysis.

## Contact

For any questions or suggestions, please open an issue on GitHub

## License

This project is licensed under the MIT License - see the LICENSE file for details.