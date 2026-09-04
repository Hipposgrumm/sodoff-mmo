using System;
using sodoffmmo.Data;

namespace sodoffmmo.Core;
public class GauntletRoom : HeadToHeadRoom {
    private class GauntletMatchmakingHandler : MatchmakingHandler {
        internal static readonly GauntletMatchmakingHandler instance = new();
        protected override int _MaxPlayers => 2;
        protected override HeadToHeadRoom CreateNewInstance(string roomgroup) {
            return new GauntletRoom();
        }
    }

    protected class GauntletStatus : Status {
        public string resultA = "";
        public string resultB = "";

        public GauntletStatus(string uid) : base(uid) {}
    }

    public GauntletRoom() : base ("GauntletDO") {
        base.RoomVariables.Add(NetworkArray.VlElement("IS_RACE_ROOM", true));
        Name = Name.Replace('_', '-'); // Fix for Math Blaster (it doesn't like underscores)
    }
    
    protected override Status CreateStatus(Client client) => new GauntletStatus(client.PlayerData.Uid);

    protected override string[] WritePlayer(KeyValuePair<Client, Status> player, bool isJoin) => [
        player.Value.uid,
        player.Value.isReady.ToString(),
        "1" // TODO this should be player gender
    ];

    protected override string[] AddDataJoin() => [
        base.Id.ToString(),
        "2" // Course
    ];
    protected override string[] AddDataLeave() => [
        base.Id.ToString()
    ];

    protected override string[] AddDataPlayAgain() => [
        base.Id.ToString(),
        "1" // Course
    ];

    public override void RemoveClient(Client client) {
        base.RemoveClient(client);
        Joinable = true;
    }

    public bool ProcessResult(Client client, string resultA, string resultB) {
        lock (base.roomLock) {
            var clientStatus = (players[client] as GauntletStatus)!;
            clientStatus.resultA = resultA;
            clientStatus.resultB = resultB;

            int count = 0;
            foreach(var player in players) {
                var status = (player.Value as GauntletStatus)!;
                if (status.resultB != "") ++count;
            }
            if (count != 2)
                return false;

            // {"a":13,"c":1,"p":{"c":"msg","p":{"arr":["GC","365587","03a3ad99-87a5-4af4-8966-0b2733a05e0f","10850","79","1","bff0c312-8763-497d-aa0c-a5dfc7d8b861","21050","73","1"]},"r":365587}}
            List<string> info = new();
            info.Add("GC");
            info.Add(base.Id.ToString());
            foreach(var player in players) {
                var status = (player.Value as GauntletStatus)!;
                if (status.resultB == "")
                    continue;
                info.Add(status.uid);
                info.Add(status.resultA);
                info.Add(status.resultB);
                info.Add("1");
            }
            NetworkPacket packet = Utils.ArrNetworkPacket(info.ToArray(), "msg", base.Id);

            Send(packet);
            return true;
        }
    }
    
    // Keeping this here in case changing it breaks something.
    static object joinLock = new object();
    static public void Join(Client client, GauntletRoom? room = null) {
        lock(joinLock) {
            if (room is null)
                room = (GauntletMatchmakingHandler.instance.Get("GauntletDO") as GauntletRoom)!;

            client.SetRoom(room);
            room.AddPlayer(client);
            room.SendUJR();
        }
    }

    static public void Join(Client client, string roomgroup) {
        GauntletMatchmakingHandler.instance.Join(client, roomgroup);
    }
}
