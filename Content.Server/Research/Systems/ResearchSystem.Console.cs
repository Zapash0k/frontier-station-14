using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared._NF.CCVar;
using Content.Shared.UserInterface;
using Content.Shared.Access.Components;
using Content.Shared.Emag.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Research.Systems;

/// <summary>
/// ResearchSystem is due for a rewrite it is made in 2022 and i am suffering changing anything in this shitcode
/// REEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE
/// </summary>
public sealed partial class ResearchSystem
{
    private void InitializeConsole()
    {
        SubscribeLocalEvent<ResearchConsoleComponent, ConsoleUnlockTechnologyMessage>(OnConsoleUnlock);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, BeforeActivatableUIOpenEvent>(OnConsoleBeforeUiOpened);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchRegistrationChangedEvent>(OnConsoleRegistrationChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, TechnologyDatabaseModifiedEvent>(OnConsoleDatabaseModified);
    }

    private void OnConsoleBeforeUiOpened(EntityUid uid, ResearchConsoleComponent component, BeforeActivatableUIOpenEvent args)
    {
        SyncClientWithServer(uid);
    }

    private void OnConsoleUnlock(EntityUid uid, ResearchConsoleComponent component, ConsoleUnlockTechnologyMessage args)
    {
        var act = args.Actor;

        if (!this.IsPowered(uid, EntityManager))
            return;

        if (!PrototypeManager.TryIndex<TechnologyPrototype>(args.Id, out var technologyPrototype))
            return;

        if (TryComp<AccessReaderComponent>(uid, out var access) && !_accessReader.IsAllowed(act, uid, access))
        {
            _popup.PopupEntity(Loc.GetString("research-console-no-access-popup"), act);
            return;
        }

        if (!UnlockTechnology(uid, args.Id, act))
            return;

        SyncClientWithServer(uid);
        // if (false && !HasComp<EmaggedComponent>(uid)) // Frontier: add false - silent R&D computers
        // {
        //     var getIdentityEvent = new TryGetIdentityShortInfoEvent(uid, act);
        //     RaiseLocalEvent(getIdentityEvent);
        //
        //     var message = Loc.GetString(
        //         "research-console-unlock-technology-radio-broadcast",
        //         ("technology", Loc.GetString(technologyPrototype.Name)),
        //         ("amount", technologyPrototype.Cost),
        //         ("approver", getIdentityEvent.Title ?? string.Empty)
        //     );
        //     _radio.SendRadioMessage(uid, message, component.AnnouncementChannel, uid, escapeMarkup: false);
        // }
        UpdateConsoleInterface(uid, component);
    }

    private void UpdateConsoleInterface(EntityUid uid, ResearchConsoleComponent? component = null, ResearchClientComponent? clientComponent = null, TechnologyDatabaseComponent? clientDatabase = null)
    {
        if (!Resolve(uid, ref component, ref clientComponent, ref clientDatabase, false))
            return;

        ResearchConsoleBoundInterfaceState state;

        var station = _station.GetOwningStation(uid);
        if (station == null)
            return;

        if (!TryComp<TechnologyDatabaseComponent>(station.Value, out var serverDatabase))
            return;

        // Frontier: increase point cost by unlocked technologies count
        var cvarModifier = _configuration.GetCVar(NFCCVars.ScienceIncreasingUnlockModifier);
        var isIncreasingUnlockCostEnabled = _configuration.GetCVar(NFCCVars.ScienceIncreasingUnlockCost);
        var unlockCostModifier = 1f;
        if (isIncreasingUnlockCostEnabled)
            unlockCostModifier = cvarModifier * serverDatabase.UnlockedTechnologies.Count + 1f;


        if (TryGetClientServer(uid, out _, out var serverComponent, clientComponent))
        {
            state = new ResearchConsoleBoundInterfaceState(serverComponent.Points, unlockCostModifier); // Frontier: add modifier
        }
        else
        {
            state = new ResearchConsoleBoundInterfaceState(default, unlockCostModifier); // Frontier: add modifier
        }

        _uiSystem.SetUiState(uid, ResearchConsoleUiKey.Key, state);
    }

    private void OnPointsChanged(EntityUid uid, ResearchConsoleComponent component, ref ResearchServerPointsChangedEvent args)
    {
        if (!_uiSystem.IsUiOpen(uid, ResearchConsoleUiKey.Key))
            return;
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleRegistrationChanged(EntityUid uid, ResearchConsoleComponent component, ref ResearchRegistrationChangedEvent args)
    {
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleDatabaseModified(EntityUid uid, ResearchConsoleComponent component, ref TechnologyDatabaseModifiedEvent args)
    {
        UpdateConsoleInterface(uid, component);
    }

}
