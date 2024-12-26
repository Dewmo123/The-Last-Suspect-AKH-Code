/*
 * PlayerCharacter에 관한 코드를 작성합니다.
 * 캐릭터의 상태변화, 이동 등 기본적인 동작을 정의합니다.
 * 서버와의 통신을 통해 위치 동기화를 수행합니다.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.C2SInGame;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 플레이어 캐릭터를 제어하는 메인 클래스
/// </summary>
public class PC : MonoBehaviour
{
    // 플레이어의 고유 인덱스
    public int Index;

    public Job MyJob;
    // 현재 플레이어가 로컬 플레이어인지 여부
    public bool MyPC = false;

    public string PcName;

    // 캐릭터의 3D 모델을 포함하는 Transform
    public Transform ModelTransform;
    // 캐릭터 모델의 회전값
    public Vector3 ModelRatation;
    public NetworkPlayer player;
    private PlayerMove MoveCompo;
    // 캐릭터 애니메이션 컨트롤러
    private PlayerAnimation PC_Animation;

    // 위치 전송을 위한 코루틴 참조
    private Coroutine mSendPositionCoroutine;
    // 위치 전송 간격을 정의하는 대기 시간 (0.3초)
    private WaitForSeconds mSendWaitTime = new WaitForSeconds(0.03f);

    public bool isGaming = false;

    #region 고유 기능 정의

    /// <summary>
    /// 캐릭터를 초기화하고 위치 전송을 시작합니다.
    /// 애니메이션 초기화와 위치 전송 코루틴을 시작합니다.
    /// </summary>
    public void Initialize()
    {
        player.Initialize(this);
        MoveCompo = player.GetCompo<PlayerMove>();
        gameObject.SetActive(true);
        player.GetCompo<nicknameUI>().SetCharacterName(PcName);

        if (MyPC == true)
        {
            mSendPositionCoroutine = StartCoroutine(CoSendPosition());
        }
    }
    /// <summary>
    /// 캐릭터의 위치를 지정된 좌표로 설정합니다.
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    /// <summary>
    /// 주기적으로 서버에 캐릭터의 위치 정보를 전송하는 코루틴입니다.
    /// 0.3초 간격으로 현재 위치와 회전값을 서버로 전송합니다.
    /// </summary>
    private IEnumerator CoSendPosition()
    {
        while (true)
        {
            yield return mSendWaitTime;
            var playerDestPosition = transform.position;
            var playerRotation = ModelTransform.eulerAngles;

            MoveReq moveReq = new MoveReq
            {
                Direction = playerRotation.Vector3ToFLocation(),
                Dest = playerDestPosition.Vector3ToFLocation(),
                speed = MoveCompo.moveSpeed,
                IsWalk = MoveCompo.isWalk,
                IsWeapon = player.isWeapon
            };

            Manager.Net.SendPacket(moveReq, PacketType.MoveReq);
        }
    }
    public void Dead()
    {
        Debug.Log("Die" + gameObject.name);
        player.Dead();
        if (MyPC)
        {
            StopCoroutine(mSendPositionCoroutine);
        }
        gameObject.layer = 21;
    }
    public void Revive()
    {
        player.Revive();
        gameObject.layer = 11;
        if (MyPC)
            mSendPositionCoroutine = StartCoroutine(CoSendPosition());
    }
    public void SetName(string name)
    {
        PcName = name;
        Debug.Log(name);
    }
    #endregion

    #region 디버깅 기능 정의

#if UNITY_EDITOR
    // 디버그 표시할 최대 위치 개수
    public int MaxDebugPositionCount;
    // 디버그 위치 표시 크기
    public float DebugPositionSize;
    // 전송된 위치들을 저장하는 큐
    private Queue<Vector3> mSendPositionQueue = new Queue<Vector3>();

    /// <summary>
    /// Unity Editor에서 위치 정보를 시각적으로 표시합니다.
    /// 빨간색 구체로 이동 경로를 시각화합니다.
    /// </summary>
    public void OnDrawGizmos()
    {
        while (mSendPositionQueue.Count > MaxDebugPositionCount)
        {
            mSendPositionQueue.Dequeue();
        }

        foreach (var serverPosition in mSendPositionQueue)
        {
            if (MyPC == true)
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = Color.blue;
            }

            Gizmos.DrawSphere(serverPosition, DebugPositionSize);
        }
    }

    /// <summary>
    /// 디버그용 위치 정보를 큐에 추가합니다.
    /// Unity Editor에서만 동작하는 조건부 컴파일 메서드입니다.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void EnqueuePosition(Vector3 position)
    {
        mSendPositionQueue.Enqueue(position);
    }

#endif
    #endregion
}