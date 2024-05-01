using Vint.Core.ECS.Entities;
using Vint.Core.Protocol.Attributes;
using Vint.Core.Server;

namespace Vint.Core.ECS.Events.Battle;

[ProtocolId(1498743823980)]
public class ReturnToCustomBattleEvent : IServerEvent {
    public Task Execute(IPlayerConnection connection, IEnumerable<IEntity> entities) {
        if (!connection.InLobby || connection.BattlePlayer!.InBattle) return Task.CompletedTask;

        connection.BattlePlayer.Init();
        return Task.CompletedTask;
    }
}
