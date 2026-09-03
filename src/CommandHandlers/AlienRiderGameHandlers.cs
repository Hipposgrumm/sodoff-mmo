using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

// Join Any Room
[ExtensionCommandHandler("alr.JD")]
class AlienRiderJoinRoomHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        AlienRiderRoom.Join(client, p.Get<string>("RG"));
        return Task.CompletedTask;
    }
}

// Lobby User Ready
[ExtensionCommandHandler("alrg.LUR")]
class AlienRiderLobbyUserReadyHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        AlienRiderRoom room = (client.Room as AlienRiderRoom)!;
        room.SetPlayerReady(client);

        NetworkPacket packet = Utils.ArrNetworkPacket([
            "LUR",
            client.PlayerData.Uid
        ], "msg", room.Id);

        room.Send(packet);

        if (room.GetReadyCount() > 1) {
            room.OnGameStart();
            packet = Utils.ArrNetworkPacket([
                "LCDD" // Lobby CountDown Done
            ], "msg", room.Id);
            
            room.Send(packet);
        }
        return Task.CompletedTask;
    }
}

// Lobby User Not Ready
[ExtensionCommandHandler("alrg.LUNR")]
class AlienRiderLobbyUserNotReadyHandler : CommandHandler  {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        AlienRiderRoom room = (client.Room as AlienRiderRoom)!;
        room.SetPlayerReady(client, false);

        NetworkPacket packet = Utils.ArrNetworkPacket([
            "LUNR",
            client.PlayerData.Uid
        ], "msg", room.Id);

        room.Send(packet);
        return Task.CompletedTask;
    }
}

// Game Level Loaded
[ExtensionCommandHandler("alrg.GLLD")]
class AlienRiderLevelLoadedHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        AlienRiderRoom room = (client.Room as AlienRiderRoom)!;
        
        NetworkPacket packet = Utils.ArrNetworkPacket([
            "GS", // Game Start
            room.Id.ToString()
        ], "msg", room.Id);
        room.Send(packet);
        
        return Task.CompletedTask;
    }
}

// Game Complete
[ExtensionCommandHandler("alrg.GC")]
class AlienRiderGameCompleteHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        AlienRiderRoom room = (client.Room as AlienRiderRoom)!;
        room.ProcessResult(client);
        return Task.CompletedTask;
    }
}

