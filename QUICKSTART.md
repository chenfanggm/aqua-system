# Quick Start Guide - Aqua Command System

Get up and running with the Aqua Command System in **5 minutes**!

---

## 1. Installation (1 minute)

### Option A: Unity Package Manager (Recommended)
1. Open `Window > Package Manager`
2. Click `+` → `Add package from git URL`
3. Enter: `https://github.com/yourusername/aqua-command.git`
4. Wait for import to complete

### Option B: Local Installation
1. Copy `aqua-command` folder to your project's `Assets/Packages/`
2. Unity will auto-import
3. Done!

---

## 2. Your First Command (2 minutes)

### Step 1: Create a Command
Create a new file `MovePlayerCommand.cs`:

```csharp
using UnityEngine;
using com.aqua.command;

public class MovePlayerCommand : ICommand
{
    public readonly int PlayerId;
    public readonly Vector3 Position;

    public MovePlayerCommand(int playerId, Vector3 position)
    {
        PlayerId = playerId;
        Position = position;
    }
}
```

### Step 2: Create a Handler
Create a new file `MovePlayerHandler.cs`:

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using com.aqua.command;

public class MovePlayerHandler : CommandHandlerBase<MovePlayerCommand>
{
    public override async UniTask<CommandResult> ExecuteAsync(
        MovePlayerCommand command,
        CancellationToken cancellationToken)
    {
        Debug.Log($"Moving player {command.PlayerId} to {command.Position}");
        
        // Your game logic here
        await UniTask.Delay(100); // Simulate work
        
        return CommandResult.Success();
    }
}
```

---

## 3. Setup Command Bus (1 minute)

Create a new file `GameManager.cs`:

```csharp
using UnityEngine;
using com.aqua.command;

public class GameManager : MonoBehaviour
{
    private AsyncCommandBus _commandBus;

    private void Awake()
    {
        // Create command bus
        _commandBus = new AsyncCommandBus();

        // Register handler
        _commandBus.RegisterHandler(new MovePlayerHandler());

        // Subscribe to events (optional)
        _commandBus.Events.OnCommandCompleted += args =>
        {
            Debug.Log("✓ Command completed!");
        };
    }

    private void Update()
    {
        // Test: Press SPACE to move player
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var command = new MovePlayerCommand(
                playerId: 1,
                position: new Vector3(10, 0, 10)
            );
            _commandBus.Enqueue(command);
        }
    }

    private void OnDestroy()
    {
        _commandBus?.Dispose();
    }
}
```

---

## 4. Test It! (1 minute)

1. Attach `GameManager` to a GameObject in your scene
2. Press **Play**
3. Press **SPACE**
4. Check Console for output:
   ```
   Moving player 1 to (10.0, 0.0, 10.0)
   ✓ Command completed!
   ```

🎉 **Congratulations!** You've successfully set up the command system!

---

## Next Steps

### Add Validation

```csharp
public class MovePlayerHandler : CommandHandlerBase<MovePlayerCommand>
{
    public override ValidationResult Validate(MovePlayerCommand command)
    {
        if (command.PlayerId <= 0)
            return ValidationResult.Invalid("Invalid player ID");

        if (command.Position.y < 0)
            return ValidationResult.Invalid("Cannot move underground");

        return ValidationResult.Valid();
    }

    // ... ExecuteAsync
}
```

### Add Cancellation

```csharp
public class MovePlayerCommand : ICommand, ICancellableCommand
{
    // ... existing properties

    public void OnCancelled()
    {
        Debug.Log($"Movement cancelled for player {PlayerId}");
    }
}
```

### Add Entity Locking

```csharp
public class MovePlayerCommand : ICommand, ILockingCommand
{
    // ... existing properties

    public object[] GetLockKeys()
    {
        return new object[] { PlayerId }; // Lock player during move
    }
}
```

---

## Common Patterns

### Pattern 1: UI Button

```csharp
public class AttackButton : MonoBehaviour
{
    [SerializeField] private AsyncCommandBus _commandBus;

    public void OnClick()
    {
        var command = new AttackCommand(playerId: 1, targetId: 2);
        _commandBus.Enqueue(command);
    }
}
```

### Pattern 2: Async Execution

```csharp
public override async UniTask<CommandResult> ExecuteAsync(
    MyCommand command,
    CancellationToken ct)
{
    // Do async work
    await LoadResourceAsync();
    await ProcessDataAsync();
    await SaveResultAsync();

    return CommandResult.Success();
}
```

### Pattern 3: Error Handling

```csharp
public override async UniTask<CommandResult> ExecuteAsync(
    MyCommand command,
    CancellationToken ct)
{
    try
    {
        await DoSomething();
        return CommandResult.Success();
    }
    catch (InvalidOperationException)
    {
        return CommandResult.Failure(
            "Operation failed",
            CommandFailureReason.ValidationFailed
        );
    }
    catch (Exception ex)
    {
        return CommandResult.Failure(ex);
    }
}
```

---

## Troubleshooting

### "Handler not found" Error
**Problem**: Command is enqueued but nothing happens  
**Solution**: Make sure you registered the handler:
```csharp
_commandBus.RegisterHandler(new MyCommandHandler());
```

### "Validation failed" Error
**Problem**: Command validation fails  
**Solution**: Check your `Validate()` method logic:
```csharp
public override ValidationResult Validate(MyCommand command)
{
    // Add debug logging
    Debug.Log($"Validating: {command}");
    
    if (someCondition)
        return ValidationResult.Invalid("Reason here");
    
    return ValidationResult.Valid();
}
```

### Commands Not Executing
**Problem**: Commands stuck in queue  
**Solution**: Check if previous command is blocking:
```csharp
// Make sure long-running commands allow parallel execution
public class LongCommand : ICommand
{
    public bool AllowParallelExecution => true; // Don't block queue
}
```

---

## Full Example: Combat System

```csharp
// 1. Command
public class AttackCommand : ICommand, ILockingCommand
{
    public readonly int AttackerId;
    public readonly int TargetId;

    public AttackCommand(int attackerId, int targetId)
    {
        AttackerId = attackerId;
        TargetId = targetId;
    }

    public object[] GetLockKeys() => new object[] { AttackerId, TargetId };
}

// 2. Handler
public class AttackHandler : CommandHandlerBase<AttackCommand>
{
    public override ValidationResult Validate(AttackCommand command)
    {
        // Check if units exist and are alive
        if (!IsUnitAlive(command.AttackerId))
            return ValidationResult.Invalid("Attacker is dead");
        if (!IsUnitAlive(command.TargetId))
            return ValidationResult.Invalid("Target is dead");

        return ValidationResult.Valid();
    }

    public override async UniTask<CommandResult> ExecuteAsync(
        AttackCommand command,
        CancellationToken ct)
    {
        // Play animation
        await PlayAttackAnimation(command.AttackerId);
        
        // Apply damage
        ApplyDamage(command.TargetId, 10);
        
        return CommandResult.Success();
    }
}

// 3. Usage
public class CombatManager : MonoBehaviour
{
    private AsyncCommandBus _commandBus;

    private void Awake()
    {
        _commandBus = new AsyncCommandBus();
        _commandBus.RegisterHandler(new AttackHandler());
    }

    public void PlayerAttack(int targetId)
    {
        _commandBus.Enqueue(new AttackCommand(attackerId: 1, targetId));
    }
}
```

---

## What's Next?

📖 **Full Documentation**: [README.md](README.md)  
💡 **More Examples**: [EXAMPLES.md](EXAMPLES.md)  
🏗️ **Architecture Guide**: [ARCHITECTURE_IMPROVEMENTS.md](ARCHITECTURE_IMPROVEMENTS.md)  
🤝 **Contributing**: [CONTRIBUTING.md](CONTRIBUTING.md)

---

## Cheat Sheet

```csharp
// Create command bus
var bus = new AsyncCommandBus();

// Register handler
bus.RegisterHandler(new MyHandler());

// Enqueue command
bus.Enqueue(new MyCommand());

// Subscribe to events
bus.Events.OnCommandCompleted += args => { };
bus.Events.OnCommandFailed += args => { };

// Cancel commands
bus.TryCancel(command);  // Cancel specific
bus.CancelAll();          // Cancel all

// Check status
int queued = bus.QueuedCommandCount;
int executing = bus.ExecutingCommandCount;

// Cleanup
bus.Dispose();
```

---

## Help & Support

- 📚 **Docs**: Check [README.md](README.md)
- 💬 **Issues**: [GitHub Issues](https://github.com/aqua/aqua-command/issues)
- 📧 **Email**: aqua@aqua.com

---

**Time to build something awesome!** 🚀

