using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

using System.Timers;

namespace sodoffmmo.CommandHandlers;

// Join Any Room
[ExtensionCommandHandler("bg.JAR")]
class BattleGameJoinRoomHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        BattleGameRoom.Join(client, p.Get<string>("RG"));
        return Task.CompletedTask;
    }
}

// BattleGame Pet
[ExtensionCommandHandler("bg.BGP")]
class BattleGamePetHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BattleGameRoom room = (client.Room as BattleGameRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");

        string petname             = p.Get<string>("0");
        string rentalPetIndex      = p.Get<string>("1");
        string currentXP           = p.Get<string>("2");
        string upgradedSuperAttack = p.Get<string>("3");
        string hasShield           = p.Get<string>("4");
        
        room.AssignPetData(client, petname, rentalPetIndex, currentXP, upgradedSuperAttack, hasShield);
        
        NetworkPacket packet = Utils.ArrNetworkPacket([
            "BGP",
            client.PlayerData.Uid,
            petname, rentalPetIndex, currentXP, upgradedSuperAttack, hasShield
        ], "msg", room.Id);
        room.Send(packet);
        
        return Task.CompletedTask;
    }
}

// Lobby User Ready
[ExtensionCommandHandler("bg.LUR")]
class BattleGameLobbyUserReadyHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BattleGameRoom room = (client.Room as BattleGameRoom)!;
        room.SetPlayerReady(client);

        NetworkPacket packet = Utils.ArrNetworkPacket([
            "LUR",
            client.PlayerData.Uid
        ], "msg", room.Id);

        room.Send(packet);

        if (room.GetReadyCount() > 1) {
            room.OnGameStart();
            packet = Utils.ArrNetworkPacket([
                "LCDD", // Lobby CountDown Done
                (room.Host ?? client).PlayerData.Uid
            ], "msg", room.Id);
            
            room.Send(packet);
        }
        return Task.CompletedTask;
    }
}

// Lobby User Not Ready
[ExtensionCommandHandler("bg.LUNR")]
class BattleGameLobbyUserNotReadyHandler : CommandHandler  {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BattleGameRoom room = (client.Room as BattleGameRoom)!;
        room.SetPlayerReady(client, false);

        NetworkPacket packet = Utils.ArrNetworkPacket([
            "LUNR",
            client.PlayerData.Uid
        ], "msg", room.Id);

        room.Send(packet);
        return Task.CompletedTask;
    }
}

// Preload Game Data
[ExtensionCommandHandler("bg.PLGD")]
class BattleGamePreloadGameDataHandler : CommandHandler  {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        // And what am I supposed to do with this information?
        // The game works fine without doing anything here.
        return Task.CompletedTask;
    }
}

// Game Level Loaded
[ExtensionCommandHandler("bg.GLLD")]
class BattleGameLevelLoadedHandler : CommandHandler  {
    private System.Timers.Timer? timer = null;
    private int counter;
    private BattleGameRoom room;

    public override Task Handle(Client client, NetworkObject receivedObject) {
        room = (client.Room as BattleGameRoom)!;
        
        room.GetState(client).Reset();
        if (room.OnClientGameLoaded(client)) {
            counter = 3;

            NetworkPacket packet = Utils.ArrNetworkPacket([
                "GCDS", // Game CountDown Start
                (--counter).ToString()
            ], "msg", room.Id);
            room.Send(packet);

            timer = new System.Timers.Timer(1500);
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Elapsed += OnTick;
        }
        return Task.CompletedTask;
    }

    private void OnTick(Object? source, ElapsedEventArgs e) {
        NetworkPacket packet;
        if (--counter > 0) {
            packet = Utils.ArrNetworkPacket([
                "GCDU", // Game CountDown Update
                counter.ToString()
            ], "msg", room.Id);
        } else {
            packet = Utils.ArrNetworkPacket([
                "GS", // Game Start
            ], "msg", room.Id);
            
            timer!.Stop();
            timer!.Close();
            timer = null;
        }
        room.Send(packet);
    }
}

// Game Complete
[ExtensionCommandHandler("bg.GC")]
class BattleGameGameCompleteHandler : CommandHandler  {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BattleGameRoom room = (client.Room as BattleGameRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        
        room.ProcessResult(client, p.Get<string>("0"), p.Get<string>("1"));
        return Task.CompletedTask;
    }
}

// Pet States
[ExtensionCommandHandler("bg.ST")]
class BattleGamePetStatesHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BattleGameRoom room = (client.Room as BattleGameRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");

        NetworkPacket packet = Utils.ArrNetworkPacket([
            "ST",
            client.PlayerData.Uid,
            p.Get<string>("0")
        ], "msg", room.Id);

        room.Send(packet);
        return Task.CompletedTask;
    }
}

// Set XP
[ExtensionCommandHandler("bg.XP")]
class BattleGameSetXPHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BattleGameRoom room = (client.Room as BattleGameRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");

        NetworkPacket packet = Utils.ArrNetworkPacket([
            "XP",
            client.PlayerData.Uid,
            p.Get<string>("0")
        ], "msg", room.Id);

        room.Send(packet);
        return Task.CompletedTask;
    }
}

// Set Health
[ExtensionCommandHandler("bg.SH")]
class BattleGameSetHealthHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BattleGameRoom room = (client.Room as BattleGameRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");

        float.TryParse(p.Get<string>("0"), out var health);
        var state = room.GetState(client);
        state.Health = health;

        NetworkPacket packet = Utils.ArrNetworkPacket([
            "SH",
            client.PlayerData.Uid,
            health.ToString()
        ], "msg", room.Id);

        room.Send(packet);
        return Task.CompletedTask;
    }
}

// Set Score
[ExtensionCommandHandler("bg.SS")]
class BattleGameSetScoreHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BattleGameRoom room = (client.Room as BattleGameRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");

        int.TryParse(p.Get<string>("0"), out var score);
        var state = room.GetState(client);
        state.Score = score;

        NetworkPacket packet = Utils.ArrNetworkPacket([
            "SS",
            client.PlayerData.Uid,
            score.ToString()
        ], "msg", room.Id);

        room.Send(packet);
        return Task.CompletedTask;
    }
}

// Set DamageAttack
[ExtensionCommandHandler("bg.DA")]
class BattleGameSetDamageAttackHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BattleGameRoom room = (client.Room as BattleGameRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");

        NetworkPacket packet = Utils.ArrNetworkPacket([
            "DA",
            client.PlayerData.Uid,
            p.Get<string>("0")
        ], "msg", room.Id);

        room.Send(packet);
        return Task.CompletedTask;
    }
}

// Set Shield
[ExtensionCommandHandler("bg.SD")]
class BattleGameSetShieldHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BattleGameRoom room = (client.Room as BattleGameRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");

        NetworkPacket packet = Utils.ArrNetworkPacket([
            "SD",
            client.PlayerData.Uid,
            p.Get<string>("0")
        ], "msg", room.Id);

        room.Send(packet);
        return Task.CompletedTask;
    }
}

// On Damage
[ExtensionCommandHandler("bg.OD")]
class BattleGameOnDamageHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BattleGameRoom room = (client.Room as BattleGameRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");

        string target = p.Get<string>("0");
        float.TryParse(p.Get<string>("1"), out var damageDealt);
        bool .TryParse(p.Get<string>("2"), out var wasBlocked);
        bool .TryParse(p.Get<string>("3"), out var wasShielded);
        float.TryParse(p.Get<string>("4"), out var targetSuperMeter);
        int  .TryParse(p.Get<string>("5"), out var pointsMade);
        bool .TryParse(p.Get<string>("6"), out var wasSuperAttack);
        float.TryParse(p.Get<string>("7"), out var attackerSuperMeter);

        var targetState = room.GetState(target);
        if (targetState == null) return Task.CompletedTask;
        var attackerState = room.GetState(client);
        
        NetworkPacket packet = Utils.ArrNetworkPacket([
            "OD",
            target, // Target Player
            damageDealt.ToString(), // damage
            wasBlocked.ToString(), // wasBlocked
            targetState.DamageHealth(damageDealt).ToString(), // newHealth
            wasShielded.ToString(), // hasShield
            targetState.AddSuperAttack(targetSuperMeter).ToString(), // attackMeterValue
            
            client.PlayerData.Uid, // Attacker
            pointsMade.ToString(), // addedScore
            attackerState.AddScore(pointsMade).ToString(), // newScore
            wasSuperAttack.ToString(), // wasSuperAttack
            (wasSuperAttack
                ? attackerState.UseSuperAttack()
                : attackerState.AddSuperAttack(attackerSuperMeter)
            ).ToString() // attackMeterValue
        ], "msg", room.Id);
        room.Send(packet);

        if (targetState.Health <= 0f) {
            NetworkPacket endPacket = Utils.ArrNetworkPacket([
                "RO",
                bool.FalseString, // wasTie - idk when this would ever be the case
                                  //     since when the timer runs out this is handled by the clients
                client.PlayerData.Uid,
                attackerState.Health.ToString(),
                target,
                targetState.Health.ToString()
            ], "msg", room.Id);
            room.Send(endPacket);
        }

        return Task.CompletedTask;
    }
}

// Play Again
[ExtensionCommandHandler("bg.PA")]
class BattleGamePlayAgainHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        BattleGameRoom room = (client.Room as BattleGameRoom)!;
        room.SetPlayerReady(client, false);

        room.SendPA(client);
        return Task.CompletedTask;
    }
}