using System;
using System.Collections.Generic;
using NetBase;

namespace Server
{
    public interface IPacketSerializable
    {
        void Serialize(PacketBase packet);
        void Deserialize(PacketBase packet);
    }
public static class Constants
{
	public const int Name_Length = 64;
}
public enum EObjectType
{
    None,
    Pc,
    Parts
}

public enum MapType
{
    None,
    Japan,
    Dungeon,
    Castle,
    Island
}

public enum PartsType
{
    None,
    Slider,
    Body,
    Magazine,
    Trigger
}

public enum Job
{
    None = 0,
    Murder,
    Economy,
    Police,
    Specture
}

public enum EAttackResult
{
    None = 0,
    Miss,
    // 공격이 빗나감
    Kill,
    Double
}

public enum EMoveResult
{
    None = 0,
    // 성공
    Success,
    // 이동 성공

    // 실패 원인들
    Failed_Obstacle,
    // 장애물로 인한 실패
    Failed_OutOfBounds,
    // 경계를 벗어남
    Failed_NoPath,
    // 경로가 없음
    Failed_Blocked,
    // 다른 유닛에 의해 막힘
    Failed_Terrain,
    // 지형으로 인한 실패 (예: 물/용암)
    Failed_Exhausted,
    // 스태미나/행동력 부족
    Failed_Stunned,
    // 기절 상태
    Failed_Rooted,
    // 속박 상태
    Failed_Frozen,
    // 빙결 상태
    Failed_Feared,
    // 공포 상태
    Failed_Charmed,
    // 매혹 상태
    Failed_Overencumbered,
    // 과도한 무게로 인한 실패
    Failed_NoFlyingAbility,
    // 비행 능력 부족 (공중 이동 시)
    Failed_NoSwimmingAbility,
    // 수영 능력 부족 (수중 이동 시)
    Failed_Cooldown,
    // 이동 능력 쿨다운 중
    Failed_InsufficientResource,
    // 자원 부족 (마나 등)
    Failed_TargetUnreachable,
    // 목표 지점에 도달 불가능
    Failed_InvalidMove,
    // 유효하지 않은 이동

    // 특수 상황
    Partial_Success,
    // 부분적 성공 (일부 거리만 이동)
    Teleported,
    // 텔레포트로 이동 (일반 이동과 다른 경우)
    Pushed,
    // 밀려남 (강제 이동)
    Pulled,
    // 끌려감 (강제 이동)

    Max
}

public enum PacketType
{
    None,
    HeartbeatReq,
    ResourceLoadCompleteReq,
    ResourceLoadCompleteAck,
    CreatePartsAck,
    ZoneUpdateAck,
    MoveReq,
    MoveAck,
    GameEndAck,
    GameStartAck,
    AttackReq,
    AttackAck,
    CollectPartsReq,
    CollectPartsAck,
    AddGunReq,
    AddGunAck,
    Max,
}


public struct PartsInfoBr : IPacketSerializable
{
    public int Index;
    public FLocation Pos;
    public PartsType type;
    public PartsInfoBr(
        int index,
        FLocation pos,
        PartsType type)
    {
        this.Index = index;
        this.Pos = pos;
        this.type = type;
    }
    public static PartsInfoBr Default => new PartsInfoBr
    {
        Index = 0,
        Pos = default,
        type = default
    };
    public void Serialize(NetBase.PacketBase PacketBase)
    {
        PacketBase.Write(Index);
        PacketBase.Write(Pos);
        PacketBase.Write(type);
    }
    public void Deserialize(NetBase.PacketBase PacketBase)
    {
        Index = PacketBase.Read<int>();
        Pos = PacketBase.Read<FLocation>();
        type = PacketBase.Read<PartsType>();
    }
}

public struct FLocation : IPacketSerializable
{
    public float X;
    public float Y;
    public float Z;
    public FLocation(
        float x,
        float y,
        float z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }
    public static FLocation Default => new FLocation
    {
        X = 0f,
        Y = 0f,
        Z = 0f
    };
    public void Serialize(NetBase.PacketBase PacketBase)
    {
        PacketBase.Write(X);
        PacketBase.Write(Y);
        PacketBase.Write(Z);
    }
    public void Deserialize(NetBase.PacketBase PacketBase)
    {
        X = PacketBase.Read<float>();
        Y = PacketBase.Read<float>();
        Z = PacketBase.Read<float>();
    }
}

public struct PcInfoBr : IPacketSerializable
{
    public int Index;
    public char[] Name;
    public FLocation Pos;
    public FLocation Dest;
    public FLocation Direction;
    public float MoveSpeed;
    public bool isGaming;
    public Job MyJob;
    public PcInfoBr(
        int index,
        char[] name,
        FLocation pos,
        FLocation dest,
        FLocation direction,
        float movespeed,
        bool isgaming,
        Job myjob)
    {
        this.Index = index;
        this.Name = new char[Constants.Name_Length];
        Array.Copy(name, this.Name, Constants.Name_Length);
        this.Pos = pos;
        this.Dest = dest;
        this.Direction = direction;
        this.MoveSpeed = movespeed;
        this.isGaming = isgaming;
        this.MyJob = myjob;
    }
    public static PcInfoBr Default => new PcInfoBr
    {
        Index = 0,
        Name = new char[Constants.Name_Length],
        Pos = default,
        Dest = default,
        Direction = default,
        MoveSpeed = 0f,
        isGaming = false,
        MyJob = default
    };
    public void Serialize(NetBase.PacketBase PacketBase)
    {
        PacketBase.Write(Index);
        foreach (var item in Name)
        {
            PacketBase.Write(item);
        }
        PacketBase.Write(Pos);
        PacketBase.Write(Dest);
        PacketBase.Write(Direction);
        PacketBase.Write(MoveSpeed);
        PacketBase.Write(isGaming);
        PacketBase.Write(MyJob);
    }
    public void Deserialize(NetBase.PacketBase PacketBase)
    {
        Index = PacketBase.Read<int>();
        for (int i = 0; i < Constants.Name_Length; i++)
        {
            Name[i] = PacketBase.Read<char>();
        }
        Pos = PacketBase.Read<FLocation>();
        Dest = PacketBase.Read<FLocation>();
        Direction = PacketBase.Read<FLocation>();
        MoveSpeed = PacketBase.Read<float>();
        isGaming = PacketBase.Read<bool>();
        MyJob = PacketBase.Read<Job>();
    }
}

public struct RemoveBr : IPacketSerializable
{
    public int Index;
    public EObjectType ObjectType;
    public RemoveBr(
        int index,
        EObjectType objecttype)
    {
        this.Index = index;
        this.ObjectType = objecttype;
    }
    public static RemoveBr Default => new RemoveBr
    {
        Index = 0,
        ObjectType = default
    };
    public void Serialize(NetBase.PacketBase PacketBase)
    {
        PacketBase.Write(Index);
        PacketBase.Write(ObjectType);
    }
    public void Deserialize(NetBase.PacketBase PacketBase)
    {
        Index = PacketBase.Read<int>();
        ObjectType = PacketBase.Read<EObjectType>();
    }
}

public struct MoveBr : IPacketSerializable
{
    public int Index;
    public float speed;
    public bool isWalk;
    public bool IsWeapon;
    public EObjectType ObjectType;
    public FLocation Pos;
    public FLocation Dest;
    public FLocation Dir;
    public MoveBr(
        int index,
        float speed,
        bool iswalk,
        bool isweapon,
        EObjectType objecttype,
        FLocation pos,
        FLocation dest,
        FLocation dir)
    {
        this.Index = index;
        this.speed = speed;
        this.isWalk = iswalk;
        this.IsWeapon = isweapon;
        this.ObjectType = objecttype;
        this.Pos = pos;
        this.Dest = dest;
        this.Dir = dir;
    }
    public static MoveBr Default => new MoveBr
    {
        Index = 0,
        speed = 0f,
        isWalk = false,
        IsWeapon = false,
        ObjectType = default,
        Pos = default,
        Dest = default,
        Dir = default
    };
    public void Serialize(NetBase.PacketBase PacketBase)
    {
        PacketBase.Write(Index);
        PacketBase.Write(speed);
        PacketBase.Write(isWalk);
        PacketBase.Write(IsWeapon);
        PacketBase.Write(ObjectType);
        PacketBase.Write(Pos);
        PacketBase.Write(Dest);
        PacketBase.Write(Dir);
    }
    public void Deserialize(NetBase.PacketBase PacketBase)
    {
        Index = PacketBase.Read<int>();
        speed = PacketBase.Read<float>();
        isWalk = PacketBase.Read<bool>();
        IsWeapon = PacketBase.Read<bool>();
        ObjectType = PacketBase.Read<EObjectType>();
        Pos = PacketBase.Read<FLocation>();
        Dest = PacketBase.Read<FLocation>();
        Dir = PacketBase.Read<FLocation>();
    }
}

public struct AttackInfo : IPacketSerializable
{
    public int TargetIndex;
    public int SkillId;
    public FLocation Direction;
    public Job Atker;
    public FLocation Pos;
    public FLocation Dest;
    public float TimeOffset;
    public FLocation PostHitPosition;
    public float PostHitDirection;
    public AttackInfo(
        int targetindex,
        int skillid,
        FLocation direction,
        Job atker,
        FLocation pos,
        FLocation dest,
        float timeoffset,
        FLocation posthitposition,
        float posthitdirection)
    {
        this.TargetIndex = targetindex;
        this.SkillId = skillid;
        this.Direction = direction;
        this.Atker = atker;
        this.Pos = pos;
        this.Dest = dest;
        this.TimeOffset = timeoffset;
        this.PostHitPosition = posthitposition;
        this.PostHitDirection = posthitdirection;
    }
    public static AttackInfo Default => new AttackInfo
    {
        TargetIndex = 0,
        SkillId = 0,
        Direction = default,
        Atker = default,
        Pos = default,
        Dest = default,
        TimeOffset = 0f,
        PostHitPosition = default,
        PostHitDirection = 0f
    };
    public void Serialize(NetBase.PacketBase PacketBase)
    {
        PacketBase.Write(TargetIndex);
        PacketBase.Write(SkillId);
        PacketBase.Write(Direction);
        PacketBase.Write(Atker);
        PacketBase.Write(Pos);
        PacketBase.Write(Dest);
        PacketBase.Write(TimeOffset);
        PacketBase.Write(PostHitPosition);
        PacketBase.Write(PostHitDirection);
    }
    public void Deserialize(NetBase.PacketBase PacketBase)
    {
        TargetIndex = PacketBase.Read<int>();
        SkillId = PacketBase.Read<int>();
        Direction = PacketBase.Read<FLocation>();
        Atker = PacketBase.Read<Job>();
        Pos = PacketBase.Read<FLocation>();
        Dest = PacketBase.Read<FLocation>();
        TimeOffset = PacketBase.Read<float>();
        PostHitPosition = PacketBase.Read<FLocation>();
        PostHitDirection = PacketBase.Read<float>();
    }
}

public struct AttackResult : IPacketSerializable
{
    public EAttackResult Result;
    public int TarObjectIndex;
    public AttackResult(
        EAttackResult result,
        int tarobjectindex)
    {
        this.Result = result;
        this.TarObjectIndex = tarobjectindex;
    }
    public static AttackResult Default => new AttackResult
    {
        Result = default,
        TarObjectIndex = 0
    };
    public void Serialize(NetBase.PacketBase PacketBase)
    {
        PacketBase.Write(Result);
        PacketBase.Write(TarObjectIndex);
    }
    public void Deserialize(NetBase.PacketBase PacketBase)
    {
        Result = PacketBase.Read<EAttackResult>();
        TarObjectIndex = PacketBase.Read<int>();
    }
}

public struct AttackBr : IPacketSerializable
{
    public int SrcObjectIndex;
    public int TarObjectIndex;
    public int SkillId;
    public FLocation Direction;
    public FLocation Pos;
    public FLocation Dest;
    public FLocation PostHitPosition;
    public float PostHitDirection;
    public float TimeOffset;
    public EAttackResult Result;
    public AttackBr(
        int srcobjectindex,
        int tarobjectindex,
        int skillid,
        FLocation direction,
        FLocation pos,
        FLocation dest,
        FLocation posthitposition,
        float posthitdirection,
        float timeoffset,
        EAttackResult result)
    {
        this.SrcObjectIndex = srcobjectindex;
        this.TarObjectIndex = tarobjectindex;
        this.SkillId = skillid;
        this.Direction = direction;
        this.Pos = pos;
        this.Dest = dest;
        this.PostHitPosition = posthitposition;
        this.PostHitDirection = posthitdirection;
        this.TimeOffset = timeoffset;
        this.Result = result;
    }
    public static AttackBr Default => new AttackBr
    {
        SrcObjectIndex = 0,
        TarObjectIndex = 0,
        SkillId = 0,
        Direction = default,
        Pos = default,
        Dest = default,
        PostHitPosition = default,
        PostHitDirection = 0f,
        TimeOffset = 0f,
        Result = default
    };
    public void Serialize(NetBase.PacketBase PacketBase)
    {
        PacketBase.Write(SrcObjectIndex);
        PacketBase.Write(TarObjectIndex);
        PacketBase.Write(SkillId);
        PacketBase.Write(Direction);
        PacketBase.Write(Pos);
        PacketBase.Write(Dest);
        PacketBase.Write(PostHitPosition);
        PacketBase.Write(PostHitDirection);
        PacketBase.Write(TimeOffset);
        PacketBase.Write(Result);
    }
    public void Deserialize(NetBase.PacketBase PacketBase)
    {
        SrcObjectIndex = PacketBase.Read<int>();
        TarObjectIndex = PacketBase.Read<int>();
        SkillId = PacketBase.Read<int>();
        Direction = PacketBase.Read<FLocation>();
        Pos = PacketBase.Read<FLocation>();
        Dest = PacketBase.Read<FLocation>();
        PostHitPosition = PacketBase.Read<FLocation>();
        PostHitDirection = PacketBase.Read<float>();
        TimeOffset = PacketBase.Read<float>();
        Result = PacketBase.Read<EAttackResult>();
    }
}

namespace C2SInGame
{
    public struct HeartbeatReq
    {
    }
    public struct ResourceLoadCompleteReq
    {
        public string Name;
        public void Serialize(NetBase.PacketBase PacketBase)
        {
            PacketBase.Write(Name);
        }
        public void Deserialize(NetBase.PacketBase PacketBase)
        {
            Name = PacketBase.Read<string>();
        }
    }
    public struct ResourceLoadCompleteAck
    {
        public int PcIndex;
        public bool isGaming;
        public void Serialize(NetBase.PacketBase PacketBase)
        {
            PacketBase.Write(PcIndex);
            PacketBase.Write(isGaming);
        }
        public void Deserialize(NetBase.PacketBase PacketBase)
        {
            PcIndex = PacketBase.Read<int>();
            isGaming = PacketBase.Read<bool>();
        }
    }
    public struct CreatePartsAck
    {
        public PartsInfoBr Info;
        public void Serialize(NetBase.PacketBase PacketBase)
        {
            PacketBase.Write(Info);
        }
        public void Deserialize(NetBase.PacketBase PacketBase)
        {
            Info = PacketBase.Read<PartsInfoBr>();
        }
    }
    public struct ZoneUpdateAck
    {
        public List<PcInfoBr> PcEnters;
        public List<PartsInfoBr> Parts;
        public List<MoveBr> Moves;
        public List<RemoveBr> Removes;
        public List<AttackBr> Attacks;
        public bool isGaming;
        public void Serialize(NetBase.PacketBase PacketBase)
        {
            PacketBase.Write(PcEnters);
            PacketBase.Write(Parts);
            PacketBase.Write(Moves);
            PacketBase.Write(Removes);
            PacketBase.Write(Attacks);
            PacketBase.Write(isGaming);
        }
        public void Deserialize(NetBase.PacketBase PacketBase)
        {
            PcEnters = PacketBase.Read<List<PcInfoBr>>();
            Parts = PacketBase.Read<List<PartsInfoBr>>();
            Moves = PacketBase.Read<List<MoveBr>>();
            Removes = PacketBase.Read<List<RemoveBr>>();
            Attacks = PacketBase.Read<List<AttackBr>>();
            isGaming = PacketBase.Read<bool>();
        }
    }
    public struct MoveReq
    {
        public FLocation Direction;
        public FLocation Dest;
        public float speed;
        public bool IsWalk;
        public bool IsWeapon;
        public void Serialize(NetBase.PacketBase PacketBase)
        {
            PacketBase.Write(Direction);
            PacketBase.Write(Dest);
            PacketBase.Write(speed);
            PacketBase.Write(IsWalk);
            PacketBase.Write(IsWeapon);
        }
        public void Deserialize(NetBase.PacketBase PacketBase)
        {
            Direction = PacketBase.Read<FLocation>();
            Dest = PacketBase.Read<FLocation>();
            speed = PacketBase.Read<float>();
            IsWalk = PacketBase.Read<bool>();
            IsWeapon = PacketBase.Read<bool>();
        }
    }
    public struct MoveAck
    {
        public EMoveResult Result;
        public void Serialize(NetBase.PacketBase PacketBase)
        {
            PacketBase.Write(Result);
        }
        public void Deserialize(NetBase.PacketBase PacketBase)
        {
            Result = PacketBase.Read<EMoveResult>();
        }
    }
    public struct GameEndAck
    {
        public Job WinJob;
        public void Serialize(NetBase.PacketBase PacketBase)
        {
            PacketBase.Write(WinJob);
        }
        public void Deserialize(NetBase.PacketBase PacketBase)
        {
            WinJob = PacketBase.Read<Job>();
        }
    }
    public struct GameStartAck
    {
        public List<PcInfoBr> JobsAndPos;
        public void Serialize(NetBase.PacketBase PacketBase)
        {
            PacketBase.Write(JobsAndPos);
        }
        public void Deserialize(NetBase.PacketBase PacketBase)
        {
            JobsAndPos = PacketBase.Read<List<PcInfoBr>>();
        }
    }
    public struct AttackReq
    {
        public List<AttackInfo> AttackInfos;
        public void Serialize(NetBase.PacketBase PacketBase)
        {
            PacketBase.Write(AttackInfos);
        }
        public void Deserialize(NetBase.PacketBase PacketBase)
        {
            AttackInfos = PacketBase.Read<List<AttackInfo>>();
        }
    }
    public struct AttackAck
    {
        public List<AttackResult> Results;
        public void Serialize(NetBase.PacketBase PacketBase)
        {
            PacketBase.Write(Results);
        }
        public void Deserialize(NetBase.PacketBase PacketBase)
        {
            Results = PacketBase.Read<List<AttackResult>>();
        }
    }
    public struct CollectPartsReq
    {
        public int Index;
        public void Serialize(NetBase.PacketBase PacketBase)
        {
            PacketBase.Write(Index);
        }
        public void Deserialize(NetBase.PacketBase PacketBase)
        {
            Index = PacketBase.Read<int>();
        }
    }
    public struct CollectPartsAck
    {
        public PartsType type;
        public void Serialize(NetBase.PacketBase PacketBase)
        {
            PacketBase.Write(type);
        }
        public void Deserialize(NetBase.PacketBase PacketBase)
        {
            type = PacketBase.Read<PartsType>();
        }
    }
    public struct AddGunReq
    {
    }
    public struct AddGunAck
    {
        public int Index;
        public void Serialize(NetBase.PacketBase PacketBase)
        {
            PacketBase.Write(Index);
        }
        public void Deserialize(NetBase.PacketBase PacketBase)
        {
            Index = PacketBase.Read<int>();
        }
    }
}

}
