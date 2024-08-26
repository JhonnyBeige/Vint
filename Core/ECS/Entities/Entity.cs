﻿using System.Diagnostics;
using ConcurrentCollections;
using Serilog;
using Vint.Core.Config;
using Vint.Core.ECS.Components;
using Vint.Core.ECS.Components.Group;
using Vint.Core.ECS.Templates;
using Vint.Core.Protocol.Commands;
using Vint.Core.Server;
using Vint.Core.Utils;

namespace Vint.Core.ECS.Entities;

public class Entity : IEntity {
    public Entity(long id, TemplateAccessor? templateAccessor, IEnumerable<IComponent> components) {
        Id = id;
        TemplateAccessor = templateAccessor;
        ComponentStorage = new EntityComponentStorage(this, components);
    }

    ILogger Logger { get; } = Log.Logger.ForType(typeof(Entity));
    EntityComponentStorage ComponentStorage { get; }

    public ConcurrentHashSet<IPlayerConnection> SharedPlayers { get; } = [];

    public long Id { get; set; }

    public TemplateAccessor? TemplateAccessor { get; }

    public IEnumerable<IComponent> SortedComponents => ComponentStorage.SortedComponents;
    public IEnumerable<IComponent> Components => ComponentStorage.Components;

    public EntityShareCommand ToShareCommand() => new(Id, TemplateAccessor, SortedComponents.ToArray());

    public EntityUnshareCommand ToUnshareCommand() => new(this);

    public async Task Share(IPlayerConnection connection) {
        Logger.Debug("Sharing {Entity} to {Connection}", this, connection);

        if (!SharedPlayers.Add(connection)) {
            Logger.Warning("{Entity} is already shared to {Connection}", this, connection);
            Debugger.Break();
        }

        await connection.Send(ToShareCommand());
        connection.SharedEntities.Add(this);
    }

    public async Task Unshare(IPlayerConnection connection) {
        Logger.Debug("Unsharing {Entity} from {Connection}", this, connection);

        if (!SharedPlayers.TryRemove(connection)) {
            Logger.Warning("{Entity} is not shared to {Connection}", this, connection);
            Debugger.Break();
        }

        connection.SharedEntities.TryRemove(this);
        await connection.Send(ToUnshareCommand());
    }

    public T GetComponent<T>() where T : class, IComponent => (T)GetComponent(typeof(T));

    public IComponent GetComponent(Type type) =>
        ComponentStorage.GetComponent(type);

    public Task RemoveComponentIfPresent<T>(IPlayerConnection? excluded = null) where T : class, IComponent =>
        RemoveComponentIfPresent(typeof(T), excluded);

    public Task RemoveComponentIfPresent(IComponent component, IPlayerConnection? excluded = null) =>
        RemoveComponentIfPresent(component.GetType(), excluded);

    public async Task RemoveComponentIfPresent(Type type, IPlayerConnection? excluded = null) {
        if (HasComponent(type))
            await RemoveComponent(type, excluded);
    }

    public IEntity Clone() => new Entity(Id,
        TemplateAccessor == null ? null
            : new TemplateAccessor(TemplateAccessor.Template, TemplateAccessor.ConfigPath),
        Components.ToHashSet());

    public async Task AddComponent(IComponent component, IPlayerConnection? excluded = null) {
        if (component is GroupComponent groupComponent)
            component = GroupComponentRegistry.FindOrRegisterGroup(groupComponent);

        ComponentStorage.AddComponent(component);
        Logger.Debug("Added {Name} component to the {Entity}", component.GetType().Name, this);

        foreach (IPlayerConnection playerConnection in SharedPlayers.Where(pc => pc != excluded))
            await playerConnection.Send(new ComponentAddCommand(this, component));
    }

    public Task AddComponent<T>(IPlayerConnection? excluded = null) where T : class, IComponent, new() =>
        AddComponent(new T(), excluded);

    public Task AddComponent<T>(string configPath, IPlayerConnection? excluded = null) where T : class, IComponent =>
        AddComponent(ConfigManager.GetComponent<T>(configPath), excluded);

    public async Task AddGroupComponent<T>(IEntity? key = null, IPlayerConnection? excluded = null) where T : GroupComponent {
        T component = GroupComponentRegistry.FindOrCreateGroup<T>(key?.Id ?? Id);
        await AddComponent(component);
    }

    public Task AddComponentFrom<T>(IEntity entity, IPlayerConnection? excluded = null) where T : class, IComponent =>
        AddComponent(entity.GetComponent<T>(), excluded);

    public Task AddComponentFromConfig<T>() where T : class, IComponent =>
        AddComponent<T>(TemplateAccessor!.ConfigPath!);

    public async Task AddComponentIfAbsent(IComponent component, IPlayerConnection? excluded = null) {
        if (!HasComponent(component))
            await AddComponent(component, excluded);
    }

    public async Task AddComponentIfAbsent<T>(IPlayerConnection? excluded = null) where T : class, IComponent, new() {
        if (!HasComponent<T>())
            await AddComponent<T>(excluded);
    }

    public bool HasComponent<T>() where T : class, IComponent =>
        HasComponent(typeof(T));

    public bool HasComponent(IComponent component) =>
        HasComponent(component.GetType());

    public bool HasComponent(Type type) => ComponentStorage.HasComponent(type);

    public async Task ChangeComponent<T>(Func<T, Task> func) where T : class, IComponent {
        T component = GetComponent<T>();

        await func(component);
        await ChangeComponent(component, null);
    }

    public async Task ChangeComponent<T>(Action<T> action) where T : class, IComponent {
        T component = GetComponent<T>();

        action(component);
        await ChangeComponent(component, null);
    }

    public async Task ChangeComponent(IComponent component, IPlayerConnection? excluded) {
        ComponentStorage.ChangeComponent(component);
        Logger.Debug("Changed {Name} component in the {Entity}", component.GetType().Name, this);

        foreach (IPlayerConnection playerConnection in SharedPlayers.Where(pc => pc != excluded))
            await playerConnection.Send(new ComponentChangeCommand(this, component));
    }

    public Task RemoveComponent<T>(IPlayerConnection? excluded) where T : class, IComponent =>
        RemoveComponent(typeof(T), excluded);

    public Task RemoveComponent(IComponent component, IPlayerConnection? excluded = null) =>
        RemoveComponent(component.GetType(), excluded);

    public async Task RemoveComponent(Type type, IPlayerConnection? excluded = null) {
        ComponentStorage.RemoveComponent(type);
        Logger.Debug("Removed {Name} component from the {Entity}", type.Name, this);

        foreach (IPlayerConnection playerConnection in SharedPlayers.Where(pc => pc != excluded))
            await playerConnection.Send(new ComponentRemoveCommand(this, type));
    }

    public override string ToString() => $"Entity {{ " +
                                         $"Id: {Id}; " +
                                         $"TemplateAccessor: {TemplateAccessor}; " +
                                         $"Components {{ {Components.ToString(false)} }} }}";

    public override int GetHashCode() => Id.GetHashCode();
}
