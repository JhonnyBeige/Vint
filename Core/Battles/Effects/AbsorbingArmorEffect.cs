using Vint.Core.Battles.Player;
using Vint.Core.Battles.Weapons;
using Vint.Core.Config;
using Vint.Core.ECS.Components.Battle.Effect;
using Vint.Core.ECS.Components.Server;
using Vint.Core.ECS.Components.Server.Effect;
using Vint.Core.ECS.Templates.Battle.Effect;
using DurationComponent = Vint.Core.ECS.Components.Battle.Effect.DurationComponent;
using EffectDurationComponent = Vint.Core.ECS.Components.Server.DurationComponent;

namespace Vint.Core.Battles.Effects;

public sealed class AbsorbingArmorEffect : DurationEffect, ISupplyEffect, IDamageMultiplierEffect, IExtendableEffect {
    const string EffectConfigPath = "battle/effect/armor";
    const string MarketConfigPath = "garage/module/upgrade/properties/absorbingarmor";
    
    public AbsorbingArmorEffect(BattleTank tank, int level = -1) : base(tank, level, MarketConfigPath) {
        MultipliersComponent = ConfigManager.GetComponent<ModuleArmorEffectPropertyComponent>(MarketConfigPath);
        
        SupplyMultiplier = ConfigManager.GetComponent<ArmorEffectComponent>(EffectConfigPath).Factor;
        SupplyDurationMs = ConfigManager.GetComponent<EffectDurationComponent>(EffectConfigPath).Duration;
        
        Multiplier = IsSupply ? SupplyMultiplier : MultipliersComponent[Level];
        
        if (IsSupply)
            Duration = TimeSpan.FromMilliseconds(SupplyDurationMs);
    }
    
    ModuleArmorEffectPropertyComponent MultipliersComponent { get; }
    public float Multiplier { get; private set; }
    
    public float GetMultiplier(BattleTank source, BattleTank target, bool isSplash) =>
        IsActive &&
        Tank == target &&
        (source.WeaponHandler is not IsisWeaponHandler ||
         source.IsEnemy(target)) ? Multiplier : 1;
    
    public void Extend(int newLevel) {
        if (!IsActive) return;
        
        UnScheduleAll();
        
        bool isSupply = newLevel < 0;
        
        if (isSupply) {
            Duration = TimeSpan.FromMilliseconds(SupplyDurationMs);
            Multiplier = SupplyMultiplier;
        } else {
            Duration = TimeSpan.FromMilliseconds(DurationsComponent[newLevel]);
            Multiplier = MultipliersComponent[newLevel];
        }
        
        Level = newLevel;
        LastActivationTime = DateTimeOffset.UtcNow;
        
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
        
        Entities.Add(new ArmorEffectTemplate().Create(EffectConfigPath, Tank.BattlePlayer, Duration));
        ShareAll();
        
        LastActivationTime = DateTimeOffset.UtcNow;
        Schedule(Duration, Deactivate);
    }
    
    public override void Deactivate() {
        if (!IsActive) return;
        
        Tank.Effects.TryRemove(this);
        
        UnshareAll();
        Entities.Clear();
        
        LastActivationTime = default;
    }
}