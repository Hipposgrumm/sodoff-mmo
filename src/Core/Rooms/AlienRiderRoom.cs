using System;
using sodoffmmo.Data;

namespace sodoffmmo.Core;

public class AlienRiderRoom : HeadToHeadRoom {
    private class AlienRiderMatchmakingHandler : MatchmakingHandler {
        internal static readonly AlienRiderMatchmakingHandler instance = new();
        protected override int _MaxPlayers => 2;
        protected override HeadToHeadRoom CreateNewInstance(string roomgroup) {
            return new AlienRiderRoom();
        }
    }

    public AlienRiderRoom() : base("AlienRider") {}

    protected override string[] WritePlayer(KeyValuePair<Client, Status> player, bool isJoin) => [
        player.Value.uid,
        player.Value.isReady.ToString()
    ];

    protected override string[] AddDataJoin() => [];
    protected override string[] AddDataLeave() => [];
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

    static public void Join(Client client, string roomgroup) {
        AlienRiderMatchmakingHandler.instance.Join(client, roomgroup);
    }
}
