using sodoffmmo.Data;

namespace sodoffmmo.Core;

public abstract class HeadToHeadRoom : Room {
    protected bool Joinable = true;
    
    public HeadToHeadRoom(string group) : base (null, group, true) {}
    
    protected abstract string[] WritePlayer(KeyValuePair<Client, Status> player, bool isJoin);
    
    protected abstract string[] AddDataJoin();
    
    protected abstract string[] AddDataLeave();
    
    protected abstract string[] AddDataPlayAgain();
    
    protected class Status {
        public string uid;
        public bool isReady = false;
        public bool gameLoaded = false;

        public Status(string uid) {
            this.uid = uid;
        }
    }

    protected Dictionary<Client, Status> players = new();

    public Client? Host { get; protected set; } = null;

    protected virtual Status CreateStatus(Client client) => new Status(client.PlayerData.Uid);

    public void AddPlayer(Client client) {
        players[client] = CreateStatus(client);
        if (Host == null) Host = client;
    }

    public void SetPlayerReady(Client client, bool status = true) {
        players[client].isReady = status;
    }

    public int GetReadyCount() {
        int count = 0;
        foreach(var player in players) {
            if (player.Value.isReady) ++count;
        }
        return count;
    }

    public virtual void OnGameStart() {
        Joinable = false;
        foreach (var player in players)
            player.Value.isReady = false;
    }

    public virtual bool OnClientGameLoaded(Client client) {
        players[client].gameLoaded = true;
        bool allClientsGameLoaded = true;
        foreach (var player in players) {
            if (!player.Value.gameLoaded) {
                allClientsGameLoaded = false;
                break;
            }
        }
        if (allClientsGameLoaded) {
            // reset value for all
            foreach (var player in players) {
                player.Value.gameLoaded = false;
            }
        }
        return allClientsGameLoaded;
    }

    public virtual void SendUJR() {
        // {"a":13,"c":1,"p":{"c":"msg","p":{"arr":["UJR","287997","2","f66cc516-7ea3-40a5-9021-01ff8f290123","false","2","03a3ad99-87a5-4af4-8966-0b2733a05e0f","false","1"]},"r":287997}}
        List<string> info = new();
        info.Add("UJR"); // User Joined Room
        info.AddRange(AddDataJoin());
        foreach(var player in players) {
            info.AddRange(WritePlayer(player, true));
        }
        NetworkPacket packet = Utils.ArrNetworkPacket(info.ToArray(), "msg", base.Id);

        Send(packet);
    }

    public override void RemoveClient(Client client) {
        base.RemoveClient(client);
        
        List<string> info = new();
        info.Add("ULR"); // User Left Room
        info.AddRange(AddDataLeave());
        info.Add(players[client].uid);
        
        NetworkPacket packet = Utils.ArrNetworkPacket(info.ToArray(), "msg", base.Id);
        Send(packet);
        
        players.Remove(client);
        
        if (client == Host) {
            if (players.Count == 0) Host = null;
            else Host = players.First().Key;
        }
    }

    public virtual void SendPA(Client client) {
        // {"a":13,"c":1,"p":{"c":"msg","p":{"arr":["UJR","287997","2","f66cc516-7ea3-40a5-9021-01ff8f290123","false","2","03a3ad99-87a5-4af4-8966-0b2733a05e0f","false","1"]},"r":287997}}
        List<string> info = new();
        info.Add("PA"); // Play Again
        info.AddRange(AddDataPlayAgain());
        foreach(var player in players) {
            info.AddRange(WritePlayer(player, false));
        }
        NetworkPacket packet = Utils.ArrNetworkPacket(info.ToArray(), "msg", base.Id);

        client.Send(packet);
        
        // Send all players unreadied.
        foreach(var player in players) {
            NetworkPacket unreadypacket = Utils.ArrNetworkPacket([
                "LUNR",
                base.Id.ToString(),
                client.PlayerData.Uid
            ], "msg", base.Id);
            client.Send(unreadypacket);
        }
    }

    protected abstract class MatchmakingHandler {
        object NextRoomLock = new object();
        Dictionary<string, SortedSet<HeadToHeadRoom>> RoomCollection = new();

        protected abstract int _MaxPlayers { get; }

        protected abstract HeadToHeadRoom CreateNewInstance(string roomgroup);

        public HeadToHeadRoom Get(string roomgroup) {
            lock(NextRoomLock) {
                if (!RoomCollection.TryGetValue(roomgroup, out var rooms)) {
                    rooms = new SortedSet<HeadToHeadRoom>(new RoomSorting());
                    RoomCollection[roomgroup] = rooms;
                }

                rooms.RemoveWhere(r => r.IsRemoved);
                foreach (var room in rooms) {
                    if (room.Joinable && room.ClientsCount < _MaxPlayers) {
                        return room;
                    }
                }
                var newRoom = CreateNewInstance(roomgroup);
                rooms.Add(newRoom);
                return newRoom;
            }
        }
        
        object joinLock = new object();
        public void Join(Client client, string roomgroup) {
            lock(joinLock) {
                HeadToHeadRoom room = Get(roomgroup);
                client.SetRoom(room);
                room.AddPlayer(client); // client will be not removed from HeadToHeadRoom.players ... after remove all client from room whole HeadToHeadRoom.players will be removed
                room.SendUJR();
            }
        }
        
        private class RoomSorting : IComparer<Room> {
            public int Compare(Room? x, Room? y) {
                return (y?.Id ?? -1) - (x?.Id ?? -1);
            }
        }
    }
}