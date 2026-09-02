using System;
using sodoffmmo.Data;

namespace sodoffmmo.Core;
public class GauntletRoom : HeadToHeadRoom {
    static object NextRoomLock = new object();
    static Dictionary<string, GauntletRoom?> NextRoom = new();

    public static GauntletRoom Get(string roomgroup) {
        lock(NextRoomLock) {
            if (
                NextRoom.TryGetValue(roomgroup, out var ret) && ret != null &&
                ret.ClientsCount == 1
            ) {
                NextRoom[roomgroup] = null; // probably more efficient than adding and removing every time
                return ret;
            } else {
                var newRoom = new GauntletRoom();
                NextRoom[roomgroup] = newRoom;
                return newRoom;
            }
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
    
    public override void AddPlayer(Client client) {
        players[client] = new GauntletStatus(client.PlayerData.Uid);
    }

    protected override string[] WritePlayer(KeyValuePair<Client, Status> player) => [
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

    static object joinLock = new object();

    // Keeping this here in case changing it breaks something.
    static public void Join(Client client, GauntletRoom? room = null) {
        lock(joinLock) {
            if (room is null)
                room = GauntletRoom.Get("GauntletDO");

            client.SetRoom(room);
            room.AddPlayer(client);
            room.SendUJR();
        }
    }

    static public void Join(Client client, string roomgroup) {
        lock(joinLock) {
            GauntletRoom room = GauntletRoom.Get(roomgroup);
            client.SetRoom(room);
            room.AddPlayer(client); // client will be not removed from GauntletRoom.players ... after remove all client from room whole GauntletRoom.players will be removed
            room.SendUJR();
        }
    }
}
