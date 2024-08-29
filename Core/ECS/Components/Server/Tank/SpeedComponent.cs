using Vint.Core.ECS.Components.Server.Common;

namespace Vint.Core.ECS.Components.Server.Tank;

public class SpeedComponent : RangedComponent, IConvertible<Components.Battle.Parameters.Chassis.SpeedComponent> {
    public void Convert(Components.Battle.Parameters.Chassis.SpeedComponent component) =>
        component.Speed = FinalValue;
}
