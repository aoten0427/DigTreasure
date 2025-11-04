using Fusion;
using Fusion.Sockets;
using System;
using System.Runtime.InteropServices;
using UnityEngine;



public class NetworkRPCTest : NetworkBehaviour
{
    [SerializeField] NetWork.GameLauncher gameLauncher;

    struct TestData : INetworkStruct
    {
        byte data;//1byte
        //byte data2;
    }

    const int setnbyte = 200;

    public override void Spawned()
    {
        gameLauncher.OnReliableDataReceived += OnReliableDataReceived;
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.B))
        {
            Sentdata();
        }

        
        if (Input.GetKeyUp(KeyCode.R))
        {
            SendDataWithReliableData();
        }
    }

    private void Sentdata()
    {
        int size = Marshal.SizeOf<TestData>();
        size /= 4;
        Debug.Log($"送るデータ:{setnbyte / size}");

        TestData[] sent = new TestData[setnbyte / size];
        RPC_SentData(sent);
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = false)]
    private void RPC_SentData(TestData[] data)
    {
        Debug.Log($"おくられたでーた:{data.Length}");
    }

    /// <summary>
    /// OnReliableDataReceived を使った送信テスト
    /// </summary>
    private void SendDataWithReliableData()
    {

        // テストデータを生成
        const int testDataCount = 1000;
        byte[] testData = new byte[testDataCount];
        for (int i = 0; i < testDataCount; i++)
        {
            testData[i] = (byte)(i % 256);
        }

        Debug.Log($"[ReliableData送信] {testDataCount}バイト送信開始");

        var key = ReliableKey.FromInts(42, 0, 0, 0);


        // 全プレイヤーに送信
        foreach (var player in Runner.ActivePlayers)
        {
            if (player == Runner.LocalPlayer) continue; // 自分には送らない

            Runner.SendReliableDataToPlayer(player, key, testData);
        }

        Debug.Log($"[ReliableData送信] 送信完了");
    }

    /// <summary>
    /// OnReliableDataReceived でデータを受信
    /// </summary>
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        Debug.Log($"[ReliableData受信] Player={player.PlayerId}, データサイズ={data.Count}バイト");

        // 最初の10バイトを表示
        int displayCount = Mathf.Min(10, data.Count);
        string preview = "最初の" + displayCount + "バイト: ";
        for (int i = 0; i < displayCount; i++)
        {
            preview += data.Array[data.Offset + i] + " ";
        }
        Debug.Log(preview);
    }
}
