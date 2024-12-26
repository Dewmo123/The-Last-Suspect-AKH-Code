using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerWeapon : PlayerCompo
{
    public event Action<PC[]> OnAttack;
    public Vector3 hitPos;

    public abstract void Attack();

    protected void InvokeAttack(PC[]pcs)
    {
        OnAttack?.Invoke(pcs);
    }
    protected void InvokeAttack(PC pc)
    {
        PC[] pcs = new PC[1] { pc };
        OnAttack?.Invoke(pcs);
    }
    protected void InvokeAttack()
    {
        PC[] pcs = new PC[0];
        OnAttack?.Invoke(pcs);
    }
}
