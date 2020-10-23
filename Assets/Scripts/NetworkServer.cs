using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class NetworkServer : MonoBehaviour
{
    public NetworkDriver m_Driver;

    public ushort serverPort;
     
    private NativeList<NetworkConnection> m_Connections;
    private NativeList<Vector3> m_listofPlayerPos;
    private List<NetworkObjects.NetworkPlayer> m_listOfPlayers;


    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = serverPort;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port " + serverPort.ToString());
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        m_listOfPlayers = new List<NetworkObjects.NetworkPlayer>();
        StartCoroutine(SendHandShakeToAllClient());

    }


    IEnumerator SendHandShakeToAllClient()
    {
        while (true)
        {
            for (int i = 0; i < m_Connections.Length; i++)
            {
                if (!m_Connections[i].IsCreated)
                    continue;

                //example to send a handshake message
                HandshakeMsg m = new HandshakeMsg();
                m.player.id = m_Connections[i].InternalId;
                SendToClient(JsonUtility.ToJson(m), m_Connections[i]);
            }
            yield return new WaitForSeconds(2);
        }
    }

    IEnumerator SendPlayerPositionToAllClient()
    {
        while (true)
        {
            for (int i = 0; i < m_Connections.Length; i++)
            {
                if (!m_Connections[i].IsCreated)
                    continue;

                ServerUpdateMsg m = new ServerUpdateMsg();
                //m.player.id = m_Connections[i].InternalId.ToString();
                
                SendToClient(JsonUtility.ToJson(m), m_Connections[i]);
            }
            yield return new WaitForSeconds(2);
        }
    }


    void SendToClient(string message, NetworkConnection c)
    {
        var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }
    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    void OnConnect(NetworkConnection c)
    {
        m_Connections.Add(c);
        Debug.Log("Accepted a connection");
        IDUpdateMsg m = new IDUpdateMsg();
        m.id = c.InternalId;
        SendToClient(JsonUtility.ToJson(m), c);
        Debug.Log("Server: Hello, client this your id: " + c.InternalId.ToString());
    }

    void OnData(DataStreamReader stream, int i){
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch(header.cmd){
            case Commands.HANDSHAKE:
            HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
                Debug.Log("Server: Hello, player " + hsMsg.player.id.ToString()); 
            break;
            case Commands.PLAYER_UPDATE:
            PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);

            m_listOfPlayers[i].body.transform.position = puMsg.player.body.transform.position;
                ServerUpdateMsg serUpMsg = new ServerUpdateMsg();
                serUpMsg.players = m_listOfPlayers;
                SendToClient(JsonUtility.ToJson(serUpMsg), m_Connections[i]);
                Debug.Log("Player update message received!");
            break;
            case Commands.SERVER_UPDATE:
            ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
            Debug.Log("Server update message received!");
            break;
            default:
            Debug.Log("SERVER ERROR: Unrecognized message received!");
            break;
        }
    }

    void OnDisconnect(int i)
    {
        Debug.Log("Client disconnected from server");
        m_Connections[i] = default(NetworkConnection);
        m_listOfPlayers[i] = null;
    }

    void Update ()
    {
        m_Driver.ScheduleUpdate().Complete();

        // CleanUpConnections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {

                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        // AcceptNewConnections
        NetworkConnection c = m_Driver.Accept();
        while (c  != default(NetworkConnection))
        {            
            OnConnect(c);

            // Check if there is another new connection
            c = m_Driver.Accept();
        }


        // Read Incoming Messages
        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Assert.IsTrue(m_Connections[i].IsCreated);
            
            NetworkEvent.Type cmd;
            cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            while (cmd != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    OnData(stream, i);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    OnDisconnect(i);
                }

                cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            }
        }
    }
}