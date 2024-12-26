using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Server;


// Pc 클래스
public class Pc : Character
{
    public Dictionary<PartsType, bool> PartsDic;
    public int Experience { get; set; }
    public int ExperienceToNextLevel { get; set; }

    public bool AutoPlayEnabled { get; set; }
    public bool isWalk { get; set; }
    public bool isWeapon { get; set; }

    public bool isGaming { get; set; }
    public Job MyJob;

    public Pc() : base()
    {
        Init();
    }
    public void Init()
    {
        Level = 1;
        Name = "Player_" + Index;
        Experience = 0;
        ExperienceToNextLevel = 100;
        Attack = 10;
        Defense = 5;
        MaxHp = 300;
        Hp = MaxHp;
        MaxMp = 100;
        Mp = MaxMp;
        AttackSpeed = 1.0f;
        MoveSpeed = 2;
        CastingSpeed = 1.0f;
        AutoPlayEnabled = false;
        PartsDic = new Dictionary<PartsType, bool>();
        ResetDic();
    }

    private void InitDic()
    {
        PartsDic.Add(PartsType.Slider, false);
        PartsDic.Add(PartsType.Magazine, false);
        PartsDic.Add(PartsType.Trigger, false);
        PartsDic.Add(PartsType.Body, false);
    }
    public void ResetDic()
    {
        PartsDic.Clear();
        InitDic();
    }
    public bool IsAllParts()
    {
        bool val = true;
        PartsDic.ToList().ForEach(item => val &= item.Value);
        return val;
    }

    public override void Update()
    {
        base.Update();
        if (!AutoPlayEnabled && (Pos.X != Dest.X || Pos.Y != Dest.Y || Pos.Z != Dest.Z))
        {
            MoveTowardsDestination(Dest);
        }
    }
}


