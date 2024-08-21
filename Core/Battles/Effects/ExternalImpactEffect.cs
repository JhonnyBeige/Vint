using Vint.Core.Battles.Player;
using Vint.Core.Battles.Weapons;
using Vint.Core.ECS.Entities;
using Vint.Core.ECS.Templates.Battle.Effect;

namespace Vint.Core.Battles.Effects;

public class ExternalImpactEffect(
    TimeSpan cooldown,
    IEntity marketEntity,
    float radius,
    float minPercent,
    float maxDamage,
    float minDamage,
    float impact,
    BattleTank tank,
    int level
) : Effect(tank, level), IModuleWeaponEffect {
    public ModuleWeaponHandler WeaponHandler { get; private set; } = null!;

    public override async Task Activate() {
        if (IsActive) return;

        Tank.Effects.Add(this);

        Entity = new ExternalImpactEffectTemplate().Create(Tank.BattlePlayer,
            Duration,
            Battle.Properties.FriendlyFire,
            impact,
            minPercent,
            0,
            radius);

        WeaponHandler = new ExternalImpactWeaponHandler(Tank,
            cooldown,
            marketEntity,
            Entity,
            true,
            0,
            radius,
            minPercent,
            maxDamage,
            minDamage,
            int.MaxValue);

        await ShareAll();
        Schedule(Duration, Deactivate);
    }

    public override async Task Deactivate() {
        if (!IsActive) return;

        Tank.Effects.TryRemove(this);

        await UnshareAll();
        Entity = null;
    }
}
