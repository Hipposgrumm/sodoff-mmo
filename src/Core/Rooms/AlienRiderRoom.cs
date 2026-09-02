using System;
using sodoffmmo.Data;

namespace sodoffmmo.Core;

public class AlienRiderRoom : HeadToHeadRoom {
    static object NextRoomLock = new object();
    static Dictionary<string, AlienRiderRoom?> NextRoom = new();

    public static AlienRiderRoom Get(string roomgroup) {
        lock(NextRoomLock) {
            if (
                NextRoom.TryGetValue(roomgroup, out var ret) && ret != null &&
                ret.ClientsCount == 1
            ) {
                NextRoom[roomgroup] = null; // probably more efficient than adding and removing every time
                return ret;
            } else {
                var newRoom = new AlienRiderRoom();
                NextRoom[roomgroup] = newRoom;
                return newRoom;
            }
        }
    }

    public AlienRiderRoom() : base("AlienRider") {}

    protected override string[] WritePlayer(KeyValuePair<Client, Status> player) => [
        player.Value.uid,
        player.Value.isReady.ToString()
    ];

    protected override string[] AddDataJoin() => [];
    protected override string[] AddDataPlayAgain() => [];

    public bool ProcessResult(Client client) {
        lock (base.roomLock) {
            List<string> info = new();
            info.Add("GC");
            info.Add(client.PlayerData.Uid);
            foreach(var player in players) {
                info.Add(player.Value.uid);
            }
            NetworkPacket packet = Utils.ArrNetworkPacket(info.ToArray(), "msg", base.Id);

            Send(packet);
            return true;
        }
    }

    static object joinLock = new object();

    static public void Join(Client client, string roomgroup, AlienRiderRoom? room = null) {
        lock(joinLock) {
            if (room is null)
                room = AlienRiderRoom.Get(roomgroup);

            client.SetRoom(room);
            room.AddPlayer(client);
            room.SendUJR();
        }
    }
}
