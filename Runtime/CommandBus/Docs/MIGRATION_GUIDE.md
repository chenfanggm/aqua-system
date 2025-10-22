# Migration Guide: ActionSystem → CommandBus

## 📋 Overview

This guide helps you migrate from the old `ActionSystem` to the new `AsyncCommandBus` system.

---

## 🔄 Migration Strategies

### Strategy 1: Hybrid Approach (Recommended)

Keep both systems running during transition period.

**Timeline**: 2-4 weeks

**Steps**:
1. Install command bus alongside action system
2. New features use command bus
3. Gradually convert existing features
4. Remove action system when complete

### Strategy 2: Big Bang Migration

Replace entire system at once.

**Timeline**: 1 week intensive work

**Only use if**: Small codebase, short deadline, full team availability

---

## 📝 Step-by-Step Migration

### Phase 1: Setup (1 day)

#### 1.1 Add UniTask Package

```bash
# Via Package Manager
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```

Or add to `manifest.json`:
```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

#### 1.2 Update GameSceneInstaller

**Before:**
```csharp
public class GameSceneInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private ActionSystem _actionSystem;
    
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.AddSingleton(_actionSystem);
    }
}
```

**After:**
```csharp
public class GameSceneInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private ActionSystem _actionSystem;  // Keep temporarily
    [SerializeField] private CommandBusInstaller _commandBusInstaller;
    
    public void InstallBindings(ContainerBuilder builder)
    {
        // Old system (will remove later)
        builder.AddSingleton(_actionSystem);
        
        // New system
        _commandBusInstaller.InstallBindings(builder);
    }
}
```

#### 1.3 Attach CommandBusInstaller

1. Open your main game scene
2. Find the GameObject with `GameSceneInstaller`
3. Add `CommandBusInstaller` component
4. Enable "Debug Logging" for initial testing

---

### Phase 2: Create Card Infrastructure (2 days)

#### 2.1 Migrate Card Class

**Before:**
```csharp
public class Card : MonoBehaviour
{
    // Tightly coupled, does everything
}
```

**After:**
```csharp
// Separate data from entity
public class CardData { /* pure data */ }

public interface ICardEntity { /* entity contract */ }

public class CardEntity : MonoBehaviour, IStackableCard
{
    // Implements interface, delegates to commands
}
```

**Migration Steps**:

1. Create `CardData` for each card type:

```csharp
var campfireData = new CardData("campfire_01", "Campfire", CardType.Structure);
var meatData = new CardData("meat_raw", "Raw Meat", CardType.Food)
    .WithCooking(3f, "meat_cooked");
```

2. Convert existing `Card` to `CardEntity`:

```csharp
// Find all Card components
var cards = FindObjectsOfType<Card>();
foreach (var card in cards)
{
    // Replace with CardEntity
    var cardEntity = card.gameObject.AddComponent<CardEntity>();
    cardEntity.Initialize(CreateDataFromOldCard(card));
    DestroyImmediate(card);
}
```

3. Create `ICardFactory` implementation:

```csharp
public class MyCardFactory : MonoBehaviour, ICardFactory
{
    [SerializeField] private CardDefinition[] _definitions;
    
    public ICardEntity CreateCard(string cardId)
    {
        // Your implementation
    }
}
```

4. Register factory in installer:

```csharp
public void InstallBindings(ContainerBuilder builder)
{
    builder.AddSingleton<ICardFactory>(GetComponent<MyCardFactory>());
    // ...
}
```

---

### Phase 3: Convert GameActions to Commands (1-2 weeks)

#### Example: DrawCardAction → DrawCardCommand

**Before:**
```csharp
// GameAction
public class DrawCardAction : GameAction { }

// Performer in CardSystem
private IEnumerator DrawCardPerformer(DrawCardAction action)
{
    Card card = Instantiate(_cardPrefab);
    yield return Tween.Position(card.transform, Vector3.zero, 0.5f);
}

// Usage
_actionSystem.Perform(new DrawCardAction());
```

**After:**
```csharp
// Command
public class DrawCardCommand : ICommand
{
    public bool AllowParallelExecution => false;
    public CommandPriority Priority => CommandPriority.Normal;
}

// Handler
public class DrawCardCommandHandler : ICommandHandler<DrawCardCommand>
{
    private readonly CardSystem _cardSystem;
    private readonly ICardFactory _cardFactory;
    
    public DrawCardCommandHandler(CardSystem cardSystem, ICardFactory cardFactory)
    {
        _cardSystem = cardSystem;
        _cardFactory = cardFactory;
    }
    
    public async UniTask<CommandResult> ExecuteAsync(
        DrawCardCommand command, 
        CancellationToken ct)
    {
        try
        {
            var card = _cardFactory.CreateCard("random_card");
            card.Transform.position = _cardSystem.SpawnPosition;
            
            // Async animation
            await Tween.PositionAsync(
                card.Transform, 
                Vector3.zero, 
                0.5f
            ).ToUniTask(cancellationToken: ct);
            
            return CommandResult.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ex);
        }
    }
}

// Register handler
bus.RegisterHandler(new DrawCardCommandHandler(cardSystem, cardFactory));

// Usage
_commandBus.Enqueue(new DrawCardCommand());
```

#### Conversion Checklist

For each GameAction:

- [ ] Create corresponding Command class
- [ ] Move performer logic to Handler
- [ ] Add validation if needed (IValidatableCommand)
- [ ] Add locking if modifies entities (ILockingCommand)
- [ ] Add progress if long-running (IProgressCommand)
- [ ] Register handler in installer
- [ ] Update all usage sites
- [ ] Test thoroughly
- [ ] Remove old GameAction and performer

---

### Phase 4: Update Interaction Code (3 days)

#### Before: Direct Manipulation

```csharp
public class Card : MonoBehaviour
{
    private void HandlePointerUp(Vector3 pointerPos)
    {
        var hitCollider = Physics2D.OverlapPoint(pointerPos);
        if (hitCollider != null)
        {
            var dropZone = hitCollider.GetComponent<ICardDropZone>();
            dropZone?.OnCardDropped(this);
        }
    }
}

public class CardDropZone : MonoBehaviour, ICardDropZone
{
    public void OnCardDropped(Card card)
    {
        // Directly modify card
        card.transform.position = transform.position;
    }
}
```

#### After: Command-Based

```csharp
public class CardInteractionController : MonoBehaviour
{
    [Inject] private ICommandBus _commandBus;
    
    private void HandlePointerUp(Vector3 pointerPos)
    {
        var hit = Physics2D.OverlapPoint(pointerPos);
        if (hit != null)
        {
            var targetCard = hit.GetComponent<ICardEntity>();
            if (targetCard != null)
            {
                ProcessInteraction(_cardEntity, targetCard);
            }
        }
    }
    
    private void ProcessInteraction(ICardEntity source, ICardEntity target)
    {
        // Determine interaction type
        if (target.Data.Type == CardType.Structure && source.Data.IsCookable)
        {
            _commandBus.Enqueue(new CookCommand(target, source));
        }
        else if (source.Data.Type == CardType.Unit && target.Data.Type == CardType.Enemy)
        {
            _commandBus.Enqueue(new AttackCommand(source, target));
        }
        // ... more interactions
    }
}
```

---

### Phase 5: Add UI Feedback (1 day)

#### 5.1 Progress Bars

```csharp
public class CookingProgressSpawner : MonoBehaviour
{
    [Inject] private CommandBusEvents _events;
    [SerializeField] private ProgressBarView _progressBarPrefab;
    
    private void OnEnable()
    {
        _events.OnCommandExecuting += HandleCommandExecuting;
    }
    
    private void HandleCommandExecuting(CommandEventArgs args)
    {
        if (args.Command is IProgressCommand progressCmd && 
            args.Command is CookCommand cookCmd)
        {
            var bar = Instantiate(_progressBarPrefab);
            bar.transform.SetParent(cookCmd.CookingStation.Transform);
            bar.BindToCommand(progressCmd);
        }
    }
}
```

#### 5.2 Error Notifications

```csharp
// Just add to scene - it auto-wires via [Inject]
public class CommandFailedNotification : MonoBehaviour
{
    [Inject] private CommandBusEvents _events;
    
    // Automatically shows popups when commands fail
}
```

---

### Phase 6: Testing (2 days)

#### 6.1 Manual Testing Checklist

- [ ] All cards can be created
- [ ] Dragging cards works
- [ ] Cooking shows progress bar
- [ ] Combat deals damage correctly
- [ ] Stacking triggers interactions
- [ ] Busy entities show feedback
- [ ] Invalid actions show errors
- [ ] No memory leaks (profile in profiler)
- [ ] No errors in console

#### 6.2 Automated Tests

```csharp
[Test]
public async Task CookCommand_ValidInput_ProducesCorrectResult()
{
    // Arrange
    var station = CreateMockStation();
    var ingredient = CreateMockIngredient();
    var factory = new MockCardFactory();
    var handler = new CookCommandHandler(factory);
    var command = new CookCommand(station, ingredient);
    
    // Act
    var result = await handler.ExecuteAsync(command, CancellationToken.None);
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
    factory.VerifyCardCreated("meat_cooked");
}
```

---

### Phase 7: Cleanup (1 day)

#### 7.1 Remove ActionSystem

1. Delete old files:
   - `ActionSystem.cs`
   - `GameAction.cs`
   - All `*Action.cs` files (if fully migrated)

2. Update installer:

```csharp
public class GameSceneInstaller : MonoBehaviour, IInstaller
{
    // Remove ActionSystem field
    // [SerializeField] private ActionSystem _actionSystem;  ← DELETE
    
    [SerializeField] private CommandBusInstaller _commandBusInstaller;
    
    public void InstallBindings(ContainerBuilder builder)
    {
        // Remove old binding
        // builder.AddSingleton(_actionSystem);  ← DELETE
        
        _commandBusInstaller.InstallBindings(builder);
    }
}
```

3. Search project for `ActionSystem` references:
   - Ctrl+Shift+F (Visual Studio)
   - Cmd+Shift+F (Rider)
   - Remove all usages

4. Test again!

---

## 🚨 Common Migration Issues

### Issue 1: "UniTask not found"

**Solution:**
```bash
# Add package via Package Manager or manifest.json
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```

### Issue 2: "ICardFactory not registered"

**Solution:**
```csharp
public void InstallBindings(ContainerBuilder builder)
{
    // IMPORTANT: Register factory BEFORE command bus
    builder.AddSingleton<ICardFactory>(GetComponent<SimpleCardFactory>());
    _commandBusInstaller.InstallBindings(builder);
}
```

### Issue 3: "Commands not executing"

**Check:**
1. Handler registered? `bus.RegisterHandler(new MyHandler())`
2. Command enqueued? `bus.Enqueue(new MyCommand())`
3. Any validation failures? Check console logs
4. Entities locked? Check `IsBusy` state

### Issue 4: "Memory leak in progress bars"

**Solution:**
Ensure progress bars call `Unbind()` on destroy:

```csharp
private void OnDestroy()
{
    Unbind();  // Important!
}
```

### Issue 5: "Handlers can't access dependencies"

**Solution:**
Pass dependencies via constructor:

```csharp
// ❌ Bad
bus.RegisterHandler(new CookHandler());

// ✅ Good
var factory = container.Resolve<ICardFactory>();
bus.RegisterHandler(new CookHandler(factory));
```

---

## 📊 Migration Progress Tracker

```markdown
### Week 1: Setup
- [ ] UniTask installed
- [ ] CommandBusInstaller added
- [ ] Debug logging enabled
- [ ] ICardFactory created
- [ ] CardData/CardEntity implemented

### Week 2: Commands
- [ ] DrawCardCommand migrated
- [ ] DealDamageCommand migrated
- [ ] Custom commands created
- [ ] All handlers registered
- [ ] Basic testing done

### Week 3: Integration
- [ ] Interaction controller updated
- [ ] Progress bars working
- [ ] Error notifications working
- [ ] All card types supported
- [ ] Comprehensive testing done

### Week 4: Cleanup
- [ ] ActionSystem removed
- [ ] Old code deleted
- [ ] Performance profiled
- [ ] Documentation updated
- [ ] Team trained on new system
```

---

## 🎓 Training Materials

### For Designers

**Before (ActionSystem):**
"Actions are C# classes that run in sequence with Pre/Perform/Post reactions."

**After (CommandBus):**
"Commands are instructions (like 'Cook this meat') that get executed when entities are free. You'll see progress bars for long actions."

**Key Concepts:**
- **Priority**: Critical > High > Normal > Low
- **Locking**: Busy entities can't do other things
- **Validation**: Some actions aren't allowed (you'll see error popups)

### For Programmers

**Key Differences:**

| ActionSystem | CommandBus |
|--------------|------------|
| Coroutine-based | Async/await (UniTask) |
| Single-threaded | Parallel-capable |
| Static handlers | Instance handlers |
| No validation | Pre-execution validation |
| No entity locking | Automatic locking |
| Retry on failure | Event-based failure |

**Best Practices:**
1. Commands are **data-only** (no logic)
2. Handlers are **stateless** (no fields except injected dependencies)
3. Always implement `IValidatableCommand` for preconditions
4. Use `ILockingCommand` if modifying entities
5. Return `CommandResult`, don't throw exceptions for expected failures

---

## 📚 Additional Resources

- [ARCHITECTURE.md](./ARCHITECTURE.md) - System design details
- [DEFECTS_AND_IMPROVEMENTS.md](./DEFECTS_AND_IMPROVEMENTS.md) - What was fixed and why
- [Examples/](./Examples/) - Reference implementations

---

## ❓ FAQ

**Q: Can I use both systems simultaneously?**
A: Yes! Use `HybridGameSceneInstaller` during migration.

**Q: What if I need synchronous execution?**
A: Set `AllowParallelExecution = false` in your command.

**Q: How do I debug commands?**
A: Enable debug logging in `CommandBusInstaller` and watch the console.

**Q: What about performance?**
A: Command bus is faster (UniTask is zero-allocation). Profile to verify.

**Q: Can I cancel commands?**
A: Yes! Implement `ICancellableCommand` and call `bus.TryCancel(cmd)`.

---

*Last updated: 2025-10-09*

