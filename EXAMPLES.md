# Aqua Command System - Usage Examples

This document provides practical, copy-paste examples for common use cases in Unity game development.

---

## Table of Contents

1. [Turn-Based Combat](#turn-based-combat)
2. [Inventory Management](#inventory-management)
3. [Player Movement](#player-movement)
4. [Ability System](#ability-system)
5. [Quest System](#quest-system)
6. [Save/Load System](#saveload-system)
7. [Networking Integration](#networking-integration)
8. [UI Integration](#ui-integration)

---

## Turn-Based Combat

### Commands

```csharp
using UnityEngine;
using com.aqua.system;

// Base combat command
public abstract class CombatCommand : ICommand, ILockingCommand
{
    public readonly int CombatId;
    public readonly int ActorId;

    protected CombatCommand(int combatId, int actorId)
    {
        CombatId = combatId;
        ActorId = actorId;
    }

    // Lock combat and actor
    public object[] GetLockKeys() => new object[] { CombatId, ActorId };
    
    // Combat commands are sequential
    public bool AllowParallelExecution => false;
}

// Attack command
public class AttackCommand : CombatCommand
{
    public readonly int TargetId;
    public readonly int Damage;

    public AttackCommand(int combatId, int actorId, int targetId, int damage)
        : base(combatId, actorId)
    {
        TargetId = targetId;
        Damage = damage;
    }
}

// Heal command
public class HealCommand : CombatCommand
{
    public readonly int TargetId;
    public readonly int Amount;

    public HealCommand(int combatId, int actorId, int targetId, int amount)
        : base(combatId, actorId)
    {
        TargetId = targetId;
        Amount = amount;
    }
}

// Use item command
public class UseItemCommand : CombatCommand, ICancellableCommand
{
    public readonly int ItemId;
    public readonly int TargetId;
    private bool _itemConsumed;

    public UseItemCommand(int combatId, int actorId, int itemId, int targetId)
        : base(combatId, actorId)
    {
        ItemId = itemId;
        TargetId = targetId;
    }

    public void OnCancelled()
    {
        // Refund item if not consumed yet
        if (!_itemConsumed)
        {
            Debug.Log($"Item {ItemId} returned to inventory");
        }
    }
}
```

### Handlers

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using com.aqua.system;

public class AttackHandler : CommandHandlerBase<AttackCommand>
{
    private readonly ICombatService _combatService;

    public AttackHandler(ICombatService combatService)
    {
        _combatService = combatService;
    }

    public override ValidationResult Validate(AttackCommand command)
    {
        var actor = _combatService.GetActor(command.ActorId);
        if (actor == null)
            return ValidationResult.Invalid("Actor not found");

        if (actor.IsDead)
            return ValidationResult.Invalid("Actor is dead");

        if (!actor.CanAct)
            return ValidationResult.Invalid("Actor cannot act this turn");

        var target = _combatService.GetActor(command.TargetId);
        if (target == null)
            return ValidationResult.Invalid("Target not found");

        if (target.IsDead)
            return ValidationResult.Invalid("Target is already dead");

        if (!_combatService.IsInRange(actor, target))
            return ValidationResult.Invalid("Target out of range");

        return ValidationResult.Valid();
    }

    public override async UniTask<CommandResult> ExecuteAsync(
        AttackCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // Play attack animation
            await _combatService.PlayAttackAnimation(command.ActorId, cancellationToken);

            // Apply damage
            var actualDamage = _combatService.ApplyDamage(command.TargetId, command.Damage);

            // Play hit effect
            await _combatService.PlayHitEffect(command.TargetId, cancellationToken);

            // Check for death
            if (_combatService.GetActor(command.TargetId).IsDead)
            {
                await _combatService.PlayDeathAnimation(command.TargetId, cancellationToken);
            }

            Debug.Log($"Actor {command.ActorId} dealt {actualDamage} damage to {command.TargetId}");
            return CommandResult.Success();
        }
        catch (OperationCanceledException)
        {
            return CommandResult.Cancelled();
        }
        catch (System.Exception ex)
        {
            return CommandResult.Failure(ex);
        }
    }
}
```

### Combat Manager Integration

```csharp
using UnityEngine;
using com.aqua.system;

public class CombatManager : MonoBehaviour
{
    private AsyncCommandBus _commandBus;
    private ICombatService _combatService;

    private void Awake()
    {
        _combatService = new CombatService();
        _commandBus = new AsyncCommandBus();

        // Register handlers
        _commandBus.RegisterHandler(new AttackHandler(_combatService));
        _commandBus.RegisterHandler(new HealHandler(_combatService));
        _commandBus.RegisterHandler(new UseItemHandler(_combatService));

        // Subscribe to events
        _commandBus.Events.OnCommandCompleted += OnCombatActionCompleted;
        _commandBus.Events.OnCommandFailed += OnCombatActionFailed;
    }

    public void ExecutePlayerAttack(int playerId, int targetId)
    {
        var damage = _combatService.CalculateDamage(playerId);
        var command = new AttackCommand(
            combatId: GetCurrentCombatId(),
            actorId: playerId,
            targetId: targetId,
            damage: damage
        );
        _commandBus.Enqueue(command);
    }

    public void ExecuteAITurn(int aiId)
    {
        var target = _combatService.ChooseTarget(aiId);
        var damage = _combatService.CalculateDamage(aiId);
        var command = new AttackCommand(
            combatId: GetCurrentCombatId(),
            actorId: aiId,
            targetId: target,
            damage: damage
        );
        _commandBus.Enqueue(command);
    }

    private void OnCombatActionCompleted(CommandEventArgs args)
    {
        Debug.Log($"Combat action completed: {args.Command.GetType().Name}");
        
        // Check for combat end
        if (_combatService.IsCombatOver())
        {
            EndCombat();
        }
        else
        {
            NextTurn();
        }
    }

    private void OnCombatActionFailed(CommandEventArgs args)
    {
        Debug.LogError($"Combat action failed: {args.Result.ErrorMessage}");
        // Allow player to retry
        ShowRetryUI();
    }

    private void OnDestroy()
    {
        _commandBus?.Dispose();
    }
}
```

---

## Inventory Management

### Commands

```csharp
using com.aqua.system;

// Add item to inventory
public class AddItemCommand : ICommand, ILockingCommand
{
    public readonly int InventoryId;
    public readonly int ItemId;
    public readonly int Quantity;

    public AddItemCommand(int inventoryId, int itemId, int quantity = 1)
    {
        InventoryId = inventoryId;
        ItemId = itemId;
        Quantity = quantity;
    }

    public object[] GetLockKeys() => new object[] { InventoryId };
    public bool AllowParallelExecution => true;
}

// Transfer item between inventories
public class TransferItemCommand : ICommand, ILockingCommand
{
    public readonly int FromInventoryId;
    public readonly int ToInventoryId;
    public readonly int ItemId;
    public readonly int Quantity;

    public TransferItemCommand(int from, int to, int itemId, int quantity)
    {
        FromInventoryId = from;
        ToInventoryId = to;
        ItemId = itemId;
        Quantity = quantity;
    }

    // Lock both inventories
    public object[] GetLockKeys() => new object[] 
    { 
        FromInventoryId, 
        ToInventoryId 
    };
}

// Equip item
public class EquipItemCommand : ICommand, ILockingCommand
{
    public readonly int PlayerId;
    public readonly int ItemId;
    public readonly EquipSlot Slot;

    public EquipItemCommand(int playerId, int itemId, EquipSlot slot)
    {
        PlayerId = playerId;
        ItemId = itemId;
        Slot = slot;
    }

    public object[] GetLockKeys() => new object[] { PlayerId };
}

public enum EquipSlot
{
    Weapon,
    Helmet,
    Chest,
    Legs,
    Boots
}
```

### Handlers

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using com.aqua.system;

public class AddItemHandler : CommandHandlerBase<AddItemCommand>
{
    private readonly IInventoryService _inventoryService;

    public AddItemHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public override ValidationResult Validate(AddItemCommand command)
    {
        var inventory = _inventoryService.GetInventory(command.InventoryId);
        if (inventory == null)
            return ValidationResult.Invalid("Inventory not found");

        if (command.Quantity <= 0)
            return ValidationResult.Invalid("Quantity must be positive");

        if (!_inventoryService.HasSpace(command.InventoryId, command.ItemId, command.Quantity))
            return ValidationResult.Invalid("Inventory is full");

        return ValidationResult.Valid();
    }

    public override async UniTask<CommandResult> ExecuteAsync(
        AddItemCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            await _inventoryService.AddItemAsync(
                command.InventoryId,
                command.ItemId,
                command.Quantity,
                cancellationToken
            );

            return CommandResult.Success();
        }
        catch (System.Exception ex)
        {
            return CommandResult.Failure(ex);
        }
    }
}

public class TransferItemHandler : CommandHandlerBase<TransferItemCommand>
{
    private readonly IInventoryService _inventoryService;

    public TransferItemHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public override ValidationResult Validate(TransferItemCommand command)
    {
        // Validate source inventory
        if (!_inventoryService.HasItem(command.FromInventoryId, command.ItemId, command.Quantity))
            return ValidationResult.Invalid("Source inventory doesn't have enough items");

        // Validate destination inventory
        if (!_inventoryService.HasSpace(command.ToInventoryId, command.ItemId, command.Quantity))
            return ValidationResult.Invalid("Destination inventory is full");

        return ValidationResult.Valid();
    }

    public override async UniTask<CommandResult> ExecuteAsync(
        TransferItemCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // Remove from source
            await _inventoryService.RemoveItemAsync(
                command.FromInventoryId,
                command.ItemId,
                command.Quantity,
                cancellationToken
            );

            // Add to destination
            await _inventoryService.AddItemAsync(
                command.ToInventoryId,
                command.ItemId,
                command.Quantity,
                cancellationToken
            );

            return CommandResult.Success();
        }
        catch (System.Exception ex)
        {
            // Rollback on failure
            await _inventoryService.AddItemAsync(
                command.FromInventoryId,
                command.ItemId,
                command.Quantity,
                CancellationToken.None
            );

            return CommandResult.Failure(ex);
        }
    }
}
```

---

## Player Movement

### Commands

```csharp
using UnityEngine;
using com.aqua.system;

public class MovePlayerCommand : ICommand, ILockingCommand, ICancellableCommand
{
    public readonly int PlayerId;
    public readonly Vector3 Destination;
    public readonly float Speed;

    private bool _isCancelled;

    public MovePlayerCommand(int playerId, Vector3 destination, float speed = 5f)
    {
        PlayerId = playerId;
        Destination = destination;
        Speed = speed;
    }

    public object[] GetLockKeys() => new object[] { PlayerId };
    
    // Allow multiple movement commands to queue
    public bool AllowParallelExecution => true;

    public void OnCancelled()
    {
        _isCancelled = true;
        Debug.Log($"Movement cancelled for player {PlayerId}");
    }

    public bool IsCancelled => _isCancelled;
}

public class TeleportPlayerCommand : ICommand, ILockingCommand
{
    public readonly int PlayerId;
    public readonly Vector3 Position;

    public TeleportPlayerCommand(int playerId, Vector3 position)
    {
        PlayerId = playerId;
        Position = position;
    }

    public object[] GetLockKeys() => new object[] { PlayerId };
    
    // Instant, no parallel
    public bool AllowParallelExecution => false;
}
```

### Handler

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using com.aqua.system;

public class MovePlayerHandler : CommandHandlerBase<MovePlayerCommand>
{
    private readonly IPlayerService _playerService;
    private readonly INavigationService _navigationService;

    public MovePlayerHandler(IPlayerService playerService, INavigationService navigationService)
    {
        _playerService = playerService;
        _navigationService = navigationService;
    }

    public override ValidationResult Validate(MovePlayerCommand command)
    {
        var player = _playerService.GetPlayer(command.PlayerId);
        if (player == null)
            return ValidationResult.Invalid("Player not found");

        if (!_navigationService.IsValidPosition(command.Destination))
            return ValidationResult.Invalid("Invalid destination");

        if (!_navigationService.CanReach(player.Position, command.Destination))
            return ValidationResult.Invalid("Destination not reachable");

        return ValidationResult.Valid();
    }

    public override async UniTask<CommandResult> ExecuteAsync(
        MovePlayerCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var player = _playerService.GetPlayer(command.PlayerId);
            var path = _navigationService.CalculatePath(player.Position, command.Destination);

            // Animate movement along path
            foreach (var waypoint in path)
            {
                // Check cancellation
                if (command.IsCancelled || cancellationToken.IsCancellationRequested)
                {
                    return CommandResult.Cancelled();
                }

                // Move towards waypoint
                while (Vector3.Distance(player.Position, waypoint) > 0.1f)
                {
                    player.Position = Vector3.MoveTowards(
                        player.Position,
                        waypoint,
                        command.Speed * Time.deltaTime
                    );

                    await UniTask.Yield(cancellationToken);
                }
            }

            return CommandResult.Success();
        }
        catch (OperationCanceledException)
        {
            return CommandResult.Cancelled();
        }
        catch (System.Exception ex)
        {
            return CommandResult.Failure(ex);
        }
    }
}
```

---

## Ability System

### Commands

```csharp
using UnityEngine;
using com.aqua.system;

public class CastAbilityCommand : ICommand, ILockingCommand, ICancellableCommand
{
    public readonly int CasterId;
    public readonly int AbilityId;
    public readonly int[] TargetIds;
    public readonly Vector3? TargetPosition;

    private bool _isCancelled;

    public CastAbilityCommand(
        int casterId,
        int abilityId,
        int[] targetIds = null,
        Vector3? targetPosition = null)
    {
        CasterId = casterId;
        AbilityId = abilityId;
        TargetIds = targetIds ?? System.Array.Empty<int>();
        TargetPosition = targetPosition;
    }

    public object[] GetLockKeys()
    {
        // Lock caster and all targets
        var locks = new System.Collections.Generic.List<object> { CasterId };
        locks.AddRange(TargetIds.Select(id => (object)id));
        return locks.ToArray();
    }

    public void OnCancelled()
    {
        _isCancelled = true;
    }

    public bool IsCancelled => _isCancelled;
}
```

### Handler

```csharp
using System.Threading;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using com.aqua.system;

public class CastAbilityHandler : CommandHandlerBase<CastAbilityCommand>
{
    private readonly IAbilityService _abilityService;
    private readonly IPlayerService _playerService;

    public CastAbilityHandler(IAbilityService abilityService, IPlayerService playerService)
    {
        _abilityService = abilityService;
        _playerService = playerService;
    }

    public override ValidationResult Validate(CastAbilityCommand command)
    {
        var caster = _playerService.GetPlayer(command.CasterId);
        if (caster == null)
            return ValidationResult.Invalid("Caster not found");

        var ability = _abilityService.GetAbility(command.AbilityId);
        if (ability == null)
            return ValidationResult.Invalid("Ability not found");

        // Check if player has ability
        if (!_playerService.HasAbility(command.CasterId, command.AbilityId))
            return ValidationResult.Invalid("Player doesn't have this ability");

        // Check cooldown
        if (_abilityService.IsOnCooldown(command.CasterId, command.AbilityId))
            return ValidationResult.Invalid("Ability is on cooldown");

        // Check resource cost (mana, stamina, etc.)
        if (!_playerService.HasResources(command.CasterId, ability.Cost))
            return ValidationResult.Invalid("Insufficient resources");

        // Check range
        if (command.TargetIds.Length > 0)
        {
            var outOfRange = command.TargetIds
                .Select(_playerService.GetPlayer)
                .Any(target => !_abilityService.IsInRange(caster, target, ability));

            if (outOfRange)
                return ValidationResult.Invalid("Target out of range");
        }

        return ValidationResult.Valid();
    }

    public override async UniTask<CommandResult> ExecuteAsync(
        CastAbilityCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var ability = _abilityService.GetAbility(command.AbilityId);

            // Consume resources
            _playerService.ConsumeResources(command.CasterId, ability.Cost);

            // Cast time (channeling)
            if (ability.CastTime > 0)
            {
                await UniTask.Delay(
                    System.TimeSpan.FromSeconds(ability.CastTime),
                    cancellationToken: cancellationToken
                );

                if (command.IsCancelled)
                {
                    // Refund partial resources
                    _playerService.RefundResources(command.CasterId, ability.Cost * 0.5f);
                    return CommandResult.Cancelled();
                }
            }

            // Execute ability effects
            foreach (var targetId in command.TargetIds)
            {
                await _abilityService.ApplyEffectsAsync(
                    command.AbilityId,
                    command.CasterId,
                    targetId,
                    cancellationToken
                );
            }

            // Position-based effects
            if (command.TargetPosition.HasValue)
            {
                await _abilityService.ApplyAreaEffectAsync(
                    command.AbilityId,
                    command.TargetPosition.Value,
                    cancellationToken
                );
            }

            // Start cooldown
            _abilityService.StartCooldown(command.CasterId, command.AbilityId, ability.Cooldown);

            return CommandResult.Success();
        }
        catch (OperationCanceledException)
        {
            return CommandResult.Cancelled();
        }
        catch (System.Exception ex)
        {
            return CommandResult.Failure(ex);
        }
    }
}
```

---

## UI Integration

### Command-Driven UI

```csharp
using UnityEngine;
using UnityEngine.UI;
using com.aqua.system;

public class CombatUI : MonoBehaviour
{
    [SerializeField] private Button _attackButton;
    [SerializeField] private Button _defendButton;
    [SerializeField] private Button _itemButton;
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private Text _statusText;

    private AsyncCommandBus _commandBus;
    private int _currentPlayerId;
    private int _selectedTargetId;

    private void Start()
    {
        // Get command bus from DI or singleton
        _commandBus = ServiceLocator.Get<AsyncCommandBus>();

        // Subscribe to events
        _commandBus.Events.OnCommandExecuting += OnCommandExecuting;
        _commandBus.Events.OnCommandCompleted += OnCommandCompleted;
        _commandBus.Events.OnCommandFailed += OnCommandFailed;

        // Setup button listeners
        _attackButton.onClick.AddListener(OnAttackClicked);
        _defendButton.onClick.AddListener(OnDefendClicked);
        _itemButton.onClick.AddListener(OnItemClicked);

        UpdateUI();
    }

    private void OnAttackClicked()
    {
        if (_selectedTargetId <= 0)
        {
            ShowError("Please select a target");
            return;
        }

        var command = new AttackCommand(
            combatId: GameManager.Instance.CurrentCombatId,
            actorId: _currentPlayerId,
            targetId: _selectedTargetId,
            damage: 10
        );

        _commandBus.Enqueue(command);
    }

    private void OnDefendClicked()
    {
        var command = new DefendCommand(
            combatId: GameManager.Instance.CurrentCombatId,
            actorId: _currentPlayerId
        );

        _commandBus.Enqueue(command);
    }

    private void OnItemClicked()
    {
        // Show item selection UI
        ShowItemSelectionUI((itemId) =>
        {
            var command = new UseItemCommand(
                combatId: GameManager.Instance.CurrentCombatId,
                actorId: _currentPlayerId,
                itemId: itemId,
                targetId: _selectedTargetId
            );

            _commandBus.Enqueue(command);
        });
    }

    private void OnCommandExecuting(CommandEventArgs args)
    {
        // Show loading
        _loadingPanel.SetActive(true);
        _statusText.text = $"Executing {args.Command.GetType().Name}...";
        
        // Disable buttons
        SetButtonsInteractable(false);
    }

    private void OnCommandCompleted(CommandEventArgs args)
    {
        // Hide loading
        _loadingPanel.SetActive(false);
        _statusText.text = "Action completed successfully!";
        
        // Re-enable buttons
        SetButtonsInteractable(true);
        
        UpdateUI();
    }

    private void OnCommandFailed(CommandEventArgs args)
    {
        _loadingPanel.SetActive(false);
        ShowError(args.Result.ErrorMessage);
        
        SetButtonsInteractable(true);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        _attackButton.interactable = interactable;
        _defendButton.interactable = interactable;
        _itemButton.interactable = interactable;
    }

    private void ShowError(string message)
    {
        _statusText.text = $"<color=red>Error: {message}</color>";
    }

    private void UpdateUI()
    {
        // Update button states based on game state
        var canAttack = GameManager.Instance.CanPlayerAct(_currentPlayerId);
        _attackButton.interactable = canAttack;
        _defendButton.interactable = canAttack;
        _itemButton.interactable = canAttack;

        // Update queue display
        var queueCount = _commandBus.QueuedCommandCount;
        var executingCount = _commandBus.ExecutingCommandCount;
        _statusText.text = $"Queue: {queueCount} | Executing: {executingCount}";
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (_commandBus != null)
        {
            _commandBus.Events.OnCommandExecuting -= OnCommandExecuting;
            _commandBus.Events.OnCommandCompleted -= OnCommandCompleted;
            _commandBus.Events.OnCommandFailed -= OnCommandFailed;
        }
    }
}
```

---

## Networking Integration

```csharp
using UnityEngine;
using com.aqua.system;

// Network command wrapper
[System.Serializable]
public class NetworkCommand
{
    public int SenderId;
    public string CommandType;
    public string CommandData; // JSON serialized

    public static NetworkCommand Create(int senderId, ICommand command)
    {
        return new NetworkCommand
        {
            SenderId = senderId,
            CommandType = command.GetType().AssemblyQualifiedName,
            CommandData = JsonUtility.ToJson(command)
        };
    }

    public ICommand Deserialize()
    {
        var type = System.Type.GetType(CommandType);
        return (ICommand)JsonUtility.FromJson(CommandData, type);
    }
}

// Network command handler
public class NetworkCommandHandler : MonoBehaviour
{
    private AsyncCommandBus _commandBus;

    private void Start()
    {
        _commandBus = new AsyncCommandBus();
        
        // Register handlers
        RegisterAllHandlers();

        // Subscribe to network events
        NetworkManager.OnCommandReceived += OnNetworkCommandReceived;
    }

    private void OnNetworkCommandReceived(NetworkCommand networkCommand)
    {
        // Validate sender
        if (!ValidateSender(networkCommand.SenderId))
        {
            Debug.LogWarning($"Invalid sender: {networkCommand.SenderId}");
            return;
        }

        // Deserialize and enqueue
        try
        {
            var command = networkCommand.Deserialize();
            _commandBus.Enqueue(command);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to deserialize command: {ex.Message}");
        }
    }

    public void SendCommandToServer(ICommand command)
    {
        var networkCommand = NetworkCommand.Create(GetLocalPlayerId(), command);
        var json = JsonUtility.ToJson(networkCommand);
        NetworkManager.SendToServer(json);
    }

    private bool ValidateSender(int senderId)
    {
        // Implement authentication logic
        return true;
    }

    private int GetLocalPlayerId()
    {
        return NetworkManager.LocalPlayerId;
    }

    private void RegisterAllHandlers()
    {
        // Register all command handlers
        _commandBus.RegisterHandler(new AttackHandler(new CombatService()));
        _commandBus.RegisterHandler(new MovePlayerHandler(new PlayerService(), new NavigationService()));
        // ... more handlers
    }

    private void OnDestroy()
    {
        NetworkManager.OnCommandReceived -= OnNetworkCommandReceived;
        _commandBus?.Dispose();
    }
}
```

---

## Summary

These examples demonstrate:

✅ **Real-world patterns** for common game systems  
✅ **Proper validation** and error handling  
✅ **Cancellation support** for long-running operations  
✅ **Entity locking** to prevent race conditions  
✅ **UI integration** with event-driven updates  
✅ **Network synchronization** of commands  

Copy and adapt these examples to your specific game needs!

---

**For more examples, see:**
- [README.md](README.md) - Complete documentation
- [ARCHITECTURE_IMPROVEMENTS.md](ARCHITECTURE_IMPROVEMENTS.md) - Advanced patterns

