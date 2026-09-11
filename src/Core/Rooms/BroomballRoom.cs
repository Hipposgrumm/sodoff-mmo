using sodoffmmo.Data;

namespace sodoffmmo.Core;

public class BroomballRoom : Room {
    private static object GameRooms_Lock = new();
    private static readonly SortedSet<BroomballRoom> GameRooms = new(new BroomballRoomSorter());

    private readonly Random Random = new();
    
    private Dictionary<Client, Player> Players = new();
    private bool GameOngoing = false;
    private System.Timers.Timer CountdownTimer = new System.Timers.Timer(1000) {
        AutoReset = true
    };
    private int CountdownCounter = 0;
    private bool CountdownFast = false;
    private int MaxPoints = 0;
    private int ObstaclesGroup = 0;
    
    private HashSet<Client> Waitlist = new();
    private object Waitlist_Lock = new object();

    public const string TEAMID_RED  = "RT",  TEAMID_BLUE = "BT";
    public readonly Team TEAM_RED, TEAM_BLUE;

    public BroomballRoom() : base(null, "BroomStickArena", true) {
        TEAM_BLUE = new Team(this, TEAMID_BLUE);
        TEAM_RED = new Team(this, TEAMID_RED);
        CountdownTimer.Elapsed += CountdownTimerTick;
    }
    
    public static void CreateRootRoom(string group) {
        // Registering the room is handled in the super's constructor.
        new Root(group);
    }
    
    public static BroomballRoom GetGameRoom() {
        lock (GameRooms_Lock) {
            foreach (var room in GameRooms) {
                if (room.ClientsCount < Configuration.ServerConfiguration.BroomballMaxPlayers) {
                    return room;
                }
            }

            var newroom = new BroomballRoom();
            GameRooms.Add(newroom);
            return newroom;
        }
    }

    public override void AddClient(Client client) {
        base.AddClient(client);
        if (!GameOngoing) return;
        
        SendGameBoardInfo(client);
        TEAM_RED .SendScore(client);
        TEAM_BLUE.SendScore(client);
    }

    public override void RemoveClient(Client client) {
        GameRemovePlayer(client);
        base.RemoveClient(client);
        lock (GameRooms_Lock) {
            if (ClientsCount == 0 || TryMergingRooms()) {
                GameRooms.Remove(this);
            }
        }
    }

    public void GameAddPlayer(Client client) {
        lock (Waitlist_Lock) {
            if (GameOngoing) {
                Waitlist.Add(client);
                SendWaitingText(client);
            } else AddPlayerToMatch(client);
        }
    }

    public void GameRemovePlayer(Client client) {
        lock (Waitlist_Lock) {
            var wasInWaitlistAndRemoved = Waitlist.Remove(client);
            if (!wasInWaitlistAndRemoved) {
                RemovePlayerFromMatch(client);
            }
        }
    }

    private void AddPlayerToMatch(Client client) {
        Player player = new Player(client);
        Players[client] = player;
        if (TEAM_BLUE.Players.Count < TEAM_RED.Players.Count) {
            player.SetTeam(TEAM_BLUE);
        } else {
            player.SetTeam(TEAM_RED);
        }
        SendTeams();

        if (Players.Count >= Configuration.ServerConfiguration.BroomballMinPlayers)
            StartOrUpdateTimer(client);
        else SendWaitingText(client);
    }
    
    private void RemovePlayerFromMatch(Client client) {
        if (!Players.TryGetValue(client, out var player)) return;
        player.SetTeam(null);
        Players.Remove(client);
        if (GameOngoing) {
            if (TEAM_RED.Players.Count == 0) {
                DeclareGameEnd(TEAM_BLUE);
            } else if (TEAM_BLUE.Players.Count == 0) {
                DeclareGameEnd(TEAM_RED);
            }
        } else {
            int difference = TEAM_BLUE.Players.Count - TEAM_RED.Players.Count;
            if (difference >= 2) {
                // Move last player on Blue over to Red
                TEAM_BLUE.Players[^1].SetTeam(TEAM_RED);
            } else if (difference <= -2) {
                // Move last player on Red over to Blue
                TEAM_RED.Players[^1].SetTeam(TEAM_BLUE);
            }
            
            if (Players.Count < Configuration.ServerConfiguration.BroomballMinPlayers) {
                StopTimer();
            }
            
            SendTeams();
        }
    }

    public void SendTeams(Client? recipient = null) {
        NetworkPacket packet = Utils.ArrNetworkPacket([
            "BG",
            base.Id.ToString(),
            "PT",
            string.Join(':', Players.Values.Select(plr =>
                (plr.Team?.ID ?? "") + ',' + plr.Uid
            ))
        ], "msg", base.Id);
        if (recipient != null) recipient.Send(packet);
        else base.Send(packet);
    }

    private int WaitingTextNumber = 0;
    public void SendWaitingText(Client? recipient = null) {
        // I added support for multiple waiting texts because I thought it was funny.
        string[] waitingTextList = GameOngoing
            ? Configuration.ServerConfiguration.BroomballWaitingMessages_Ongoing
            : Configuration.ServerConfiguration.BroomballWaitingMessages_Players;
        
        string waitingText = waitingTextList.Length > 0
            ? waitingTextList[WaitingTextNumber % waitingTextList.Length]
            : "Waiting...";
        WaitingTextNumber++;

        NetworkPacket packet = Utils.ArrNetworkPacket([
            "BG",
            base.Id.ToString(),
            "GT", "PW",
            waitingText
        ], "msg", base.Id);
        if (recipient != null) recipient.Send(packet);
        else base.Send(packet);
    }

    public void StartOrUpdateTimer(Client? recipient = null) {
        if (!CountdownTimer.Enabled) {
            CountdownTimer.Start();
            CountdownCounter = int.MaxValue; // Gets actually set by calls below.
        }
        
        if (!CountdownFast && Players.Count == int.Min(ClientsCount, Configuration.ServerConfiguration.BroomballMaxPlayers)) {
            // If all available players are playing, timer go faster.
            CountdownCounter = int.Min(5, CountdownCounter);
            CountdownFast = true;
        } else {
            CountdownCounter = int.Min(30, CountdownCounter);
        }
        
        NetworkPacket packet = Utils.ArrNetworkPacket([
            "BG",
            base.Id.ToString(),
            "GT", "GC",
            CountdownCounter.ToString()
        ], "msg", base.Id);
        base.Send(packet);
    }

    public void StopTimer() {
        if (!CountdownTimer.Enabled) return;
        CountdownTimer.Stop();
        
        SendWaitingText();
    }

    private void CountdownTimerTick(object? source, System.Timers.ElapsedEventArgs e) {
        if (--CountdownCounter <= 0) {
            GameOngoing = true;
            MaxPoints = ((Players.Count / 2) * 5) + 15;
            ObstaclesGroup = Random.Next(0, 5);
            SendGameBoardInfo();
            CountdownTimer.Stop();
        }
        
        // Game start logic is handled by the client when CountdownCounter is exactly "0"
        NetworkPacket packet = Utils.ArrNetworkPacket([
            "BG",
            base.Id.ToString(),
            "GT", "GC",
            CountdownCounter.ToString()
        ], "msg", base.Id);
        base.Send(packet);
    }

    private void SendGameBoardInfo(Client? recipient = null) {
        NetworkPacket packet = Utils.ArrNetworkPacket([
            "BG",
            base.Id.ToString(),
            "GT", "GC",
            "",
            MaxPoints.ToString(),
            ObstaclesGroup.ToString()
        ], "msg", base.Id);
        if (recipient != null) recipient.Send(packet);
        else base.Send(packet);
    }

    public void DeclareGameEnd(Team? winningTeam) {
        NetworkPacket packet = Utils.ArrNetworkPacket([
            "BG",
            base.Id.ToString(),
            "GT", "GO",
            winningTeam?.ID ?? ""
        ], "msg", base.Id);
        base.Send(packet);
        GameOngoing = false;
        Players.Clear();
        TEAM_RED.Reset();
        TEAM_BLUE.Reset();

        lock (Waitlist_Lock) {
            foreach (var client in Waitlist) {
                AddPlayerToMatch(client);
            }
        }
    }
    
    public class Player {
        public readonly Client Client;
        
        public string Uid => Client.PlayerData.Uid;
        public Team? Team;
        
        public Player(Client client) {
            Client = client;
        }

        public void SetTeam(Team? team) {
            if (Team != null) Team.Players.Remove(this);
            Team = team;
            if (team != null) team.Players.Add(this);
        }
    }

    public class Team {
        private readonly BroomballRoom Room;
        
        public readonly string ID;
        internal List<Player> Players = new();

        private int mPoints = 0;
        public int Points {
            get => mPoints;
            set {
                mPoints = value;
                SendScore();
                if (mPoints >= Room.MaxPoints) {
                    Room.DeclareGameEnd(this);
                }
            }
        } 

        public Team(BroomballRoom room, string ID) {
            this.Room = room;
            this.ID = ID;
        }

        public void Reset() {
            mPoints = 0;
            foreach (var player in Players) {
                player.Team = null;
            }
            Players.Clear();
        }

        public void SendScore(Client? recipient = null) {
            NetworkPacket packet = Utils.ArrNetworkPacket([
                "BG",
                Room.Id.ToString(),
                "US",
                this.ID,
                mPoints.ToString()
            ], "msg", Room.Id);
            if (recipient != null) recipient.Send(packet);
            else Room.Send(packet);
        }
    }

    private class Root : Room {
        public Root(string group) : base(group, group) {
            Name += "_Root";
        }

        public override void AddClient(Client client) {
            client.SetRoom(GetGameRoom());
        }
    }

    private bool TryMergingRooms() {
        // Don't move players into a different lobby if there is a match going.
        // Note: Due to how Broomball is set up, other players can spectate an active game.
        //       It is therefore safe to move players to a lobby with an active game.
        if (this.GameOngoing) return false;

        BroomballRoom? foundroom = null;
        lock (GameRooms_Lock) {
            foreach (var room in GameRooms) {
                if (room == this) continue;
                if ((this.ClientsCount + room.ClientsCount) <= Configuration.ServerConfiguration.BroomballMaxPlayers) {
                    foundroom = room;
                    break;
                }
            }
        }
        if (foundroom == null) return false;
        
        // Move all players from here into that room. Then this room will be discarded.
        foreach (var client in Clients) {              
            client.SetRoom(foundroom);
        }
        return true;
    }
    
    private class BroomballRoomSorter : IComparer<Room> {
        public int Compare(Room? x, Room? y) {
            // Put rooms with most clients first.
            return (x?.ClientsCount ?? 0) - (y?.ClientsCount ?? 0);
        }
    }
}