using Content.Server.Body.Components;
using Content.Shared._CM14.Medical.IV;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Timing;

namespace Content.Server._CM14.Medical.IV;

public sealed class IVDripSystem : SharedIVDripSystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<IVDripComponent>();
        while (query.MoveNext(out var ivId, out var ivComp))
        {
            if (ivComp.AttachedTo is not { } attachedTo)
                continue;

            if (!InRange((ivId, ivComp), attachedTo))
                Detach((ivId, ivComp), true, false);

            if (_itemSlots.GetItemOrNull(ivId, ivComp.Slot) is not { } pack)
                continue;

            if (!TryComp(pack, out BloodPackComponent? packComponent))
                continue;

            if (!_solutionContainer.TryGetSolution(pack, packComponent.Solution, out var packSolution))
                continue;

            if (!TryComp(attachedTo, out BloodstreamComponent? targetBloodstream))
                continue;

            if (time < ivComp.TransferAt)
                continue;

            if (ivComp.Injecting)
            {
                if (targetBloodstream.BloodSolution.Volume < targetBloodstream.BloodSolution.MaxVolume)
                {
                    _solutionContainer.TryTransferSolution(pack, attachedTo, packSolution, targetBloodstream.BloodSolution, ivComp.TransferAmount);
                }
            }
            else
            {
                if (packSolution.Volume < packSolution.MaxVolume)
                {
                    _solutionContainer.TryTransferSolution(attachedTo, pack, targetBloodstream.BloodSolution, packSolution, ivComp.TransferAmount);
                }
            }

            ivComp.TransferAt = time + ivComp.TransferDelay;
            Dirty(ivId, ivComp);
        }
    }
}
