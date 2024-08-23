using Vint.Core.Battles.Player;
using Vint.Core.Battles.Weapons;
using Vint.Core.ECS.Entities;
using Vint.Core.ECS.Templates.Battle.Effect;

namespace Vint.Core.Battles.Effects;

public class FireRingEffect(
    TimeSpan cooldown,
    IEntity marketEntity,
    float radius,
    float minDamagePercent,
    float impact,
    float temperatureLimit,
    float temperatureDelta,
    float heatDamage,
    BattleTank tank,
    int level
) : WeaponEffect(tank, level) {
    public override ModuleWeaponHandler WeaponHandler { get; protected set; } = null!;

    float MinDamageRadius => radius;
    float MaxDamageRadius => 0;

    public override async Task Activate() {
        if (IsActive) return;

        Tank.Effects.Add(this);

        WeaponEntity = Entity = new FireRingEffectTemplate().Create(Tank.BattlePlayer,
            Duration,
            Battle.Properties.FriendlyFire,
            impact,
            minDamagePercent,
            MinDamageRadius,
            MaxDamageRadius);

        WeaponHandler = new FireRingWeaponHandler(Tank, cooldown, marketEntity, Entity, temperatureLimit, temperatureDelta, heatDamage,
            minDamagePercent, MinDamageRadius, MaxDamageRadius);

        await ShareToAllPlayers();
        Schedule(TimeSpan.FromSeconds(10), Deactivate);
    }

    public override async Task Deactivate() {
        if (!IsActive) return;

        Tank.Effects.TryRemove(this);

        await UnshareFromAllPlayers();
        Entity = null;
    }
}
