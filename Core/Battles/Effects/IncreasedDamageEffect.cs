using Vint.Core.Battles.Player;
using Vint.Core.Config;
using Vint.Core.ECS.Components.Battle.Effect;
using Vint.Core.ECS.Components.Server;
using Vint.Core.ECS.Components.Server.Effect;
using Vint.Core.ECS.Templates.Battle.Effect;
using Vint.Core.Utils;
using DurationComponent = Vint.Core.ECS.Components.Battle.Effect.DurationComponent;
using EffectDurationComponent = Vint.Core.ECS.Components.Server.DurationComponent;

namespace Vint.Core.Battles.Effects;

public sealed class IncreasedDamageEffect : DurationEffect, ISupplyEffect, IDamageMultiplierEffect, IExtendableEffect {
    const string EffectConfigPath = "battle/effect/damage";
    const string MarketConfigPath = "garage/module/upgrade/properties/increaseddamage";

    public IncreasedDamageEffect(BattleTank tank, int level = -1) : base(tank, level, MarketConfigPath) {
        SupplyMultiplier = ConfigManager.GetComponent<DamageEffectComponent>(EffectConfigPath).Factor;
        SupplyDurationMs = ConfigManager.GetComponent<EffectDurationComponent>(EffectConfigPath).Duration * Tank.SupplyDurationMultiplier;

        Multiplier = IsSupply ? SupplyMultiplier : Leveling.GetStat<ModuleDamageEffectMaxFactorPropertyComponent>(MarketConfigPath, Level);

        if (IsSupply)
            Duration = TimeSpan.FromMilliseconds(SupplyDurationMs);
    }

    public float Multiplier { get; private set; }

    public float GetMultiplier(BattleTank source, BattleTank target, bool isSplash, bool isBackHit, bool isTurretHit) =>
        IsActive && (Tank != target || isSplash) ? Multiplier : 1;

    public void Extend(int newLevel) {
        if (!IsActive) return;

        UnScheduleAll();

        bool isSupply = newLevel < 0;

        if (isSupply) {
            Duration = TimeSpan.FromMilliseconds(SupplyDurationMs);
            Multiplier = SupplyMultiplier;
        } else {
            Duration = TimeSpan.FromMilliseconds(DurationsComponent.UpgradeLevel2Values[newLevel]);
            Multiplier = Leveling.GetStat<ModuleDamageEffectMaxFactorPropertyComponent>(MarketConfigPath, newLevel);
        }

        Level = newLevel;

        Entity!.ChangeComponent<DurationConfigComponent>(component => component.Duration = Convert.ToInt64(Duration.TotalMilliseconds));
        Entity!.RemoveComponent<DurationComponent>();
        Entity!.AddComponent(new DurationComponent(DateTimeOffset.UtcNow));

        Schedule(Duration, Deactivate);
    }

    public float SupplyMultiplier { get; }
    public float SupplyDurationMs { get; }

    public override void Activate() {
        if (IsActive) return;

        Tank.Effects.Add(this);

        Entities.Add(new DamageEffectTemplate().Create(Tank.BattlePlayer, Duration));
        ShareAll();

        Schedule(Duration, Deactivate);
    }

    public override void Deactivate() {
        if (!IsActive) return;

        Tank.Effects.TryRemove(this);

        UnshareAll();
        Entities.Clear();
    }
}
