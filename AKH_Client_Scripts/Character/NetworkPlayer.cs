using Server;
using Server.C2SInGame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static KeyAction;

public class NetworkPlayer : MonoBehaviour, IPlayerActions
{
    public PC myPc;

    public bool isOwner { get; private set; }

    public KeyAction keyAction;
    public Rigidbody rb;
    public AudioSource audioSource;
    public PlayerAnimation body;

    public float jumpPower;
    public bool isDead = false;

    private Dictionary<Type, PlayerCompo> _components;
    public Transform weaponHolder;

    private PlayerWeapon weapon;
    [SerializeField] private Transform _groundDetector;
    [SerializeField] private Vector3 _detectSize;
    [SerializeField] private LayerMask _groundLayer;

    public bool isWeapon = false;
    public bool HaveWeapon => weapon != null;
    public NotifyValue<bool> isGround = new NotifyValue<bool>();
    public UnityEvent IsCanCollision;


    public void Initialize(PC pc)
    {
        myPc = pc;
        isOwner = pc.MyPC;

        body = GetComponent<PlayerAnimation>();
        audioSource = GetComponent<AudioSource>();
        #region SetIPlayerCompo
        _components = new Dictionary<Type, PlayerCompo>();
        GetComponentsInChildren<PlayerCompo>().ToList().ForEach(x => _components.Add(x.GetType(), x));
        _components.Values.ToList().ForEach(compo => compo.Initialize(this));
        #endregion
        isGround.OnvalueChanged += HandleGround;
        rb = GetComponentInParent<Rigidbody>();
        if (!isOwner)
        {
            return;
        }
        keyAction = new KeyAction();

        keyAction.Player.Enable();
        keyAction.Player.Jump.started += OnJump;
        keyAction.Player.Attack.started += OnAttack;
        keyAction.Player.Collect.started += OnCollect;

    }

    private void HandleGround(bool prev, bool next)
    {
        body.PlayerAnimationChange("isJump", !next);
        audioSource.volume = next ? 1 : 0;
    }

    private void Update()
    {
        if (!isOwner) return;

        if (Input.GetKeyDown(KeyCode.E))
            ToggleWeapon();
        DetectGround();
    }

    private void DetectGround()
    {
        isGround.Value = Physics.OverlapBox(_groundDetector.position, _detectSize, Quaternion.identity, _groundLayer).Length > 0;
    }

    private void ToggleWeapon()
    {
        if (weapon == null) return;
        isWeapon = !isWeapon;
        SetWeapon();
    }
    public void SetWeapon()
    {
        if (weapon == null) return;
        body.ChangeWeaponAni(isWeapon);
        weapon.gameObject.SetActive(isWeapon);
    }
    #region IPlayerCOmpo
    public T GetCompo<T>() where T : class
    {
        Type t = typeof(T);
        if (_components.TryGetValue(t, out PlayerCompo compo))
        {
            return compo as T;
        }
        return default;
    }
    public void AddCompo<T>(T compo) where T : class
    {
        Type t = typeof(T);
        if (!_components.TryGetValue(t, out PlayerCompo c))
        {
            _components.Add(t, compo as PlayerCompo);
        }
    }
    public void RemoveCompo<T>(T compo) where T : class
    {
        Type t = typeof(T);
        if (_components.TryGetValue(t, out PlayerCompo c))
        {
            _components.Remove(t);
        }
    }
    #endregion
    #region input

    public void Jump()
    {
        if (isOwner)
            rb.AddForce(new Vector3(0, jumpPower, 0), ForceMode.Impulse);
        body.PlayerAnimationChange("isJump", true);
    }

    public void OnMove(InputAction.CallbackContext context)
    {

    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (isDead) return;
        if (isGround.Value)
            Jump();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (isDead) return;
        if (context.started && isWeapon)
        {
            weapon.OnAttack += RecvAtkInfo;
            weapon.Attack();
            body.AttackAnimation();
        }
    }
    public void OnCollect(InputAction.CallbackContext context)
    {
        if (isDead) return;
        IsCanCollision?.Invoke();
    }

    #endregion
    #region attack
    public void AddWeapon(PlayerWeapon w)
    {
        weapon = w;
        AddCompo(w);
    }
    public void RemoveWeapon()
    {
        if (weapon == null) return;
        RemoveCompo(weapon);
        Destroy(weapon.gameObject);
        isWeapon = false;
        body.ChangeWeaponAni(false);
        weapon = null;
    }
    public void RecvAtkInfo(PC[] pcs)
    {
        var attack = new AttackReq()
        {
            AttackInfos = new List<Server.AttackInfo>()
        };
        if (pcs.Length == 0)
            attack.AttackInfos.Add(new Server.AttackInfo
            {
                TargetIndex = -1,
                TimeOffset = 0,
                PostHitPosition = weapon.hitPos.Vector3ToFLocation(),
                PostHitDirection = 0,
                SkillId = 0,
                Dest = new Server.FLocation(),
                Direction = body.transform.eulerAngles.Vector3ToFLocation(),
                Pos = myPc.transform.position.Vector3ToFLocation(),
            });
        else
            foreach (var pc in pcs)
            {
                if (pc == myPc) continue;
                attack.AttackInfos.Add(new Server.AttackInfo
                {
                    TargetIndex = pc.Index,
                    TimeOffset = 0,
                    PostHitPosition = pc.transform.position.Vector3ToFLocation(),
                    PostHitDirection = pc.transform.eulerAngles.y,
                    SkillId = 0,
                    Dest = new Server.FLocation(),
                    Direction = body.transform.eulerAngles.Vector3ToFLocation(),
                    Pos = myPc.transform.position.Vector3ToFLocation(),
                });
            }
        Manager.Net.SendPacket(attack, Server.PacketType.AttackReq);
        weapon.OnAttack -= RecvAtkInfo;
        if (weapon is NetworkGun)
            RemoveWeapon();
    }
    public void Attack(Vector3 dir)
    {
        if (!isOwner && myPc.MyJob != Job.Murder)
        {
            (weapon as NetworkGun).ShootBullet(dir);
            body.ShootAnimation();
            RemoveWeapon();
        }

        if (!isOwner && myPc.MyJob == Job.Murder)
        {
            body.AttackAnimation();
        }
    }
    #endregion
    public void Dead()
    {
        body.PlayerAnimationChange("isDead", true);
        isDead = true;
    }
    public void Revive()
    {
        body.PlayerAnimationChange("isDead", false);
        isDead = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(_groundDetector.position, _detectSize);
    }

    // ÆÄÃ÷ È¹µæ Å° ÀÔ·Â
#endif
}
