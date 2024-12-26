using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using NetBase;
using Server;
using Server.C2SInGame;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct PacketHeader
{
    public ushort PacketSize;
    public ushort PacketType;

    public const int HeaderSize = 4; // 2 bytes for PacketSize, 2 bytes for PacketType

    public static PacketHeader FromBytes(byte[] buffer)
    {
        PacketHeader header = new PacketHeader { PacketSize = BitConverter.ToUInt16(buffer, 0), PacketType = BitConverter.ToUInt16(buffer, 2) };
        return header;
    }

    public byte[] ToBytes()
    {
        byte[] buffer = new byte[HeaderSize];
        BitConverter.GetBytes(PacketSize).CopyTo(buffer, 0);
        BitConverter.GetBytes(PacketType).CopyTo(buffer, 2);
        return buffer;
    }
}

public class PacketHandler
{
    private Dictionary<PacketType, Action<MemoryStream, Socket>> _packetHandlers;

    public PacketHandler()
    {
        _packetHandlers = new Dictionary<PacketType, Action<MemoryStream, Socket>>
        {
            { PacketType.ResourceLoadCompleteAck, HandleResourceLoadCompleteAck },
            { PacketType.ZoneUpdateAck, HandleZoneUpdateAck },
            { PacketType.MoveAck, HandleMoveAck },
            { PacketType.GameStartAck, HandleGameStartAck },
            { PacketType.AttackAck,HandleAttackAck },
            { PacketType.GameEndAck, HandleGameEndAck },
            { PacketType.AddGunAck, HandleAddGunAck },
            { PacketType.CollectPartsAck, HandleCollectAck }
        };
    }



    public void HandlePacket(MemoryStream packetStream, Socket handler)
    {
        packetStream.Position = 0;
        PacketHeader header = DeserializeHeader(packetStream);

        if (_packetHandlers.TryGetValue((PacketType)header.PacketType, out Action<MemoryStream, Socket> handlerAction))
        {
            byte[] bodyData = new byte[packetStream.Length - PacketHeader.HeaderSize];
            packetStream.Read(bodyData, 0, bodyData.Length);
            MemoryStream bodyStream = new MemoryStream(bodyData);

            Console.WriteLine($"[PACKET] Received packet: Type {header.PacketType}, Size {header.PacketSize}");
            handlerAction(bodyStream, handler);
        }
        else
        {
            Console.WriteLine($"[PACKET] Unknown packet type: {header.PacketType}");
        }
    }

    private PacketHeader DeserializeHeader(MemoryStream stream)
    {
        byte[] headerData = new byte[PacketHeader.HeaderSize];
        stream.Read(headerData, 0, PacketHeader.HeaderSize);

        return new PacketHeader { PacketSize = BitConverter.ToUInt16(headerData, 0), PacketType = BitConverter.ToUInt16(headerData, 2) };
    }
    private void HandleCollectAck(MemoryStream stream, Socket socket)
    {
        var data = stream.ToArray();
        var packet = new PacketBase();
        packet.SetPacketData(data);
        var collect = packet.Read<CollectPartsAck>();
        Debug.Log(collect.type.ToString());
        Manager.Char.MyPC.player.GetCompo<SenseParts>().Collect(collect.type);
        Manager.Data.ClientCanvas.CivilianInventory.SetPartsEnable();
    }
    private void HandleAddGunAck(MemoryStream stream, Socket socket)
    {
        byte[] data = stream.ToArray();
        var packet = new PacketBase();
        packet.SetPacketData(data);
        AddGunAck addGun = packet.Read<AddGunAck>();
        PC pc;
        if (Manager.Char.MyPC.Index == addGun.Index)
            pc = Manager.Char.MyPC;
        else if (!Manager.Char.IndexPC_Dictionary.TryGetValue(addGun.Index, out pc))
            return;
        if (pc.MyJob == Job.Murder || pc.player.HaveWeapon) return;
        var gun = GameObject.Instantiate(Manager.Data.PlayerGun, pc.player.weaponHolder);
        if (pc.MyPC)
        {
            pc.player.GetCompo<SenseParts>().ResetParts();
            Manager.Data.ClientCanvas.CivilianInventory.SetPartsEnable();
        }
        pc.player.AddWeapon(gun.GetComponent<NetworkGun>());
    }
    private void HandleGameEndAck(MemoryStream stream, Socket socket)
    {
        Manager.Data.ClientCanvas.Timer.StopTimer();
        byte[] data = stream.ToArray();
        var packet = new PacketBase();
        packet.SetPacketData(data);
        GameEndAck end = packet.Read<GameEndAck>();
        Manager.Game.SetGaming(false);
        Manager.Data.ClientCanvas.Minimap.SetMinimap(false);
        foreach (var item in Manager.Char.IndexPC_Dictionary)
        {
            item.Value.player.GetCompo<SenseParts>().ResetParts();
            if (item.Value.isGaming)
                item.Value.player.RemoveWeapon();
            if (item.Value.player.isDead)
                item.Value.Revive();
            item.Value.isGaming = false;
        }
        if (Manager.Char.MyPC.isGaming)
        {
            Manager.Char.MyPC.player.RemoveWeapon();
            Manager.Data.ClientCanvas.Gameover.Gameover(end.WinJob);
            Manager.Char.MyPC.SetPosition(new Vector3(-232, 0, 312));
            if (Manager.Char.MyPC.MyJob == end.WinJob || (Manager.Char.MyPC.MyJob == Job.Police && end.WinJob == Job.Economy))
                WinLoseSound.instance.PlayWin();
            else
                WinLoseSound.instance.PlayLose();
        }
        Manager.Char.MyPC.player.GetCompo<SenseParts>().ResetParts();
        if (Manager.Char.MyPC.player.isDead)
            Manager.Char.MyPC.Revive();
        Manager.Char.MyPC.isGaming = false;
    }
    private void HandleGameStartAck(MemoryStream stream, Socket socket)
    {
        byte[] packetData = stream.ToArray();
        var packet = new PacketBase();
        packet.SetPacketData(packetData);

        GameStartAck start = new GameStartAck();
        start = packet.Read<GameStartAck>();
        Manager.Game.SetGaming(true);
        Manager.Data.ClientCanvas.Timer.SetTime(300);
        Manager.Data.ClientCanvas.Minimap.SetMinimap(true, MapType.Japan);
        foreach (var item in start.JobsAndPos)
        {
            PC pc;
            if (item.Index == Manager.Char.MyPC.Index)
            {
                pc = Manager.Char.MyPC;
                Manager.Data.ClientCanvas.SetRole.SetRole(item.MyJob);
            }
            else
                pc = Manager.Char.IndexPC_Dictionary[item.Index];
            pc.isGaming = item.isGaming;
            pc.MyJob = item.MyJob;
            pc.SetPosition(item.Pos.FLocationToVector3());
            switch (item.MyJob)
            {
                case Job.Murder:
                    var gun = GameObject.Instantiate(Manager.Data.PlayerNife, pc.player.weaponHolder);
                    pc.player.AddWeapon(gun.GetComponent<NetworkNife>());
                    gun.SetActive(false);
                    break;
                case Job.Police:
                    var nife = GameObject.Instantiate(Manager.Data.PlayerGun, pc.player.weaponHolder);
                    pc.player.AddWeapon(nife.GetComponent<NetworkGun>());
                    nife.SetActive(false);
                    break;
            }
        }
    }
    private void HandleResourceLoadCompleteAck(MemoryStream packetStream, Socket handler)
    {
        Console.WriteLine("[PACKET] Handling ResourceLoadCompleteAck");
        byte[] packetData = packetStream.ToArray();
        Console.WriteLine($"[PACKET] Packet data length: {packetData.Length} bytes");
        var packet = new PacketBase();
        packet.SetPacketData(packetData);


        ResourceLoadCompleteAck resourceLoadCompleteAck = new ResourceLoadCompleteAck();
        resourceLoadCompleteAck = packet.Read<ResourceLoadCompleteAck>();
        Manager.Game.SetGaming(resourceLoadCompleteAck.isGaming);
        Manager.Data.MyPC_Index = resourceLoadCompleteAck.PcIndex;
        Console.WriteLine($"[PACKET] ResourceLoadCompleteAck PcIndex:{resourceLoadCompleteAck.PcIndex}");
    }

    private void HandleZoneUpdateAck(MemoryStream packetStream, Socket handler)
    {
        byte[] packetData = packetStream.ToArray();
        var packet = new PacketBase();
        packet.SetPacketData(packetData);

        ZoneUpdateAck zoneUpdate = new ZoneUpdateAck();
        zoneUpdate = packet.Read<ZoneUpdateAck>();
        Manager.Game.SetGaming(zoneUpdate.isGaming);

        #region PCEnters
        // PC Enters
        if (zoneUpdate.PcEnters != null && zoneUpdate.PcEnters.Count > 0)
        {
            foreach (var pc in zoneUpdate.PcEnters)
            {
                Manager.Char.CreatePC(pc);
            }
        }
        else
        {
            Console.WriteLine("[PACKET] No PC Enters in this update.");
        }
        #endregion

        #region Moves
        // Moves
        if (zoneUpdate.Moves != null && zoneUpdate.Moves.Count > 0)
        {
            foreach (var move in zoneUpdate.Moves)
            {
                if (move.Index == Manager.Data.MyPC_Index && Manager.Char.MyPC != null)
                {
#if UNITY_EDITOR
                    Manager.Char.MyPC.EnqueuePosition(move.Pos.FLocationToVector3());
#endif
                }
                else if (Manager.Char.IndexPC_Dictionary.TryGetValue(move.Index, out PC targetPC))
                {
                    var moveCompo = targetPC.player.GetCompo<PlayerMove>();
                    moveCompo.EnqueueDestinationPosition(move.Pos.FLocationToVector3());
                    moveCompo.ModelRatation = move.Dir.FLocationToVector3();

                    moveCompo.moveSpeed = move.speed * 10;
                    moveCompo.isWalk = move.isWalk;

                    targetPC.player.isWeapon = move.IsWeapon;
                    targetPC.player.SetWeapon();
#if UNITY_EDITOR
                    targetPC.EnqueuePosition(move.Pos.FLocationToVector3());
#endif
                }
                else
                {
                    Debug.Log("can't Find");
                }
            }
        }
        else
        {
            Console.WriteLine("[PACKET] No Moves in this update.");
        }
        #endregion
        #region Removes
        // Removes
        if (zoneUpdate.Removes != null && zoneUpdate.Removes.Count > 0)
        {
            Console.WriteLine("[PACKET] Removes:");
            foreach (var remove in zoneUpdate.Removes)
            {
                Console.WriteLine($"[PACKET]   Index: {remove.Index}");
                Console.WriteLine($"[PACKET]   Object Type: {remove.ObjectType}");
                //Manager.Char.log.LogInfo("RemoveClient: " + remove.Index);
                if (remove.ObjectType == EObjectType.Pc)
                    GameObject.Destroy(Manager.Char.IndexPC_Dictionary[remove.Index].gameObject);
                else if (remove.ObjectType == EObjectType.Parts)
                {
                    Manager.Parts.RemovePartsWithIndex(remove.Index);
                }

            }
        }
        else
        {
            Console.WriteLine("[PACKET] No Removes in this update.");
        }
        #endregion
        #region Attacks
        if (zoneUpdate.Attacks != null && zoneUpdate.Attacks.Count > 0 && Manager.Char.MyPC.isGaming)
        {
            foreach (var item in zoneUpdate.Attacks)
            {
                if (item.SrcObjectIndex == Manager.Data.MyPC_Index && Manager.Char.MyPC != null)
                {
                    if (item.TarObjectIndex == -1)
                        return;
                    var attacker = Manager.Char.MyPC;
                    if (item.Result == EAttackResult.Double)
                    {
                        Manager.Data.ClientCanvas.DieUI.CharacterDied("Me");
                        attacker.Dead();
                    }
                    Manager.Char.IndexPC_Dictionary[item.TarObjectIndex].Dead();
                }
                else if (Manager.Char.IndexPC_Dictionary.TryGetValue(item.SrcObjectIndex, out PC attacker))
                {
                    attacker.player.Attack(item.PostHitPosition.FLocationToVector3());
                    if (item.TarObjectIndex == -1)
                        return;
                    if (item.Result == EAttackResult.Double)
                        attacker.Dead();
                    if (item.TarObjectIndex == Manager.Char.MyPC.Index)
                    {
                        Manager.Data.ClientCanvas.DieUI.CharacterDied(attacker.PcName);
                        Manager.Char.MyPC.Dead();
                    }
                    else
                        Manager.Char.IndexPC_Dictionary[item.TarObjectIndex].Dead();
                }
                else
                {
                    Debug.LogError($"PC: {item.SrcObjectIndex} Not Fount");
                }
            }
        }
        #endregion

        if (zoneUpdate.Parts != null && zoneUpdate.Parts.Count > 0)
        {
            foreach (var item in zoneUpdate.Parts)
            {
                Manager.Parts.CreateParts(item);
            }
        }
    }
    private void HandleAttackAck(MemoryStream stream, Socket handler)
    {

    }
    private void HandleMoveAck(MemoryStream packetStream, Socket handler)
    {
        byte[] packetData = packetStream.ToArray();
        var packet = new PacketBase();
        packet.SetPacketData(packetData);
        MoveAck moveAck = new MoveAck();
        moveAck = packet.Read<MoveAck>();
    }
}

public class NetworkManager : MonoBehaviour
{
    private bool _running = true;
    private List<byte> _receivedDataBuffer = new List<byte>();
    private PacketHandler _packetHandler;
    private Socket sender;

    private void ProcessReceivedData(Socket socket, byte[] receivedData, int bytesReceived)
    {
        _receivedDataBuffer.AddRange(receivedData.Take(bytesReceived));

        while (_receivedDataBuffer.Count >= PacketHeader.HeaderSize)
        {
            byte[] headerBytes = _receivedDataBuffer.Take(PacketHeader.HeaderSize).ToArray();
            PacketHeader receivedHeader = PacketHeader.FromBytes(headerBytes);

            if (_receivedDataBuffer.Count >= receivedHeader.PacketSize)
            {
                byte[] packetData = _receivedDataBuffer.Take(receivedHeader.PacketSize).ToArray();

                using (MemoryStream packetStream = new MemoryStream(packetData))
                {
                    _packetHandler.HandlePacket(packetStream, socket);
                }

                _receivedDataBuffer.RemoveRange(0, receivedHeader.PacketSize);
            }
            else
            {
                break;
            }
        }
    }

    public void Initialize()
    {
        _packetHandler = new PacketHandler();
        StartCoroutine(CoInitializeServer());
    }

    private IEnumerator CoInitializeServer()
    {
        //var serverInfo = SetServerEnterInfo();
        IPAddress ipAddress;
        ipAddress = IPAddress.Parse("172.31.0.250");
        IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

        using (sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            try
            {
                sender.Connect(remoteEP);
            }
            catch
            {
                remoteEP = new IPEndPoint(IPAddress.Parse("192.168.56.1"), 11000);
                sender.Connect(remoteEP);
            }
            Console.WriteLine($"[PACKET] Socket connected to {sender.RemoteEndPoint.ToString()}");

            sender.NoDelay = true;
            sender.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);
            sender.Blocking = false;

            var responsePacket = new PacketBase();
            ResourceLoadCompleteReq resourceLoadCompleteReq = new ResourceLoadCompleteReq();
            resourceLoadCompleteReq.name = PlayerPrefs.GetString("nickname");
            responsePacket.Write(resourceLoadCompleteReq);
            SendPacket(PacketType.ResourceLoadCompleteReq, responsePacket.GetPacketData());

            int SendEscapeMoveCount = 0;

            byte[] buffer = new byte[1024 * 16];
            while (_running)
            {
                bool dataReceived = false;
                do
                {
                    if (sender.Poll(0, SelectMode.SelectRead))
                    {
                        int bytesReceived = sender.Receive(buffer);
                        if (bytesReceived > 0)
                        {
                            ProcessReceivedData(sender, buffer, bytesReceived);
                            dataReceived = true;
                        }
                        else
                        {
                            _running = false;
                            break;
                        }

                        yield return null;
                    }
                    else
                    {
                        dataReceived = false;
                    }
                } while (dataReceived);

                if (_running && !dataReceived)
                {
                    Thread.Sleep(10);
                    yield return new WaitForSeconds(0.1f);
                    if (SendEscapeMoveCount % 10 == 0)
                    {
                        SendEscapeMoveCount = 0;
                    }

                    SendEscapeMoveCount++;
                }
            }

            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
        }
    }


    private void OnApplicationQuit()
    {
        _running = false;
    }
    public void Quit()
    {
        _running = false;
    }

    public void SendPacket<T>(T playerMoveReq, PacketType type) where T : struct
    {
        var responsePacket = new PacketBase();
        responsePacket.Write(playerMoveReq);
        SendPacket(type, responsePacket.GetPacketData());
    }

    private void SendPacket(PacketType packetType, byte[] data)
    {
        ushort packetSize = (ushort)(PacketHeader.HeaderSize + data.Length);
        PacketHeader header = new PacketHeader { PacketSize = packetSize, PacketType = (ushort)packetType };
        byte[] headerBytes = header.ToBytes();

        byte[] packet = new byte[packetSize];
        headerBytes.CopyTo(packet, 0);
        data.CopyTo(packet, PacketHeader.HeaderSize);

        int bytesSent = sender.Send(packet);
    }

    private Dictionary<string, string> mConfig = new Dictionary<string, string>();

    private (string ServerIP, int ServerPort) SetServerEnterInfo()
    {

#if UNITY_EDITOR
        var configFilePath = "Assets/Editor/Debug/config.txt";
#else
        var configFilePath = "C:/Users/K/ggms unity/ExamMulti/Day2/Client/MMORPG/Assets/Editor/Debug";
#endif
        if (File.Exists(configFilePath))
        {
            foreach (string line in File.ReadAllLines(configFilePath))
            {
                string[] parts = line.Split('=');
                if (parts.Length == 2)
                {
                    mConfig[parts[0].Trim()] = parts[1].Trim();
                }
            }

            Debug.Log($"Config file loaded successfully: {configFilePath}");

            return (mConfig["ServerIP"], int.Parse(mConfig["ServerPort"]));

        }
        else
        {
            Debug.LogError($"Config file not found: {configFilePath}");

            return (String.Empty, -1);
        }
    }

}