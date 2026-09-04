using System.Timers;
using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

// Join Any Room
[ExtensionCommandHandler("si.JAR")]
class SnatchItJoinRoomHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        SnatchItRoom.Join(client, p.Get<string>("RG"));
        return Task.CompletedTask;
    }
}

// Lobby User Ready
[ExtensionCommandHandler("si.LUR")]
class SnatchItLobbyUserReadyHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        SnatchItRoom room = (client.Room as SnatchItRoom)!;
        room.SetPlayerReady(client);

        NetworkPacket packet = Utils.ArrNetworkPacket([
            "LUR",
            room.Id.ToString(),
            client.PlayerData.Uid
        ], "msg", room.Id);

        room.Send(packet);

        if (room.GetReadyCount() > 1) {
            room.OnGameStart();
            packet = Utils.ArrNetworkPacket([
                "LCDD", // Lobby CountDown Done
                room.Id.ToString(),
                (room.Host ?? client).PlayerData.Uid
            ], "msg", room.Id);
            
            room.Send(packet);
        }
        return Task.CompletedTask;
    }
}

// Lobby User Not Ready
[ExtensionCommandHandler("si.LUNR")]
class SnatchItLobbyUserNotReadyHandler : CommandHandler  {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        SnatchItRoom room = (client.Room as SnatchItRoom)!;
        room.SetPlayerReady(client, false);

        NetworkPacket packet = Utils.ArrNetworkPacket([
            "LUNR",
            room.Id.ToString(),
            client.PlayerData.Uid
        ], "msg", room.Id);

        room.Send(packet);
        return Task.CompletedTask;
    }
}

// Game Level Load
[ExtensionCommandHandler("si.GLL")]
class SnatchItLevelLoadHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        SnatchItRoom room = (client.Room as SnatchItRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        
        NetworkPacket packet = Utils.ArrNetworkPacket([
            "GLL", // Game CountDown Start
            room.Id.ToString(),
            p.Get<string>("0")
        ], "msg", room.Id);
        room.Send(packet);
        
        if (room.OnClientGameLoaded(client)) SnatchItLevelLoadedHandler.StartTimer(room);
        return Task.CompletedTask;
    }
}

// Game Level Loaded
[ExtensionCommandHandler("si.GLLD")]
class SnatchItLevelLoadedHandler : CommandHandler  {

    public override Task Handle(Client client, NetworkObject receivedObject) {
        SnatchItRoom room = (client.Room as SnatchItRoom)!;
        if (room.OnClientGameLoaded(client)) StartTimer(room);
        return Task.CompletedTask;
    }


    internal static void StartTimer(SnatchItRoom room) {
        int counter = 5;
        
        NetworkPacket packet = Utils.ArrNetworkPacket([
            "GCDS", // Game CountDown Start
            room.Id.ToString(),
            (--counter).ToString()
        ], "msg", room.Id);
        room.Send(packet);

        System.Timers.Timer timer = new System.Timers.Timer(1500);
        timer.AutoReset = true;
        timer.Enabled = true;
        timer.Elapsed += (source, e) => {
            NetworkPacket packet;
            if (--counter > 0) {
                packet = Utils.ArrNetworkPacket([
                    "GCDU", // Game CountDown Update
                    room.Id.ToString(),
                    counter.ToString()
                ], "msg", room.Id);
            } else {
                packet = Utils.ArrNetworkPacket([
                    "GS", // Game Start
                    room.Id.ToString()
                ], "msg", room.Id);
            
                timer.Stop();
                timer.Close();
            }
            room.Send(packet);
        };
    }
}

// Relay Game Data
[ExtensionCommandHandler("si.RGD")]
class SnatchItRelayGameDataHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        SnatchItRoom room = (client.Room as SnatchItRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        
        NetworkPacket packet = Utils.ArrNetworkPacket([
            "RGD", // Relay Game Data
            room.Id.ToString(),
            p.Get<string>("0"),
            p.Get<string>("1"),
            p.Get<string>("2")
        ], "msg", room.Id);
        room.Send(packet, client);
        return Task.CompletedTask;
    }
}

// Game Complete
[ExtensionCommandHandler("si.GC")]
class SnatchItGameCompleteHandler : CommandHandler  {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        SnatchItRoom room = (client.Room as SnatchItRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        
        room.ProcessResult(client, p.Get<string>("0"), p.Get<string>("1"));
        return Task.CompletedTask;
    }
}

// Play Again
[ExtensionCommandHandler("si.PA")]
class SnatchItPlayAgainHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        GauntletRoom room = (client.Room as GauntletRoom)!;
        room.SetPlayerReady(client, false);

        room.SendPA(client);
        return Task.CompletedTask;
    }
}

// SnatchIt Pet Name
[ExtensionCommandHandler("si.SNPN")]
class SnatchItPetNameHandler : CommandHandler  {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        SnatchItRoom room = (client.Room as SnatchItRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        string petname = p.Get<string>("0");
        room.AssignPetName(client, petname);

        NetworkPacket packet = Utils.ArrNetworkPacket([
            "SNPN",
            room.Id.ToString(),
            client.PlayerData.Uid,
            petname
        ], "msg", room.Id);
        
        room.Send(packet);
        return Task.CompletedTask;
    }
}
