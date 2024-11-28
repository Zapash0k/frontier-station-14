using Content.Shared.Containers.ItemSlots;
using Content.Shared.Research;
using Content.Shared.Research.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Research.UI
{
    public sealed class DiskConsoleBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private DiskConsoleMenu? _menu;

        public DiskConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindow<DiskConsoleMenu>();

            _menu.OnServerButtonPressed += () =>
            {
                SendMessage(new ConsoleServerSelectionMessage());
            };
            _menu.OnPrintButtonPressed += () =>
            {
                SendMessage(new DiskConsolePrintDiskMessage());
            };
            _menu.OnPrintRareButtonPressed += () =>
            {
                SendMessage(new DiskConsolePrintRareDiskMessage());
            };

            // Frontier
            _menu.OnEjectResearchButtonPressed += () =>
            {
                SendMessage(new DiskConsoleEjectResearchMessage());
            };
            _menu.OnInsertOrEjectIdCardButtonPressed += () =>
            {
                SendMessage(new ItemSlotButtonPressedEvent("DiskConsole-targetId"));
            };
            // Frontier - end
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not DiskConsoleBoundUserInterfaceState msg)
                return;

            _menu?.Update(msg);
        }
    }
}
