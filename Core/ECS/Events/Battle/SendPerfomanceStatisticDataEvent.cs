using Vint.Core.Battles.Player;
using Vint.Core.ECS.Entities;
using Vint.Core.Protocol.Attributes;
using Vint.Core.Server;

namespace Vint.Core.ECS.Events.Battle;

[ProtocolId(1470658766256)]
public class SendPerfomanceStatisticDataEvent : IServerEvent { // todo: what the fuck am i supposed to do with this information?
    public PerformanceStatisticData Data { get; set; } = null!;

    public void Execute(IPlayerConnection connection, IEnumerable<IEntity> entities) { }
}
