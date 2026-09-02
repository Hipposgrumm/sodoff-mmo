using sodoffmmo.Data;

namespace sodoffmmo.Core;

public abstract class HeadToHeadRoom : Room {
    protected bool Joinable = true;
    
    public HeadToHeadRoom(string group) : base (null, group, true) {}
    
    protected abstract string[] WritePlayer(KeyValuePair<Client, Status> player);
    
    protected abstract string[] AddDataJoin();
    
    protected abstract string[] AddDataLeave();
    
    protected abstract string[] AddDataPlayAgain();
    
    protected class Status {
        public string uid;
        public bool isReady = false;

        public Status(string uid) {
            this.uid = uid;
        }
    }

    protected Dictionary<Client, Status> players = new();

    public virtual void AddPlayer(Client client) {
        players[client] = new Status(client.PlayerData.Uid);
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
    }

    public virtual void SendUJR() {
        // {"a":13,"c":1,"p":{"c":"msg","p":{"arr":["UJR","287997","2","f66cc516-7ea3-40a5-9021-01ff8f290123","false","2","03a3ad99-87a5-4af4-8966-0b2733a05e0f","false","1"]},"r":287997}}
        List<string> info = new();
        info.Add("UJR"); // User Joined Room
        info.AddRange(AddDataJoin());
        foreach(var player in players) {
            info.AddRange(WritePlayer(player));
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
    }

    public virtual void SendPA(Client client) {
        // {"a":13,"c":1,"p":{"c":"msg","p":{"arr":["UJR","287997","2","f66cc516-7ea3-40a5-9021-01ff8f290123","false","2","03a3ad99-87a5-4af4-8966-0b2733a05e0f","false","1"]},"r":287997}}
        List<string> info = new();
        info.Add("PA"); // Play Again
        info.AddRange(AddDataPlayAgain());
        foreach(var player in players) {
            info.AddRange(WritePlayer(player));
        }
        NetworkPacket packet = Utils.ArrNetworkPacket(info.ToArray(), "msg", base.Id);

        client.Send(packet);
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