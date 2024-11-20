using Content.Client._NF.Atmos.UI;
using Content.Shared._NF.Atmos.BUI;
using Content.Shared._NF.Atmos.Events;
using Robust.Client.UserInterface;

namespace Content.Client._NF.Atmos.BUI;

public sealed class GasSaleConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private GasSaleMenu? _menu;

    public GasSaleConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<GasSaleMenu>();
        _menu.SellRequested += OnSell;
    }

    private void OnSell()
    {
        SendMessage(new GasSaleSellMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not GasSaleConsoleBoundUserInterfaceState gasState)
            return;

        _menu?.SetEnabled(gasState.Enabled);
        _menu?.SetMixture(gasState.Mixture, gasState.Appraisal);
    }
}
