using Vint.Core.Battles.Player;

namespace Vint.Core.Battles.Weapons;

public class VulcanWeaponHandler(
    BattleTank battleTank
) : StreamWeaponHandler(battleTank) {
    public override int MaxHitTargets => 1;
}