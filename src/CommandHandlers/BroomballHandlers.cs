using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

// Add Player
[ExtensionCommandHandler("brb.AP")]
class BroomballPlayHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BroomballRoom room = (client.Room as BroomballRoom)!;
        room.GameAddPlayer(client);
        return Task.CompletedTask;
    }
}

// User Logon
[ExtensionCommandHandler("bb.UL")]
class BroomballUserLogonHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        // This endpoint is only called immediately after BG:PT is received,
        //     and doesn't receive any data from the client.
        // Implementing something into this would likely only confuse the server,
        //     since it would get called every time the teams are updated (an event
        //     the server is well aware of, being the one that initiated it).
        return Task.CompletedTask;
    }
}

// Level Loaded
[ExtensionCommandHandler("brb.LL")]
class BroomballLevelLoadedHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        // This endpoint is called when the game countdown ends (not lobby one).
        // Seemingly nothing meaningful can be implemented here.
        return Task.CompletedTask;
    }
}

// Remove Player
[ExtensionCommandHandler("brb.TR")]
class BroomballRemoveFromTeamHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BroomballRoom room = (client.Room as BroomballRoom)!;
        room.GameRemovePlayer(client);
        return Task.CompletedTask;
    }
}

// Reset Single Player
[ExtensionCommandHandler("brb.RS")]
class BroomballResetSinglePlayerHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BroomballRoom room = (client.Room as BroomballRoom)!;
        room.GameRemovePlayer(client);
        return Task.CompletedTask;
    }
}

// Update Score
[ExtensionCommandHandler("brb.US")]
class BroomballUpdateScoreHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BroomballRoom room = (client.Room as BroomballRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");

        
        BroomballRoom.Team? team = p.Get<string>("TE") switch {
            BroomballRoom.TEAMID_RED  => room.TEAM_RED,
            BroomballRoom.TEAMID_BLUE => room.TEAM_BLUE,
            _ => null
        };
        if (team == null) return Task.CompletedTask;
        
        if (int.TryParse(p.Get<string>("GH"), out var points)) {
            team.Points += points;
        }
        
        return Task.CompletedTask;
    }
}
