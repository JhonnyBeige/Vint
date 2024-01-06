using Vint.Core.ECS.Components.Battle.Weapon;
using Vint.Core.ECS.Components.Group;
using Vint.Core.ECS.Entities;
using Vint.Core.Protocol.Attributes;

namespace Vint.Core.ECS.Templates.Battle.Weapon;

[ProtocolId(636046254605033322)]
public class WeaponSkinBattleItemTemplate : EntityTemplate {
    public IEntity Create(IEntity skin, IEntity tank) => Entity(skin.TemplateAccessor!.ConfigPath, builder => 
        builder
            .AddComponent(new WeaponSkinBattleItemComponent())
            .AddComponent(tank.GetComponent<UserGroupComponent>())
            .AddComponent(tank.GetComponent<BattleGroupComponent>())
            .AddComponent(tank.GetComponent<TankGroupComponent>())
            .AddComponent(skin.GetComponent<MarketItemGroupComponent>()));
}