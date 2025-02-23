﻿using System.Linq;
using System.Numerics;
using Content.Server._NF.CrateMachine;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Shared._NF.CrateMachine.Components;
using Content.Shared._NF.CrateStorage;
using Content.Shared._NF.Trade;
using Content.Shared.Placeable;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._NF.CrateStorage;

public sealed class CrateStorageSystem: EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly CrateMachineSystem _crateMachineSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;

    // Define a static range.
    private const float Range = 0.5f;

    // Dictionary where the key is the entity id of the crate storage and the value is the entity id of the crate.
    private readonly Dictionary<EntityUid, List<EntityUid>> _storedCrates = new();
    private EntityUid? _storageMap;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrateStorageComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CrateStorageComponent, ItemPlacedEvent>(OnItemPlacedEvent);
        SubscribeLocalEvent<CrateStorageComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<CrateStorageComponent, CrateMachineOpenedEvent>(OnCrateMachineOpened);
    }

    private void OnInit(EntityUid uid, CrateStorageComponent component, ComponentInit args)
    {
        _signalSystem.EnsureSinkPorts(uid, component.TriggerPort);
    }

    /// <summary>
    /// Once a crate machine is opened we will check if there are any crates intersecting it.
    /// </summary>
    /// <param name="crateMachineUid">the EntityUid of the crate </param>
    /// <param name="crateStorageComponent">the crate storage component</param>
    /// <param name="args">the arguments for the open event</param>
    private void OnCrateMachineOpened(EntityUid crateMachineUid, CrateStorageComponent crateStorageComponent, CrateMachineOpenedEvent args)
    {
        CheckIntersectingCrates(crateMachineUid);
    }

    /// <summary>
    /// Processes a signal received event to open a crate storage.
    /// </summary>
    /// <param name="crateStorageUid">the EntityUid for the crate storage</param>
    /// <param name="crateStorageComponent">the crate storage component</param>
    /// <param name="args">the args for the signal</param>
    private void OnSignalReceived(EntityUid crateStorageUid, CrateStorageComponent crateStorageComponent, SignalReceivedEvent args)
    {
        // Get the CrateMachineComponent from the entity.
        if (!TryComp(crateStorageUid, out CrateMachineComponent? crateMachineComponent))
            return;

        // Can't receive signals if we're not powered.
        if (!crateMachineComponent.Powered)
            return;

        if (args.Port == crateStorageComponent.TriggerPort)
            _crateMachineSystem.StartOpening(crateStorageUid, crateMachineComponent);
    }

    private void OnItemPlacedEvent(EntityUid crateStorageUid, CrateStorageComponent component, ItemPlacedEvent args)
    {
        // TODO
    }

    private EntityUid GetStorageMap()
    {
        if (!Deleted(_storageMap))
            return _storageMap.Value;

        _storageMap = _mapSystem.CreateMap(out var mapId);
        _mapManager.SetMapPaused(mapId, true);
        return _storageMap.Value;
    }

    /// <summary>
    /// Stores a crate in a crate storage map.
    /// </summary>
    /// <param name="crateStorageUid">the EntityUid of the crate storage</param>
    /// <param name="crateUid">the EntityUid of the crate that is to be stored</param>
    private void StoreCrate(EntityUid crateStorageUid, EntityUid crateUid)
    {
        if (!_storedCrates.TryGetValue(crateStorageUid, out var storedCrates))
        {
            storedCrates = new List<EntityUid>();
            _storedCrates.Add(crateStorageUid, storedCrates);
        }
        storedCrates.Add(crateUid);
        _transformSystem.SetCoordinates(crateUid, new EntityCoordinates(GetStorageMap(), Vector2.Zero));
    }

    /// <summary>
    /// Ejects the last crate stored in the crate storage. (First in first out)
    /// </summary>
    /// <param name="crateStorageUid">the EntityUid of the crate storage</param>
    private void EjectCrate(EntityUid crateStorageUid)
    {
        // Get the crate from the storage.
        if (!_storedCrates.TryGetValue(crateStorageUid, out var storedCrates))
            return;

        if (storedCrates.Count == 0)
            return;

        _transformSystem.SetCoordinates(storedCrates.First(), Transform(crateStorageUid).Coordinates);
    }

    /// <summary>
    /// Two functions: if there is a crate, move it. If there is none, eject a stored crate.
    /// </summary>
    /// <param name="crateStorageUid">the EntityUid of the crate storage</param>
    private void CheckIntersectingCrates(EntityUid crateStorageUid)
    {
        foreach (var near in _lookup.GetEntitiesInRange(crateStorageUid, Range, LookupFlags.Dynamic))
        {
            // Check if this is a trade crate
            if (TryComp(near, out TradeCrateComponent? _))
            {
                StoreCrate(crateStorageUid, near);
                return; // We found a crate and moved it, so we can stop here.
            }
        }

        // At this point we havent found any crates, so we should eject one.
        EjectCrate(crateStorageUid);
    }
}
