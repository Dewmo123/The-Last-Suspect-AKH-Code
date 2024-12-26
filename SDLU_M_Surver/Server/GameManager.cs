//#define TimeLogger

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Security.Principal;
using System.Linq;
using NetBase;
using System.Runtime.InteropServices;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using Server.C2SInGame;
using Server;
using System.Security.AccessControl;
public static class Constants
{
    public const int Name_Length = 64;
    public const int DebugMsg_Length = 1024;
}

public class GameManager : ISingleton<GameManager>
{
    private static readonly TimeSpan NPC_REMOVE_DELAY = TimeSpan.FromSeconds(10); // 5초 후에 NPC 제거
    private const int TIMEOUT_MILLISECONDS = 5000; // 5초, 필요에 따라 조정
    private const int MapSize = 400;
    private static GameManager _instance;
    private System.Diagnostics.Stopwatch aStarStopwatch = new System.Diagnostics.Stopwatch();

    private static SpinLock _lock = new SpinLock();
    private Dictionary<int, CObject> activeObjects = new Dictionary<int, CObject>();
    private Dictionary<int, CObject> activeClientIdPc = new Dictionary<int, CObject>();
    private int nextIndex = 1;
    private ObjectPool<Pc> pcPool;
    private ObjectPool<Parts> partsPool;
    public FileLogger log = new FileLogger("C:\\Users\\K\\ggms unity\\2024_SDLU_3\\SDLU_M_Surver\\Server\\bin\\Debug\\PCLog.txt");

    public int ClientCount => activeClientIdPc.Count;
    public bool isGaming = false;

    private NotifyValue<int> economyCnt = new NotifyValue<int>();
    private NotifyValue<int> murderCnt = new NotifyValue<int>();

    public Dictionary<Job, NotifyValue<int>> JobCountDic = new Dictionary<Job, NotifyValue<int>>();

    public MapType map;
    private Random _rand = new Random();

    private List<PartsInfoBr> _infos = new List<PartsInfoBr>();
    #region Singleton and class
    public static GameManager GetGameManager()
    {
        return new GameManager();
    }

    public static new GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new GameManager();
                    }
                }
            }
            return _instance;
        }
    }

    // Parameterless constructor
    public GameManager()
    {
        pcPool = new ObjectPool<Pc>(1000, 3000, 50);
        partsPool = new ObjectPool<Parts>(1000, 3000, 50);
        economyCnt.OnvalueChanged += HandleEconomyChanged;
        murderCnt.OnvalueChanged += HandleMurderChanged;
        JobCountDic.Add(Job.Economy, economyCnt);
        JobCountDic.Add(Job.Murder, murderCnt);
        JobCountDic.Add(Job.Police, economyCnt);
    }
    #endregion
    #region Notify
    private void HandleMurderChanged(int prev, int next)
    {
        log.LogInfo("MurderCnt: " + next);
        if (next <= 0 && isGaming)
        {
            //시민 이김 처리
            GameEnd(Job.Economy);
        }
    }


    private void HandleEconomyChanged(int prev, int next)
    {
        log.LogInfo("EconomyCnt: " + next);
        if (next == 0 && isGaming)
        {
            //머더 이김 처리
            GameEnd(Job.Murder);
        }
    }
    #endregion
    public void GameEnd(Job job)
    {
        isGaming = false;
        log.LogInfo("GameEnd");
        var packet = new PacketBase();
        map = MapType.None;
        GameEndAck end = new GameEndAck()
        {
            WinJob = job,
        };
        foreach (var item in GetAllPc())
            item.isGaming = false;
        RemoveParts();
        ClearPCDic();
        _infos.Clear();
        packet.Write(end);
        Program.ResetEndTimer();
        Program.ResetPartsTimer();
        log.LogInfo("ResetEndTimer");
        Program.InitializeStartTimer();
        economyCnt.Value = 0;
        murderCnt.Value = 0;
        SocketManager.BroadcastToAll(PacketType.GameEndAck, packet.GetPacketData());
    }

    private void ClearPCDic()
    {
        var pcs = GetAllPc();
        pcs.ForEach(item => item.ResetDic());
    }


    // Parameterized constructor

    public void SetParts()
    {
        var parts = SpawnManager.Instance.SetParts(map, 20);
        List<PartsInfoBr> infos = new List<PartsInfoBr>();
        log.LogInfo("SetParts");
        if (_infos.Count > 0)
            RemoveParts();
        foreach (var i in parts)
        {
            var item = CreateObject<Parts>("Parts");
            item.Pos = i;
            item.type = (PartsType)_rand.Next(1, 5);
            infos.Add(new PartsInfoBr()
            {
                Index = item.Index,
                type = item.type,
                Pos = item.Pos
            });
        }
        _infos = infos;
        ZoneUpdateAck zone = new ZoneUpdateAck()
        {
            Attacks = new List<AttackBr>(),
            Removes = new List<RemoveBr>(),
            Moves = new List<MoveBr>(),
            isGaming = isGaming,
            Parts = infos,
            PcEnters = new List<PcInfoBr>()
        };
        var packet = new PacketBase();
        packet.Write(zone);
        SocketManager.BroadcastToAll(PacketType.ZoneUpdateAck, packet.GetPacketData());
    }

    private void RemoveParts()
    {
        log.LogInfo("RemoveParts");
        ZoneUpdateAck zone = new ZoneUpdateAck()
        {
            Attacks = new List<AttackBr>(),
            Moves = new List<MoveBr>(),
            Parts = new List<PartsInfoBr>(),
            PcEnters = new List<PcInfoBr>(),
            isGaming = isGaming,
            Removes = new List<RemoveBr>()
        };
        foreach (var item in _infos)
        {
            RemoveBr remove = new RemoveBr();
            RemoveObject(item.Index);
            remove.Index = item.Index;
            remove.ObjectType = EObjectType.Parts;
            zone.Removes.Add(remove);
        }
        var packet = new PacketBase();
        packet.Write(zone);
        SocketManager.BroadcastToAll(PacketType.ZoneUpdateAck, packet.GetPacketData());

    }
    public void SpawnCharacter(Character character, FLocation position)
    {
        character.Pos = position;
        character.OnSpawn();
        activeObjects[character.Index] = character;
        Console.WriteLine($"Spawned character {character.Name} at position ({position.X}, {position.Y}, {position.Z})");
    }
    public void StartGame()
    {
        GameStartAck gameStartAck = new GameStartAck
        {
            JobsAndPos = new List<PcInfoBr>()
        };
        List<Pc> pcs = GetAllPc();
        List<Job> jobs = GetPlayerJobs(pcs.Count);
        isGaming = true;

        //map = SpawnManager.Instance.GetRandomMap();
        map = MapType.Japan;
        SetParts();
        Server.FLocation[] pts = SpawnManager.Instance.GetRandomPCSpawnTable(map);
        for (int i = 0; i < pcs.Count; i++)
        {
            Pc pc = pcs[i];
            gameStartAck.JobsAndPos.Add(new PcInfoBr
            {
                Name = pc.Name.ToCharArray(),
                Index = pc.Index,
                Pos = pts[i],
                Dest = new Server.FLocation { X = pc.Dest.X, Y = pc.Dest.Y, Z = pc.Dest.Z },
                Direction = pc.Direction,
                MoveSpeed = pc.MoveSpeed,
                MyJob = jobs[i],
                isGaming = true
            });
            pc.isGaming = true;
            pc.MyJob = jobs[i];
        }
        var packet = new PacketBase();
        packet.Write(gameStartAck);
        Console.WriteLine("Recv StartTrigger");
        log.LogInfo("GameStart");
        Program.InitializeEndTimer();
        Program.InitializePartsTimer();
        SocketManager.BroadcastToAll(PacketType.GameStartAck, packet.GetPacketData());
    }


    public void UpdateAll(bool logFlag)
    {
        int pcCount = 0;
        int npcCount = 0;
        int itemCount = 0;
        //lock (activeObjects)
        {
            var objectsToUpdate = new List<CObject>();
            foreach (var obj in activeObjects.Values)
            {
                if (obj != null)
                    objectsToUpdate.Add(obj);
            }

            foreach (var obj in objectsToUpdate)
            {
                if (obj != null)
                {
                    obj.Update();
                    if (logFlag)
                    {
                        if (obj is Pc) pcCount++;
                    }
                }
            }
            //if (logFlag)
                //Console.WriteLine($"PC count: {pcCount} NPC count: {npcCount} Item count: {itemCount}");
        }

    }
    #region Object
    public List<Job> GetPlayerJobs(int count)
    {
        List<Job> playerJobs = new List<Job>();
        for (int i = 0; i < count; i++)
            playerJobs.Add(Job.None);

        Console.WriteLine("PlayerJobs:" + playerJobs.Count);

        Random rand = new Random();
        int killer = rand.Next(0, count);
        Console.WriteLine("Killer" + killer);
        playerJobs[killer] = Job.Murder;

        murderCnt.Value = 1;
        economyCnt.Value = 0;
        int police = killer;
        while (police == killer)
        {
            police = rand.Next(0, count);
            Console.WriteLine("Police" + police);
        }
        playerJobs[police] = Job.Police;
        economyCnt.Value++;

        for (int i = 0; i < ClientCount; i++)
        {
            if (playerJobs[i] == Job.None)
            {
                economyCnt.Value++;
                playerJobs[i] = Job.Economy;
            }
        }
        return playerJobs;
    }
    public void PrintPoolStatus()
    {
        Console.WriteLine($"PC Pool size: {pcPool.Count}");
    }
    public void DespawnCharacter(Character character)
    {
        character.OnDespawn();
        Console.WriteLine($"Despawned character {character.Name}");
    }
    public T CreateObject<T>(string name) where T : CObject, new()
    {
        T obj;
        if (typeof(T) == typeof(Pc))
        {
            log.LogInfo("AddClient" + nextIndex + 1);
            obj = (T)(object)pcPool.Get();
        }
        else if (typeof(T) == typeof(Parts))
            obj = (T)(object)partsPool.Get();
        else
            obj = new T();

        if (obj == null)
            obj = new T();

        obj.Name = name;
        obj.Index = nextIndex++;

        activeObjects[obj.Index] = obj;

        return obj;
    }

    public void RemoveObject(int index)
    {
        if (activeObjects.TryGetValue(index, out CObject obj))
        {
            activeObjects.Remove(index);
            if (obj is Parts parts)
            {
                partsPool.Return(parts);
            }
            if (obj is Pc pc)
            {
                log.LogInfo("RemoveClient" + index + isGaming + pc.isGaming);
                if (isGaming && pc.isGaming)
                {
                    JobCountDic[pc.MyJob].Value--;
                }
                pcPool.Return(pc);
            }
        }
    }

    public CObject GetObject(int index)
    {
        return activeObjects.TryGetValue(index, out CObject obj) ? obj : null;
    }
    // Method to associate a Socket with a Pc object
    public void AssociateClientIdWithPc(int ClientId, Pc pc)
    {
        activeClientIdPc[ClientId] = pc;
    }
    public List<Pc> GetAllPc()
    {
        List<Pc> allPcs = new List<Pc>();

        foreach (var kvp in activeClientIdPc)
        {
            if (kvp.Value is Pc pc)
            {
                allPcs.Add(pc);
            }
        }

        return allPcs;
    }

    // Method to get Pc object by Socket
    public Pc GetPcByClientId(int ClientId)
    {
        return activeClientIdPc.TryGetValue(ClientId, out CObject obj) ? obj as Pc : null;
    }

    public void RemovePcByClientId(int clientId)
    {
        if (activeClientIdPc.TryGetValue(clientId, out CObject obj))
        {
            if (obj is Pc pc)
            {
                if (ClientCount < Define.MinPCCount)
                {
                    log.LogInfo("ResetStartTimer");
                    Program.ResetStartTimer();
                }
                // Pc 객체 제거
                RemoveObject(pc.Index);
                BroadcastRemove(pc, EObjectType.Pc); // <-이거 추가
                activeClientIdPc.Remove(clientId);
                Console.WriteLine($"Removed Pc with index {pc.Index} for client ID {clientId}");
            }
        }
    }

    public Character GetNearestPc(FLocation position, float range)
    {
        Pc nearestPc = null;
        float nearestDistance = float.MaxValue;

        foreach (var kvp in activeClientIdPc)
        {
            if (kvp.Value is Pc pc)
            {
                float distance = CalculateDistance(position, pc.Pos);
                if (distance <= range && distance < nearestDistance)
                {
                    nearestPc = pc;
                    nearestDistance = distance;
                }
            }
        }

        return nearestPc;
    }
    public Character GetNearestEnemy(Character source, float range)
    {
        // Pc와 Npc 목록을 모두 가져옵니다.
        List<Character> allCharacters = new List<Character>();
        allCharacters.AddRange(GetAllPc());

        return allCharacters
            .Where(c => c != source && c.IsAlive && CalculateDistance(source.Pos, c.Pos) <= range)
            .OrderBy(c => CalculateDistance(source.Pos, c.Pos))
        .FirstOrDefault();
    }

    private float CalculateDistance(FLocation pos1, FLocation pos2)
    {
        float dx = pos1.X - pos2.X;
        float dy = pos1.Y - pos2.Y;
        float dz = pos1.Z - pos2.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
    #endregion
    #region BroadcastPacket
    public void BroadcastMove(Character character)
    {
        var broadcastPacket = new PacketBase();
        ZoneUpdateAck zoneUpdateAck = new ZoneUpdateAck
        {
            PcEnters = new List<PcInfoBr>(),
            Moves = new List<MoveBr>
            {
                new MoveBr
                {
                    Index = character.Index,
                    ObjectType = EObjectType.Pc,
                    Pos = new Server.FLocation { X = character.Pos.X, Y = character.Pos.Y, Z = character.Pos.Z },
                    Dest = new Server.FLocation { X = character.Dest.X, Y = character.Dest.Y, Z = character.Dest.Z }
                }
            },
            Removes = new List<RemoveBr>(),
            Attacks = new List<AttackBr>(),
            Parts = new List<PartsInfoBr>(),
            isGaming = isGaming
        };

        broadcastPacket.Write(zoneUpdateAck);
        SocketManager.BroadcastToAll(PacketType.ZoneUpdateAck, broadcastPacket.GetPacketData());
    }

    public void BroadcastRemove(CObject character, EObjectType type)
    {
        var broadcastPacket = new PacketBase();
        ZoneUpdateAck zoneUpdateAck = new ZoneUpdateAck
        {
            PcEnters = new List<PcInfoBr>(),
            Moves = new List<MoveBr>(),
            Removes = new List<RemoveBr>
            {
                new RemoveBr
                {
                    Index = character.Index,
                    ObjectType = type
                }
            },
            Attacks = new List<AttackBr>(),
            Parts = new List<PartsInfoBr>(),
            isGaming = isGaming
        };

        broadcastPacket.Write(zoneUpdateAck);
        SocketManager.BroadcastToAll(PacketType.ZoneUpdateAck, broadcastPacket.GetPacketData());
    }
    public void BroadcastAttack(Character attacker, Character target, int skillId, Server.FLocation direction,
                        Server.FLocation pos, Server.FLocation dest, Server.FLocation postHitPosition, float postHitDirection, EAttackResult result, float timeOffset)
    {
        var broadcastPacket = new PacketBase();
        ZoneUpdateAck zoneUpdateAck = new ZoneUpdateAck
        {
            PcEnters = new List<PcInfoBr>(),
            Moves = new List<MoveBr>(),
            Removes = new List<RemoveBr>(),
            Attacks = new List<AttackBr>
            {
                new AttackBr
                {
                    SrcObjectIndex = attacker.Index,
                    TarObjectIndex = target==null?-1:target.Index,
                    SkillId = skillId,
                    Direction = direction,
                    Pos = pos,
                    Dest = dest,
                    PostHitPosition = postHitPosition,
                    PostHitDirection = postHitDirection,
                    TimeOffset = timeOffset,
                    Result = result,
                }
            },
            Parts = new List<PartsInfoBr>(),
            isGaming = isGaming
        };
        broadcastPacket.Write(zoneUpdateAck);
        SocketManager.BroadcastToAll(PacketType.ZoneUpdateAck, broadcastPacket.GetPacketData());
    }
    #endregion
    private char[] ConvertToCharArray(string message)
    {
        char[] result = new char[Constants.DebugMsg_Length];
        if (message.Length > Constants.DebugMsg_Length)
        {
            message = message.Substring(0, Constants.DebugMsg_Length);
        }
        message.CopyTo(0, result, 0, message.Length);
        return result;
    }

    // 모든 활성 객체의 ID를 반환하는 메서드
    public IEnumerable<int> GetAllActiveObjectIds()
    {
        return activeObjects.Keys;
    }

    // 현재 활성화된 객체 수를 반환하는 메서드 (모니터링용)
    public int GetActiveObjectCount()
    {
        return activeObjects.Count;
    }

    // 옵션: 객체 타입별 카운트를 반환하는 메서드
    public Dictionary<string, int> GetActiveObjectCountByType()
    {
        return activeObjects
            .Values
            .GroupBy(obj => obj.GetType().Name)
            .ToDictionary(
                group => group.Key,
                group => group.Count()
            );
    }
}
