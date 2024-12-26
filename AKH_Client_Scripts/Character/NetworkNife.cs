using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkNife : PlayerWeapon
{
    public PlayerSound sound;
    [SerializeField] private LayerMask _pcLayer;
    [SerializeField] private float _attackRadius;
    [SerializeField] private Transform _trm;

    private void Awake()
    {
        sound = GetComponentInParent<PlayerSound>();
    }

    public override void Attack()
    {
        
        Collider[] col = Physics.OverlapSphere(_trm.position, _attackRadius, _pcLayer);
        List<PC> targets = new List<PC>();
        col.ToList().ForEach(item => targets.Add(item.GetComponent<PC>()));
        if(targets.Count > 0)
        {
            sound.SetSound(sound.knife);
            sound.PlaySound();
        }
        InvokeAttack(targets.ToArray());
    }

    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_trm.position, _attackRadius);
    }
#endif
}
