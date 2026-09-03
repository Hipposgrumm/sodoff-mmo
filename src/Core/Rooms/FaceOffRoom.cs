using System;
using sodoffmmo.Data;

namespace sodoffmmo.Core;

public class FaceOffRoom : HeadToHeadRoom {
    private class FaceOffMatchmakingHandler : MatchmakingHandler {
        internal static readonly FaceOffMatchmakingHandler instance = new();
        protected override int _MaxPlayers => 2;
        protected override HeadToHeadRoom CreateNewInstance(string roomgroup) {
            return new FaceOffRoom(roomgroup);
        }
    }

    protected class FaceOffStatus : Status {
        public string petname = "";
        public string? trick;

        public FaceOffStatus(string uid) : base(uid) {}
    }

    public FaceOffRoom(string roomname) : base(roomname) {}
    
    protected override Status CreateStatus(Client client) => new FaceOffStatus(client.PlayerData.Uid);

    protected override string[] WritePlayer(KeyValuePair<Client, Status> player) => [
        player.Value.uid,
        player.Value.isReady.ToString(),
        (player.Value as FaceOffStatus)!.petname
    ];

    protected override string[] AddDataJoin() => [];
    protected override string[] AddDataLeave() => [];
    protected override string[] AddDataPlayAgain() => [];

    public void AssignPetName(Client client, string petname) {
        (players[client] as FaceOffStatus)!.petname = petname;
    }

    public void SelectTrick(Client client, string trickname) {
        lock (base.roomLock) {
            var clientStatus = (players[client] as FaceOffStatus)!;
            clientStatus.trick = trickname;
            
            if (players.All(p => (p.Value as FaceOffStatus)!.trick != null)) {
                List<string> info = new();
                info.Add("FOTP");
                foreach (var player in players) {
                    var status = (player.Value as FaceOffStatus)!;
                    info.Add(status.uid);
                    info.Add(status.trick!);
                    
                    // Prepare for next turn. 
                    status.trick = null;
                }
                NetworkPacket packet = Utils.ArrNetworkPacket(info.ToArray(), "msg", base.Id);
                
                Send(packet);
            }
        }
    }

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

    static public void Join(Client client, string roomgroup) {
        FaceOffMatchmakingHandler.instance.Join(client, roomgroup);
    }
}
