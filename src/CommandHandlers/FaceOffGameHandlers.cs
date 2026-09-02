using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

// Join Any Room
[ExtensionCommandHandler("fo.JD")]
class FaceOffJoinRoomHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        FaceOffRoom.Join(client, p.Get<string>("RG"));
        return Task.CompletedTask;
    }
}

// FaceOff Pet Name
[ExtensionCommandHandler("fog.FOPN")]
class FaceOffPetNameHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        
        NetworkPacket packet = Utils.ArrNetworkPacket([
            "FOPN",
            client.PlayerData.Uid,
            p.Get<string>("0")
        ], "msg", client.Room!.Id);
        
        client.Room!.Send(packet);
        return Task.CompletedTask;
    }
}

// Lobby User Ready
[ExtensionCommandHandler("fog.LUR")]
class FaceOffLobbyUserReadyHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        FaceOffRoom room = (client.Room as FaceOffRoom)!;
        room.SetPlayerReady(client);

        NetworkPacket packet = Utils.ArrNetworkPacket([
            "LUR",
            client.PlayerData.Uid
        ], "msg", room.Id);

        room.Send(packet);

        if (room.GetReadyCount() > 1) {
            packet = Utils.ArrNetworkPacket([
                "LCDD", // Lobby CountDown Done
                client.PlayerData.Uid
            ], "msg", room.Id);
            
            room.Send(packet);
        }
        return Task.CompletedTask;
    }
}

// Lobby User Not Ready
[ExtensionCommandHandler("fog.LUNR")]
class FaceOffLobbyUserNotReadyHandler : CommandHandler  {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        FaceOffRoom room = (client.Room as FaceOffRoom)!;
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
[ExtensionCommandHandler("fog.GLLD")]
class FaceOffLevelLoadedHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        FaceOffRoom room = (client.Room as FaceOffRoom)!;
        
        NetworkPacket packet = Utils.ArrNetworkPacket([
            "GS", // Game Start
            room.Id.ToString()
        ], "msg", room.Id);
        room.Send(packet);
        
        return Task.CompletedTask;
    }
}

// Game Complete
[ExtensionCommandHandler("fog.GC")]
class FaceOffGameCompleteHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        FaceOffRoom room = (client.Room as FaceOffRoom)!;
        room.ProcessResult(client);
        return Task.CompletedTask;
    }
}

// Trick Selected
[ExtensionCommandHandler("fog.RGD")]
class FaceOffTrickPerformHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        FaceOffRoom room = (client.Room as FaceOffRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        
        room.SelectTrick(client, p.Get<string>("0"));
        return Task.CompletedTask;
    }
}

// Trick Not Selected
[ExtensionCommandHandler("fog.FOTNS")]
class FaceOffNoTrickHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        FaceOffRoom room = (client.Room as FaceOffRoom)!;
        room.SelectTrick(client, "");
        return Task.CompletedTask;
    }
}
