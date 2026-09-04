using System;
using sodoffmmo.Data;

namespace sodoffmmo.Core;

public class BattleGameRoom : HeadToHeadRoom {
    private class BattleGameMatchmakingHandler : MatchmakingHandler {
        internal static readonly BattleGameMatchmakingHandler instance = new();
        protected override int _MaxPlayers => 2;
        protected override HeadToHeadRoom CreateNewInstance(string roomgroup) {
            return new BattleGameRoom(roomgroup);
        }
    }

    protected class BattleGameStatus : Status {
        public string petname = "";
        public string rentalPetIndex = "-1";
        public string currentXP = "0";
        public string upgradedSuperAttack = bool.FalseString;
        public string hasShield = bool.FalseString;

        public BattleState state = new BattleState();
        
        public BattleGameStatus(string uid) : base(uid) {}
    }
    
    public class BattleState {
        public float Health;
        public int Score;
        public float SuperAttackMeter;

        public BattleState() {
            Reset();
        }
        
        public void Reset() {
            Health = 100f;
            Score = 0;
            SuperAttackMeter = 0f;
        }

        public float DamageHealth(float amount) {
            Health = float.Clamp(Health-amount, 0f, 100f);
            return Health;
        }

        public int AddScore(int amount) {
            Score += amount;
            return Score;
        }

        public float AddSuperAttack(float amount) {
            SuperAttackMeter = float.Clamp(SuperAttackMeter+amount, 0f, 1f);
            return SuperAttackMeter;
        }

        public float UseSuperAttack() {
            SuperAttackMeter = 0f;
            return 0f;
        }
    }

    public BattleGameRoom(string roomname) : base(roomname) {}
    
    protected override Status CreateStatus(Client client) => new BattleGameStatus(client.PlayerData.Uid);

    protected override string[] WritePlayer(KeyValuePair<Client, Status> player, bool isJoin) {
        var status = (player.Value as BattleGameStatus)!;
        return isJoin ? [
            player.Value.uid,
            player.Value.isReady.ToString(),
            player.Value.uid, // Username (identifier for room vars)
            status.petname,
            status.rentalPetIndex,
            status.currentXP,
            status.upgradedSuperAttack,
            status.hasShield
        ] : [
            player.Value.uid,
            player.Value.isReady.ToString()
        ];
    }

    protected override string[] AddDataJoin() => [];
    
    protected override string[] AddDataLeave() => [];
    
    protected override string[] AddDataPlayAgain() => [];

    public void AssignPetData(Client client, string petname, string defaultPetIndex, string currentXP, string upgradedSuperAttack, string hasShield) {
        var status = (players[client] as BattleGameStatus)!;
        status.petname = petname;
        status.rentalPetIndex = defaultPetIndex;
        status.currentXP = currentXP;
        status.upgradedSuperAttack = upgradedSuperAttack;
        status.hasShield = hasShield;
    }

    public BattleState? GetState(string uid) {
        foreach (var player in players) {
            if (player.Key.PlayerData.Uid == uid)
                return (player.Value as BattleGameStatus)!.state;
        }
        return null;
    }

    public BattleState GetState(Client client) {
        return (players[client] as BattleGameStatus)!.state;
    }

    public bool ProcessResult(Client client, string score, string collectCount) {
        lock (base.roomLock) {
            var clientStatus = (players[client] as BattleGameStatus)!;
            //clientStatus.resultScore = score;
            //clientStatus.resultCollectCount = collectCount;

            //if (players.Any(p => (p.Value as BattleGameStatus)!.resultScore == null))
            //    return false;
            
            List<string> info = new();
            info.Add("GC");
            info.Add(base.Id.ToString());
            foreach(var player in players) {
                var status = (player.Value as BattleGameStatus)!;
                //if (status.resultScore == null) continue;
                info.Add(status.uid);
                //.Add(status.resultScore!);
                //info.Add(status.resultCollectCount!);
            }
            NetworkPacket packet = Utils.ArrNetworkPacket(info.ToArray(), "msg", base.Id);

            Send(packet);
            return true;
        }
    }

    static public void Join(Client client, string roomgroup) {
        BattleGameMatchmakingHandler.instance.Join(client, roomgroup);
    }
}
