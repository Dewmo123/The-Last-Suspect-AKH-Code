using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkGun : PlayerWeapon
{
    public PlayerSound sound;
    public GameObject _bulletPrefab;
    public Transform _bulletSpawn;
    public float _bulletVelocity = 30f;
    public float _bulleyPrfLifeTime = 3f;
    [SerializeField] private LayerMask _canHitLayer;
    private Vector3 _screenCenter;
    private LineRenderer _line;
    private Ray _ray;
    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        sound = GetComponentInParent<PlayerSound>();
    }

    public override void Attack()
    {
        _screenCenter = new Vector3(Camera.main.pixelWidth / 2, Camera.main.pixelHeight / 2);
        sound.SetSound(sound.gun);
        sound.PlaySound();
        _ray = Camera.main.ScreenPointToRay(_screenCenter);
        RaycastHit info = new RaycastHit();
        bool detect = Physics.Raycast(_ray, out info, 1000, _canHitLayer);
        ShootBullet(info.point);
        if (detect)
        {
            if (info.collider.TryGetComponent(out PC pc))
            {
                InvokeAttack(pc);
                return;
            }
        }
        InvokeAttack();
    }
    public void ShootBullet(Vector3 dir)
    {
        hitPos = dir;
        GameObject bullet = Instantiate(_bulletPrefab, _bulletSpawn.position, Quaternion.identity);
        bullet.GetComponent<Bullet>().SetDir(dir - _bulletSpawn.position);

        StartCoroutine(DestroyBulletAfterTime(bullet, _bulleyPrfLifeTime));
    }

    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);

        Destroy(bullet);
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(Camera.main.ScreenPointToRay(_screenCenter));
    }
#endif

}