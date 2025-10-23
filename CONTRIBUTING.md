# Contributing to Aqua Command System

Thank you for your interest in contributing to the Aqua Command System! This document provides guidelines and instructions for contributing.

---

## Table of Contents

1. [Code of Conduct](#code-of-conduct)
2. [How Can I Contribute?](#how-can-i-contribute)
3. [Development Setup](#development-setup)
4. [Coding Guidelines](#coding-guidelines)
5. [Testing Guidelines](#testing-guidelines)
6. [Documentation Guidelines](#documentation-guidelines)
7. [Pull Request Process](#pull-request-process)
8. [Issue Guidelines](#issue-guidelines)

---

## Code of Conduct

### Our Standards

- **Be respectful**: Treat everyone with respect and kindness
- **Be constructive**: Provide helpful feedback and suggestions
- **Be patient**: Help newcomers learn and grow
- **Be inclusive**: Welcome contributors from all backgrounds
- **Be professional**: Keep discussions focused and productive

### Unacceptable Behavior

- Harassment, discrimination, or personal attacks
- Trolling, insulting, or derogatory comments
- Spam or off-topic content
- Publishing others' private information
- Other unprofessional conduct

---

## How Can I Contribute?

### Reporting Bugs

Before creating a bug report:
1. **Check existing issues** to avoid duplicates
2. **Verify it's a bug** and not a configuration issue
3. **Test with the latest version**

Create a bug report with:
```markdown
**Description**
Clear description of the bug

**Steps to Reproduce**
1. Step one
2. Step two
3. ...

**Expected Behavior**
What should happen

**Actual Behavior**
What actually happens

**Environment**
- Unity Version: 6000.2
- Package Version: 1.0.0
- Platform: Windows/Mac/Linux
- .NET Version: Standard 2.1

**Additional Context**
Code samples, screenshots, logs
```

### Suggesting Enhancements

Enhancement suggestions are welcome! Please include:

- **Clear title**: Concise feature description
- **Use case**: Why this feature is needed
- **Implementation ideas**: How it might work
- **Alternatives**: Other approaches considered
- **Examples**: Code samples or mockups

### Code Contributions

We welcome pull requests for:
- Bug fixes
- Performance improvements
- New features (discuss first via issue)
- Documentation improvements
- Test coverage improvements

---

## Development Setup

### Prerequisites

- Unity 6000.2 or later (2021.3+ may work with modifications)
- Git
- Code editor (VS Code, Rider, Visual Studio)
- UniTask package

### Setup Steps

1. **Fork the repository**
   ```bash
   # Fork via GitHub UI, then clone
   git clone https://github.com/YOUR_USERNAME/aqua-command.git
   cd aqua-command
   ```

2. **Open in Unity**
   - Open Unity Hub
   - Add project from disk
   - Open project

3. **Install dependencies**
   - UniTask should auto-install via UPM

4. **Create a branch**
   ```bash
   git checkout -b feature/my-feature
   ```

5. **Make your changes**
   - Write code
   - Add tests
   - Update documentation

6. **Test your changes**
   - Run all tests
   - Test in Unity Editor
   - Check for compilation errors

---

## Coding Guidelines

### C# Style Guide

#### Naming Conventions

```csharp
// Classes, Interfaces, Structs, Enums: PascalCase
public class MyClass { }
public interface IMyInterface { }
public struct MyStruct { }
public enum MyEnum { }

// Methods, Properties, Events: PascalCase
public void DoSomething() { }
public int MyProperty { get; set; }
public event Action MyEvent;

// Private fields: _camelCase with underscore
private int _myField;
private readonly string _myReadonlyField;

// Local variables, parameters: camelCase
public void Method(int parameterName)
{
    var localVariable = 10;
}

// Constants: PascalCase or UPPER_CASE
private const int MaxRetries = 3;
private const int DEFAULT_TIMEOUT = 30;
```

#### Code Formatting

```csharp
// Braces: Always on new line (Allman style)
if (condition)
{
    DoSomething();
}

// Spacing: One space after keywords
if (condition)
while (running)
for (int i = 0; i < count; i++)

// Indentation: 4 spaces (no tabs)
public class MyClass
{
    public void Method()
    {
        if (condition)
        {
            DoSomething();
        }
    }
}

// Line length: Max 120 characters
// Break long lines logically
var result = await _service.DoSomethingAsync(
    parameter1,
    parameter2,
    parameter3
);
```

#### XML Documentation

All public APIs must have XML documentation:

```csharp
/// <summary>
/// Executes the command asynchronously.
/// </summary>
/// <param name="command">The command to execute</param>
/// <param name="cancellationToken">Token to cancel execution</param>
/// <returns>Result of the command execution</returns>
/// <exception cref="ArgumentNullException">Thrown when command is null</exception>
public async UniTask<CommandResult> ExecuteAsync(
    ICommand command,
    CancellationToken cancellationToken)
{
    // Implementation
}
```

### Architecture Guidelines

#### SOLID Principles

✅ **Single Responsibility**
```csharp
// Good: One responsibility
public class CommandValidator
{
    public ValidationResult Validate(ICommand command) { }
}

// Bad: Multiple responsibilities
public class CommandProcessor
{
    public ValidationResult Validate(ICommand command) { }
    public void Execute(ICommand command) { }
    public void Log(string message) { }
    public void SendMetrics() { }
}
```

✅ **Open/Closed**
```csharp
// Good: Extend through interfaces
public interface ICommandInterceptor { }
public class LoggingInterceptor : ICommandInterceptor { }

// Bad: Modifying existing code for new features
public class CommandBus
{
    public bool EnableLogging { get; set; }
    public bool EnableMetrics { get; set; }
    public bool EnableProfiling { get; set; }
}
```

✅ **Liskov Substitution**
```csharp
// Good: All implementations work the same way
public interface ICommandHandler<T>
{
    UniTask<CommandResult> ExecuteAsync(T command, CancellationToken ct);
}

// Bad: Different implementations behave differently
public interface ICommandHandler<T>
{
    // Some return void, some return result
    object ExecuteAsync(T command);
}
```

✅ **Interface Segregation**
```csharp
// Good: Focused interfaces
public interface ICommand { }
public interface ICancellableCommand : ICommand
{
    void OnCancelled();
}
public interface ILockingCommand : ICommand
{
    object[] GetLockKeys();
}

// Bad: Fat interface
public interface ICommand
{
    void OnCancelled(); // Not all commands need this
    object[] GetLockKeys(); // Not all commands need this
    int Priority { get; } // Not all commands need this
}
```

✅ **Dependency Inversion**
```csharp
// Good: Depend on abstractions
public class CommandHandler
{
    private readonly ILogger _logger;
    private readonly IMetrics _metrics;
}

// Bad: Depend on concrete classes
public class CommandHandler
{
    private readonly UnityLogger _logger;
    private readonly PerformanceMetrics _metrics;
}
```

#### Design Patterns

**Use established patterns:**
- Command Pattern (core)
- Mediator Pattern (command bus)
- Observer Pattern (events)
- Result Pattern (error handling)
- Factory Pattern (command creation)
- Decorator Pattern (command wrappers)

---

## Testing Guidelines

### Unit Tests

```csharp
using NUnit.Framework;

[TestFixture]
public class CommandHandlerTests
{
    private TestCommandBus _commandBus;
    private MockService _mockService;
    private MyCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _commandBus = new TestCommandBus();
        _mockService = new MockService();
        _handler = new MyCommandHandler(_mockService);
    }

    [Test]
    public async Task ExecuteAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = new MyCommand(validParam: 10);

        // Act
        var result = await _handler.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _mockService.AssertMethodCalled();
    }

    [Test]
    public void Validate_InvalidCommand_ReturnsFalse()
    {
        // Arrange
        var command = new MyCommand(validParam: -1);

        // Act
        var result = _handler.Validate(command);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("Parameter must be positive", result.ErrorMessage);
    }

    [TearDown]
    public void TearDown()
    {
        _commandBus?.Dispose();
    }
}
```

### Test Coverage

Aim for:
- **80%+ code coverage** for core functionality
- **100% coverage** for public APIs
- **Edge cases** covered (null, empty, invalid inputs)
- **Async behavior** tested with cancellation
- **Thread safety** tested with concurrent operations

---

## Documentation Guidelines

### Code Comments

```csharp
// Good: Explain WHY, not WHAT
// Use exponential backoff to prevent server overload during retry
await UniTask.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));

// Bad: Redundant comment
// Increment i
i++;
```

### README Updates

When adding features, update:
- Quick start guide (if API changed)
- API reference (new methods/classes)
- Examples (if usage changed)
- Best practices (if patterns changed)

### Example Code

All examples must:
- **Compile without errors**
- **Follow coding guidelines**
- **Be self-contained** (minimal dependencies)
- **Include comments** explaining key concepts
- **Show best practices**

---

## Pull Request Process

### Before Submitting

- [ ] Code follows style guidelines
- [ ] All tests pass
- [ ] New tests added for new features
- [ ] Documentation updated
- [ ] No compilation warnings
- [ ] Commit messages are clear

### PR Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests added/updated
- [ ] Manual testing completed
- [ ] All tests pass

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-reviewed code
- [ ] Commented complex code
- [ ] Documentation updated
- [ ] No new warnings

## Related Issues
Fixes #123
```

### Review Process

1. **Automated checks** run (compilation, tests)
2. **Maintainer review** (code quality, design)
3. **Feedback addressed**
4. **Approved and merged**

### Merge Criteria

PRs must:
- Pass all automated tests
- Have no merge conflicts
- Be approved by at least one maintainer
- Follow coding guidelines
- Include tests for new features
- Update relevant documentation

---

## Issue Guidelines

### Bug Reports

Use the bug report template and include:
- Clear description
- Steps to reproduce
- Expected vs actual behavior
- Environment details
- Code samples

### Feature Requests

Use the feature request template and include:
- Problem statement
- Proposed solution
- Alternatives considered
- Use cases
- Breaking changes (if any)

### Questions

For questions:
- Check existing documentation first
- Search existing issues
- Provide context and code samples
- Be specific about what you're trying to achieve

---

## Recognition

Contributors are recognized in:
- CHANGELOG.md (major contributions)
- README.md credits section
- Release notes

Thank you for contributing! 🎉

---

## Contact

- **Email**: aqua@aqua.com
- **Website**: https://www.aqua.com
- **Issues**: [GitHub Issues](https://github.com/aqua/aqua-command/issues)

---

**Last Updated**: 2025-10-23

