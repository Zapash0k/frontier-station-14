using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Visuals;

[Serializable, NetSerializable]
public enum DockablePumpVisuals : byte
{
    Docked, // bool
    PumpingOutwards, // bool
}
