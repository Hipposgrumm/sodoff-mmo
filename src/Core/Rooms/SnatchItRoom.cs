using System;
using sodoffmmo.Data;

namespace sodoffmmo.Core;

public class SnatchItRoom : HeadToHeadRoom {
    private class SnatchItMatchmakingHandler : MatchmakingHandler {
        internal static readonly SnatchItMatchmakingHandler instance = new();
        protected override int _MaxPlayers => 2;
        protected override HeadToHeadRoom CreateNewInstance(string roomgroup) {
            return new SnatchItRoom(roomgroup);
        }
    }

    protected class SnatchItStatus : Status {
        public string petname = "";

        public string? resultScore = null;
        public string? resultCollectCount = null;

        public SnatchItStatus(string uid) : base(uid) {}
    }

    public SnatchItRoom(string roomname) : base(roomname) {}

    protected override Status CreateStatus(Client client) => new SnatchItStatus(client.PlayerData.Uid);

    protected override string[] WritePlayer(KeyValuePair<Client, Status> player) => [
        player.Value.uid,
        player.Value.isReady.ToString(),
        (player.Value as SnatchItStatus)!.petname+'/'
                                                 +"0" // ImageSlotIndex - Unknown and it's not used anywhere.
    ];

    protected override string[] AddDataJoin() => [
        base.Id.ToString()
    ];
    
    protected override string[] AddDataLeave() => [
        base.Id.ToString()
    ];
    
    protected override string[] AddDataPlayAgain() => [
        base.Id.ToString()
    ];

    public void AssignPetName(Client client, string petname) {
        (players[client] as SnatchItStatus)!.petname = petname;
    }
    
    public bool ProcessResult(Client client, string score, string collectCount) {
        lock (base.roomLock) {
            var clientStatus = (players[client] as SnatchItStatus)!;
            clientStatus.resultScore = score;
            clientStatus.resultCollectCount = collectCount;

            if (players.Any(p => (p.Value as SnatchItStatus)!.resultScore == null))
                return false;
            
            List<string> info = new();
            info.Add("GC");
            info.Add(base.Id.ToString());
            foreach(var player in players) {
                var status = (player.Value as SnatchItStatus)!;
                if (status.resultScore == null) continue;
                info.Add(status.uid);
                info.Add(status.resultScore!);
                info.Add(status.resultCollectCount!);
            }
            NetworkPacket packet = Utils.ArrNetworkPacket(info.ToArray(), "msg", base.Id);

            Send(packet);
            return true;
        }
    }

    static public void Join(Client client, string roomgroup) {
        SnatchItMatchmakingHandler.instance.Join(client, roomgroup);
    }
}
