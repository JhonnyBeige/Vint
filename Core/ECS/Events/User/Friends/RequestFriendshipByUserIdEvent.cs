using LinqToDB;
using Vint.Core.Database;
using Vint.Core.Database.Models;
using Vint.Core.ECS.Entities;
using Vint.Core.ECS.Enums;
using Vint.Core.Protocol.Attributes;
using Vint.Core.Server;

namespace Vint.Core.ECS.Events.User.Friends;

[ProtocolId(1506939447770)]
public class RequestFriendshipByUserIdEvent : IServerEvent {
    public InteractionSource InteractionSource { get; set; }
    public long SourceId { get; set; }
    public long UserId { get; set; }

    public async Task Execute(IPlayerConnection connection, IEnumerable<IEntity> entities) {
        await using DbConnection db = new();
        Player? player = db.Players.SingleOrDefault(player => player.Id == UserId);

        if (player == null) return;

        await db.BeginTransactionAsync();

        Relation? thisToTargetRelation = await db.Relations
            .SingleOrDefaultAsync(relation => relation.SourcePlayerId == connection.Player.Id &&
                                         relation.TargetPlayerId == player.Id);

        Relation? targetToThisRelation = await db.Relations
            .SingleOrDefaultAsync(relation => relation.SourcePlayerId == player.Id &&
                                         relation.TargetPlayerId == connection.Player.Id);

        if ((targetToThisRelation?.Types & RelationTypes.Blocked) == RelationTypes.Blocked)
            return;

        if (thisToTargetRelation != null &&
            targetToThisRelation != null &&
            (thisToTargetRelation.Types & RelationTypes.IncomingRequest) == RelationTypes.IncomingRequest &&
            (targetToThisRelation.Types & RelationTypes.OutgoingRequest) == RelationTypes.OutgoingRequest) {
            thisToTargetRelation.Types = thisToTargetRelation.Types & ~RelationTypes.IncomingRequest | RelationTypes.Friend;
            targetToThisRelation.Types = targetToThisRelation.Types & ~RelationTypes.OutgoingRequest | RelationTypes.Friend;

            await db.UpdateAsync(thisToTargetRelation);
            await db.UpdateAsync(targetToThisRelation);
        } else {
            if (thisToTargetRelation == null) {
                await db.InsertAsync(new Relation { SourcePlayer = connection.Player, TargetPlayer = player, Types = RelationTypes.OutgoingRequest });
            } else {
                thisToTargetRelation.Types &= ~RelationTypes.Blocked;
                thisToTargetRelation.Types |= RelationTypes.OutgoingRequest;
                await db.UpdateAsync(thisToTargetRelation);
            }

            if (targetToThisRelation == null) {
                await db.InsertAsync(new Relation { SourcePlayer = player, TargetPlayer = connection.Player, Types = RelationTypes.IncomingRequest });
            } else {
                targetToThisRelation.Types |= RelationTypes.IncomingRequest;
                await db.UpdateAsync(targetToThisRelation);
            }
        }

        await db.CommitTransactionAsync();

        IPlayerConnection? targetConnection = connection.Server.PlayerConnections.Values
            .Where(conn => conn.IsOnline)
            .SingleOrDefault(conn => conn.Player.Id == player.Id);

        targetConnection?.Send(new IncomingFriendAddedEvent(connection.Player.Id), targetConnection.User);
    }
}
