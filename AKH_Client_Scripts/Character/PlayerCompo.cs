using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerCompo : MonoBehaviour
{
    protected bool _isOwner;
    protected NetworkPlayer _player;
    protected PC _pc;
    public virtual void Initialize(NetworkPlayer player)
    {
        _pc = player.myPc;
        _player = player;
        _isOwner = player.isOwner;
    }
}
