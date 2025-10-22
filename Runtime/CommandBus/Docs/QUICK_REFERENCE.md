# Command Bus Quick Reference

## 🚀 One-Page Cheat Sheet

### Basic Command

```csharp
public class MyCommand : ICommand
{
    public bool AllowParallelExecution => true;
    public CommandPriority Priority => CommandPriority.Normal;
}
```

### Command with Locking

```csharp
public class MyCommand : ICommand, ILockingCommand
{
    private readonly ICardEntity _entity;
    
    public object[] GetLockKeys() => new[] { _entity.InstanceId };
}
```

### Command with Validation

```csharp
public class MyCommand : ICommand, IValidatableCommand
{
    public ValidationResult Validate()
    {
        if (someCondition)
            return ValidationResult.Invalid("Error message");
        return ValidationResult.Valid();
    }
}
```

### Command with Progress

```csharp
public class MyCommand : ICommand, IProgressCommand
{
    private readonly ProgressReporter _progress = new();
    public IProgressReporter ProgressReporter => _progress;
}
```

### Handler

```csharp
public class MyHandler : ICommandHandler<MyCommand>
{
    public async UniTask<CommandResult> ExecuteAsync(
        MyCommand cmd, 
        CancellationToken ct)
    {
        try
        {
            // Your logic here
            return CommandResult.Success();
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ex);
        }
    }
}
```

### Register & Execute

```csharp
// Register (in installer)
bus.RegisterHandler(new MyHandler());

// Execute (in game code)
[Inject] private AsyncCommandBus _commandBus;

void DoSomething()
{
    _commandBus.Enqueue(new MyCommand());
}
```

### Priorities

```csharp
CommandPriority.Critical  // Save game, emergency
CommandPriority.High      // Combat, damage
CommandPriority.Normal    // Standard gameplay
CommandPriority.Low       // Visual effects
```

### Event Subscription

```csharp
[Inject] private CommandBusEvents _events;

void OnEnable()
{
    _events.OnCommandFailed += HandleFailed;
}

void HandleFailed(CommandEventArgs args)
{
    Debug.Log($"Failed: {args.Result.ErrorMessage}");
}
```

### Progress Bar

```csharp
if (command is IProgressCommand progressCmd)
{
    var bar = Instantiate(progressBarPrefab);
    bar.BindToCommand(progressCmd);
}
```

---

## 📋 Common Patterns

### Cook Pattern
```csharp
var cmd = new CookCommand(campfire, ingredient);
_commandBus.Enqueue(cmd);
```

### Attack Pattern
```csharp
var cmd = new AttackCommand(attacker, target);
_commandBus.Enqueue(cmd, CommandPriority.High);
```

### Stack Pattern
```csharp
var rule = new StackInteractionRule(
    CardType.Unit, CardType.Resource, "result_id");
var cmd = new StackCommand(baseCard, cardToStack, rule);
_commandBus.Enqueue(cmd);
```

### Move Pattern
```csharp
var cmd = new MoveCommand(card, targetPos, duration: 0.5f);
_commandBus.Enqueue(cmd, CommandPriority.Low);
```

---

## ⚠️ Common Mistakes

### ❌ DON'T
```csharp
// Don't put logic in commands
public class BadCommand : ICommand
{
    public void Execute() { /* logic */ }  // ❌
}

// Don't forget finally
cmd.Entity.SetBusy(true);
await Work();  // ❌ What if exception?

// Don't use static events
public static event Action OnFailed;  // ❌
```

### ✅ DO
```csharp
// Commands are data-only
public class GoodCommand : ICommand
{
    public ICardEntity Entity { get; }  // ✅
}

// Always use try/finally
try {
    cmd.Entity.SetBusy(true);
    await Work();
} finally {
    cmd.Entity.SetBusy(false);  // ✅
}

// Use instance events
[Inject] private CommandBusEvents _events;  // ✅
```

---

## 🐛 Debugging

### Enable Debug Logging
```csharp
// In CommandBusInstaller inspector
[x] Enable Debug Logging
```

### Console Output
```
[CommandBus] Enqueued: CookCommand
[CommandBus] Executing: CookCommand
[CommandBus] Completed: CookCommand
```

### Check Failure Reason
```csharp
_events.OnCommandFailed += args =>
{
    switch (args.Result.FailureReason)
    {
        case CommandFailureReason.EntityLocked:
            // Entity was busy
            break;
        case CommandFailureReason.ValidationFailed:
            // Validation failed
            break;
    }
};
```

---

## 📖 Full Documentation

- `README.md` - Start here
- `ARCHITECTURE.md` - Deep dive
- `MIGRATION_GUIDE.md` - How to migrate
- `DEFECTS_AND_IMPROVEMENTS.md` - What was fixed
- `IMPLEMENTATION_SUMMARY.md` - What was delivered

---

*Print this page for quick reference while coding*

