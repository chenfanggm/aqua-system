# Command Bus System Architecture

## 📐 Overview

This is a production-ready command bus system designed for Stacklands-like games, built on SOLID principles and industry-standard patterns.

## 🎯 Design Principles

### SOLID Compliance

#### 1. Single Responsibility Principle (SRP)
- **Commands**: Only represent user intent (data-only)
- **Handlers**: Only contain execution logic for one command type
- **CommandBus**: Only coordinates command routing and execution
- **LockManager**: Only manages entity locking
- **Queue**: Only manages command prioritization

#### 2. Open/Closed Principle (OCP)
- **Extensible**: Add new commands/handlers without modifying core bus
- **Closed to modification**: Core infrastructure is stable and unchanging
- **Example**: Adding `CraftCommand` requires only creating the command and handler, no changes to `AsyncCommandBus`

#### 3. Liskov Substitution Principle (LSP)
- All `ICommand` implementations work seamlessly in the bus
- All `ICommandHandler<T>` implementations are interchangeable
- Mock implementations can replace real ones for testing

#### 4. Interface Segregation Principle (ISP)
- Small, focused interfaces: `ILockingCommand`, `IProgressCommand`, `ICancellableCommand`, `IValidatableCommand`
- Commands only implement what they need
- Clients depend on minimal interfaces

#### 5. Dependency Inversion Principle (DIP)
- High-level modules (CommandBus) depend on abstractions (ICommand, ICommandHandler)
- Low-level modules (concrete handlers) implement abstractions
- Dependencies injected via constructor (DI-friendly)

---

## 🏗️ Architecture Layers

```
┌─────────────────────────────────────────────┐
│           Presentation Layer (UI)           │
│  ProgressBarView, CommandFailedNotification │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│         Application Layer (Commands)        │
│  CookCommand, AttackCommand, StackCommand   │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│        Domain Layer (Command Bus)           │
│   AsyncCommandBus, Handlers, EntityLocks    │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│       Infrastructure Layer (Unity)          │
│     UniTask, Reflex DI, MonoBehaviours      │
└─────────────────────────────────────────────┘
```

---

## 🔄 Execution Flow

### Command Lifecycle

```
1. User Action (drag card onto campfire)
   ↓
2. Create Command
   var cmd = new CookCommand(campfire, meat);
   ↓
3. Enqueue Command
   commandBus.Enqueue(cmd);
   ↓
4. Validation (if IValidatableCommand)
   cmd.Validate() → Valid/Invalid
   ↓
5. Lock Acquisition
   lockManager.TryAcquireLocks(campfire, meat)
   ↓
6. Execute Handler
   handler.ExecuteAsync(cmd, cancellationToken)
   ↓
7. Progress Updates (if IProgressCommand)
   progressReporter.SetProgress(0.5f)
   ↓
8. Complete/Fail
   events.RaiseCompleted/Failed(cmd, result)
   ↓
9. Release Locks
   lockManager.ReleaseLocks(campfire, meat)
```

### Priority Queue Processing

```
Queue: [Critical₀, High₁, Normal₂, Low₃]
       ↓
    Dequeue highest priority
       ↓
    Check locks
       ↓
    ┌─────────────┐
    │ Parallel?   │
    └──┬────────┬─┘
       │        │
    Yes│        │No
       ↓        ↓
   Run async   Await
       │        │
       └────┬───┘
            ↓
     Process next
```

---

## 🔐 Entity Locking System

### Composite Locks

Supports locking multiple entities atomically:

```csharp
// Single lock
public object[] GetLockKeys() => new[] { _attacker };

// Composite lock (both must be free)
public object[] GetLockKeys() => new[] { _cookingStation, _ingredient };
```

### Lock Guarantees

- **Atomic**: All locks acquired or none
- **Deadlock-free**: Ordered acquisition (alphabetical by key hash)
- **Thread-safe**: Lock manager uses internal synchronization
- **Fair**: FIFO for same priority commands

---

## 📊 Key Components

### Core Interfaces

| Interface | Purpose | Required? |
|-----------|---------|-----------|
| `ICommand` | Base command marker | ✅ Always |
| `ILockingCommand` | Needs entity locks | ⚠️ If modifies entities |
| `IProgressCommand` | Long-running with progress | ⚠️ If >1s duration |
| `ICancellableCommand` | Can be cancelled | ⚠️ If user can abort |
| `IValidatableCommand` | Pre-execution validation | ⚠️ If has preconditions |

### Event System

Instead of static events (bad), we use instance-based events:

```csharp
// ❌ Bad (old system)
public static event Action<GameAction> OnActionCompleted;

// ✅ Good (new system)
public class CommandBusEvents : ICommandBusEvents
{
    public event Action<CommandEventArgs> OnCommandCompleted;
}
```

**Benefits:**
- Testable (can mock)
- No memory leaks from forgotten unsubscribes
- Multiple command buses can coexist
- Proper cleanup via `Dispose()`

---

## 🎮 Stacklands-Specific Commands

### CookCommand

**Purpose**: Cook ingredient on cooking station

**Locks**: Cooking station + ingredient

**Progress**: Reports 0% → 100% over cooking duration

**Validation**:
- Station must be `CardType.Structure`
- Ingredient must be cookable (`IsCookable == true`)
- Neither can be busy

**Handler**: Creates cooked result, destroys ingredient

---

### AttackCommand

**Purpose**: One card attacks another

**Locks**: Attacker + target

**Priority**: `High` (combat is important)

**Validation**:
- Both entities alive
- Attacker has attack power
- Neither busy

**Handler**: Applies damage, destroys if health ≤ 0

---

### StackCommand

**Purpose**: Stack card on another, trigger interactions

**Locks**: Base card + card to stack

**Interaction Rules**: Defines what happens (e.g., Villager + Tree → Wood)

**Validation**:
- Base card accepts stacking
- Neither busy

**Handler**: Processes interaction or simple stacking

---

### MoveCommand

**Purpose**: Animate card movement

**Locks**: Card being moved

**Priority**: `Low` (visual, non-critical)

**Cancellable**: Yes, snaps back to original position

**Handler**: Lerps position over duration

---

## 🔧 Integration with Unity/Reflex

### Installer Pattern

```csharp
public class CommandBusInstaller : MonoBehaviour, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        // Register services
        builder.AddSingleton<ICommandQueue, CommandQueue>();
        builder.AddSingleton<IEntityLockManager, EntityLockManager>();
        builder.AddSingleton<CommandBusEvents>();
        
        // Register command bus
        builder.AddSingleton<ICommandBus>(container => {
            var bus = new AsyncCommandBus(/*...*/);
            RegisterHandlers(bus, container);
            return bus;
        });
    }
}
```

### Usage in MonoBehaviours

```csharp
public class CardController : MonoBehaviour
{
    [Inject] private ICommandBus _commandBus;
    
    private void OnCardDroppedOnCampfire(Card ingredient)
    {
        var cmd = new CookCommand(campfire, ingredient);
        _commandBus.Enqueue(cmd);
    }
}
```

---

## 🚨 Error Handling Strategy

### No Exceptions for Expected Failures

We use `CommandResult` instead of throwing exceptions:

```csharp
// ❌ Bad
if (entity.IsBusy)
    throw new InvalidOperationException("Entity is busy");

// ✅ Good
if (entity.IsBusy)
    return CommandResult.EntityLocked("Entity is busy");
```

**Why?**
- Exceptions are for **unexpected** errors
- Entity being busy is **expected** and common
- Result pattern is more performant
- Easier to test

### Exception Handling

Only catch exceptions for **unexpected** errors:

```csharp
try
{
    await handler.ExecuteAsync(cmd, ct);
}
catch (OperationCanceledException)
{
    // Expected when cancelling
    throw;
}
catch (Exception ex)
{
    // Unexpected error
    Debug.LogException(ex);
    return CommandResult.Failure(ex);
}
```

---

## 🧪 Testing Strategy

### Unit Tests

**Commands** (pure data):
```csharp
[Test]
public void CookCommand_GetLockKeys_ReturnsBothEntities()
{
    var cmd = new CookCommand(station, ingredient);
    var keys = cmd.GetLockKeys();
    Assert.AreEqual(2, keys.Length);
}
```

**Handlers** (business logic):
```csharp
[Test]
public async Task CookHandler_ExecuteAsync_CreatesCorrectResult()
{
    var handler = new CookCommandHandler(mockFactory);
    var cmd = new CookCommand(station, ingredient);
    
    var result = await handler.ExecuteAsync(cmd, CancellationToken.None);
    
    Assert.IsTrue(result.IsSuccess);
    mockFactory.Verify(f => f.CreateCard("cooked_meat"));
}
```

### Integration Tests

**Command Bus**:
```csharp
[Test]
public async Task CommandBus_Enqueue_ExecutesCommand()
{
    var bus = new AsyncCommandBus();
    var handler = new MockHandler();
    bus.RegisterHandler(handler);
    
    bus.Enqueue(new MockCommand());
    await UniTask.Delay(100);
    
    Assert.AreEqual(1, handler.ExecutedCount);
}
```

---

## 📈 Performance Considerations

### Memory Management

- Commands are **transient** (created, executed, GC'd)
- Handlers are **singleton** (registered once, reused)
- Progress reporters **clear subscribers** on completion (prevent leaks)

### Allocation Optimization

```csharp
// ✅ Allocate array once
private object[] _lockKeys;
public object[] GetLockKeys() => _lockKeys ??= new[] { _card };

// ❌ Allocates every call
public object[] GetLockKeys() => new[] { _card };
```

### Async Performance

- Uses `UniTask` (zero-allocation async)
- `UniTask.Yield()` instead of `await Task.Delay(0)`
- Cancellation tokens to stop work early

---

## 🔄 Migration from ActionSystem

### Phase 1: Hybrid (Both Systems)

```csharp
// Old code still works
actionSystem.Perform(new DrawCardAction());

// New code uses commands
commandBus.Enqueue(new DrawCardCommand());
```

### Phase 2: Adapter Pattern

```csharp
public class GameActionToCommandAdapter
{
    public void Execute(GameAction action)
    {
        var cmd = ConvertToCommand(action);
        _commandBus.Enqueue(cmd);
    }
}
```

### Phase 3: Full Migration

Remove `ActionSystem`, use only `CommandBus`.

---

## 📚 Further Reading

- [Command Pattern (GoF)](https://refactoring.guru/design-patterns/command)
- [Mediator Pattern](https://refactoring.guru/design-patterns/mediator)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [Result Pattern](https://enterprisecraftsmanship.com/posts/error-handling-exception-or-result/)

---

## 🎓 Best Practices

### DO ✅

- Keep commands **immutable** (readonly fields)
- Keep handlers **stateless** (no instance state)
- Use **dependency injection** for handler dependencies
- Implement `IValidatableCommand` for preconditions
- Use `CommandResult` for expected failures
- Clear progress reporter subscribers on completion

### DON'T ❌

- Put business logic in commands (use handlers)
- Use static events (use instance-based)
- Lock entities manually (use `ILockingCommand`)
- Throw exceptions for expected failures
- Forget to release locks (always use try/finally)
- Create long-lived command instances

---

*Last updated: 2025-10-09*

