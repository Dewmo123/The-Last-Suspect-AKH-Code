using System;
using System.Collections.Generic;
using System.IO;
using Server;

// Struct for location
// 기본 오브젝트 클래스
public abstract class CObject
{
    public string Name { get; set; }
    public int Index { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public FLocation Pos { get; set; }

    public Server.FLocation Direction { get; set; }
    public virtual void Update()
    {
        // 기본 업데이트 로직
    }
}

