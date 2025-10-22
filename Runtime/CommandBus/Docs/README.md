# Stacklands-Style Command Bus System

## 🎮 What is this?

A **production-ready command bus system** for Unity, specifically designed for Stacklands-like card-based simulation games. Built on **SOLID principles** and **industry-standard patterns**.

---

## ✨ Features

- ✅ **Async/Await** - Using UniTask for zero-allocation async
- ✅ **Priority Queue** - Critical > High > Normal > Low
- ✅ **Parallel Execution** - Multiple commands run simultaneously
- ✅ **Entity Locking** - Prevents concurrent operations on same entity
- ✅ **Progress Reporting** - Built-in progress bars for long operations
- ✅ **Cancellation** - Stop commands mid-execution
- ✅ **Validation** - Pre-execution validation with user feedback
- ✅ **Event System** - Hook into command lifecycle
- ✅ **Dependency Injection** - Full Reflex DI support
- ✅ **SOLID Compliant** - Professional architecture
- ✅ **Memory Safe** - No leaks, proper cleanup
- ✅ **Thread Safe** - Concurrent access handled correctly

---

## 🚀 Quick Start

### 1. Install Dependencies

**UniTask** (required):

```
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```

**Reflex** (already in project):
You're using Reflex for DI, which is perfect.

### 2. Setup Scene

1. Open your game scene
2. Add `CommandBusInstaller` component to your installer GameObject
3. Reference it in your scene installer:

```csharp
public class GameSceneInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private CommandBusInstaller _commandBusInstaller;

    public void InstallBindings(ContainerBuilder builder)
    {
        _commandBusInstaller.InstallBindings(builder);
    }
}
```

### 3. Create Your First Command

```csharp
using com.aqua.command;
using com.aqua.command;

public class MyCookCommand : ICommand, ILockingCommand, IValidatableCommand
{
    private readonly ICardEntity _campfire;
    private readonly ICardEntity _meat;

    public MyCookCommand(ICardEntity campfire, ICardEntity meat)
    {
        _campfire = campfire;
        _meat = meat;
    }

    public object[] GetLockKeys() => new[] { _campfire, _meat };

    public ValidationResult Validate()
    {
        if (_campfire.IsBusy || _meat.IsBusy)
            return ValidationResult.Invalid("Entity is busy");
        return ValidationResult.Valid();
    }
}
```

### 4. Create Handler

```csharp
public class MyCookHandler : ICommandHandler<MyCookCommand>
{
    public async UniTask<CommandResult> ExecuteAsync(
        MyCookCommand cmd,
        CancellationToken ct)
    {
        cmd._campfire.SetBusy(true);
        cmd._meat.SetBusy(true);

        try
        {
            await UniTask.Delay(3000, cancellationToken: ct);
            Debug.Log("Cooking complete!");
            return CommandResult.Success();
        }
        finally
        {
            cmd._campfire.SetBusy(false);
            cmd._meat.SetBusy(false);
        }
    }
}
```

### 5. Register & Use

```csharp
// In installer
bus.RegisterHandler(new MyCookHandler());

// In game code
[Inject] private ICommandBus _commandBus;

void OnCardDroppedOnCampfire(ICardEntity meat)
{
    _commandBus.Enqueue(new MyCookCommand(campfire, meat));
}
```

---

## 📦 What's Included

### Core System

```
CommandBus/
├── Core/
│   ├── ICommand.cs              - Base interfaces
│   ├── AsyncCommandBus.cs       - Main command bus
│   ├── CommandResult.cs         - Result pattern
│   ├── CommandPriority.cs       - Priority enum
│   ├── CommandQueue.cs          - Priority queue
│   ├── EntityLockManager.cs     - Locking system
│   ├── IProgressReporter.cs     - Progress tracking
│   └── CommandBusEvents.cs      - Event system
```

### Stacklands Commands

```
├── StacklandsCommands/
│   ├── CardData.cs              - Card data model
│   ├── ICardEntity.cs           - Entity interfaces
│   ├── ICardFactory.cs          - Factory pattern
│   └── Commands/
│       ├── CookCommand.cs       - Cooking mechanic
│       ├── AttackCommand.cs     - Combat mechanic
│       ├── StackCommand.cs      - Card stacking
│       └── MoveCommand.cs       - Movement/animation
```

### Integration

```
├── Integration/
│   └── CommandBusInstaller.cs   - Reflex DI installer
```

### UI

```
├── UI/
│   ├── ProgressBarView.cs              - Progress bar
│   └── CommandFailedNotification.cs    - Error popups
```

### Examples

```
├── Examples/
│   ├── CardEntity.cs                   - Sample entity
│   ├── SimpleCardFactory.cs            - Sample factory
│   └── CardInteractionController.cs    - Sample usage
```

### Migration Tools

```
├── Migration/
│   ├── GameActionToCommandAdapter.cs   - Adapter pattern
│   └── HybridGameSceneInstaller.cs     - Hybrid installer
```

### Documentation

```
├── README.md                    - This file
├── ARCHITECTURE.md              - System design
├── DEFECTS_AND_IMPROVEMENTS.md  - What was fixed
└── MIGRATION_GUIDE.md           - How to migrate
```

---

## 📖 Documentation

### For Quick Reference

- **[README.md](./README.md)** ← You are here

### For Understanding the System

- **[ARCHITECTURE.md](./ARCHITECTURE.md)** - Deep dive into design, patterns, flow

### For Migrating from Old System

- **[MIGRATION_GUIDE.md](./MIGRATION_GUIDE.md)** - Step-by-step migration

### For Understanding Changes

- **[DEFECTS_AND_IMPROVEMENTS.md](./DEFECTS_AND_IMPROVEMENTS.md)** - 20 defects fixed

---

## 💡 Common Use Cases

### Cooking

```csharp
var cookCmd = new CookCommand(campfire, rawMeat);
_commandBus.Enqueue(cookCmd);
// → Shows progress bar
// → Creates cooked meat after 3s
// → Destroys raw meat
```

### Combat

```csharp
var attackCmd = new AttackCommand(villager, goblin);
_commandBus.Enqueue(attackCmd, CommandPriority.High);
// → Villager attacks goblin
// → Deals damage
// → Destroys goblin if HP <= 0
```

### Card Stacking

```csharp
var rule = new StackInteractionRule(
    CardType.Unit,
    CardType.Resource,
    "wood",
    processingTime: 2f
);
var stackCmd = new StackCommand(villager, tree, rule);
_commandBus.Enqueue(stackCmd);
// → Villager "processes" tree
// → Creates wood card after 2s
```

### Simple Movement

```csharp
var moveCmd = new MoveCommand(card, targetPos, duration: 0.5f);
_commandBus.Enqueue(moveCmd, CommandPriority.Low);
// → Smoothly animates card to position
```

---

## 🎯 SOLID Principles

### Single Responsibility

- **Commands**: Data only
- **Handlers**: Logic only
- **CommandBus**: Routing only

### Open/Closed

- Add new commands without modifying bus
- Extend via interfaces, not modification

### Liskov Substitution

- All `ICommand` work in bus
- All `ICommandHandler<T>` are interchangeable

### Interface Segregation

- Small focused interfaces
- Commands only implement what they need

### Dependency Inversion

- Depend on abstractions (interfaces)
- Inject dependencies via constructor

---

## 🧪 Testing

### Unit Test Example

```csharp
[Test]
public async Task CookCommand_ValidInput_Success()
{
    // Arrange
    var mockFactory = new Mock<ICardFactory>();
    var handler = new CookCommandHandler(mockFactory.Object);
    var cmd = new CookCommand(station, ingredient);

    // Act
    var result = await handler.ExecuteAsync(cmd, CancellationToken.None);

    // Assert
    Assert.IsTrue(result.IsSuccess);
}
```

---

## 🔍 Debugging

### Enable Debug Logging

In `CommandBusInstaller`, check "Enable Debug Logging":

```
[CommandBus] Enqueued: CookCommand
[CommandBus] Executing: CookCommand
[CommandBus] Completed: CookCommand
```

### Check Console for Failures

```
[CommandBus] Failed: CookCommand - Entity is busy (EntityLocked)
```

### Profile Memory

Use Unity Profiler to verify no leaks:

- Progress reporters cleaned up
- Event subscribers removed
- Commands garbage collected

---

## ⚠️ Common Pitfalls

### ❌ DON'T put logic in commands

```csharp
// Bad
public class CookCommand : ICommand
{
    public void Cook() { /* logic here */ }  // ❌
}
```

### ✅ DO put logic in handlers

```csharp
// Good
public class CookCommandHandler : ICommandHandler<CookCommand>
{
    public async UniTask<CommandResult> ExecuteAsync(...)
    {
        // Logic here ✅
    }
}
```

### ❌ DON'T forget to release locks

```csharp
// Bad
cmd.Entity.SetBusy(true);
await DoWork();
// Forgot to SetBusy(false) ❌
```

### ✅ DO use try/finally

```csharp
// Good
try
{
    cmd.Entity.SetBusy(true);
    await DoWork();
}
finally
{
    cmd.Entity.SetBusy(false);  // ✅ Always releases
}
```

---

## 🚀 Performance Tips

### 1. Cache Lock Keys

```csharp
private object[] _lockKeys;
public object[] GetLockKeys() => _lockKeys ??= new[] { _entity };
```

### 2. Use Parallel Execution

```csharp
public bool AllowParallelExecution => true;  // Multiple can run
```

### 3. Set Appropriate Priority

```csharp
public CommandPriority Priority => CommandPriority.Low;  // Non-critical
```

### 4. Clear Progress Subscribers

```csharp
// Handled automatically by bus, but good to know
if (reporter is ProgressReporter pr)
    pr.ClearSubscribers();
```

---

## 📞 Support

### Issues Found?

Check [DEFECTS_AND_IMPROVEMENTS.md](./DEFECTS_AND_IMPROVEMENTS.md) to see if it's a known issue.

### Need Help?

Refer to [ARCHITECTURE.md](./ARCHITECTURE.md) for deep dive.

### Migrating?

Follow [MIGRATION_GUIDE.md](./MIGRATION_GUIDE.md) step-by-step.

---

## 📜 License

This is a custom implementation for your project. Use as needed.

---

## 🎓 Learning Resources

- [Command Pattern](https://refactoring.guru/design-patterns/command)
- [Mediator Pattern](https://refactoring.guru/design-patterns/mediator)
- [Result Pattern](https://enterprisecraftsmanship.com/posts/error-handling-exception-or-result/)
- [UniTask Documentation](https://github.com/Cysharp/UniTask)

---

**Version**: 1.0.0  
**Last Updated**: 2025-10-09  
**Status**: ✅ Production Ready

---

_Built with ❤️ following SOLID principles and industry standards_
