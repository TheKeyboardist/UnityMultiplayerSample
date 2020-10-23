using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using NetworkObjects;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
public class NetworkClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    private NativeList<NetworkConnection> m_Connections;
    public List<NetworkObjects.NetworkPlayer> m_players;
    public string serverIP;
    public ushort serverPort;
    public NetworkObjects.NetworkPlayer player;
    public GameObject cubePrefab;

    void Start ()
    {
        //var endpoint = NetworkEndPoint.LoopbackIpv4;
     
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP,serverPort);
        
        m_Connection = m_Driver.Connect(endpoint);
        m_players = new List<NetworkObjects.NetworkPlayer>(4);
        player.body = Instantiate(cubePrefab, new Vector3(0,0,0), Quaternion.identity);
        m_players[0] = player;
        StartCoroutine(SendRepeatedHandshake());
        StartCoroutine(SendRepeatedClientPositionUpdate());
    }

    IEnumerator SendRepeatedHandshake()
    {
        while (true)
        {
            yield return new WaitForSeconds(2);
            HandshakeMsg m = new HandshakeMsg();
            m.player.id = player.id;
            SendToServer(JsonUtility.ToJson(m));
            Debug.Log("(Client: " + m.player.id.ToString() + ") Sending a handshake");
        }
    }

    IEnumerator SendRepeatedClientPositionUpdate()
    {
        while (true)
        {
            //yield return new WaitForSeconds(1);

            PlayerUpdateMsg m = new PlayerUpdateMsg();
            m.player.id = m_Connection.InternalId;
            m.player.body.transform.position = player.body.transform.position;
            SendToServer(JsonUtility.ToJson(m));
            Debug.Log("(Client " + player.id.ToString() + ") " + player.body.transform.position);
        }
    }


    IEnumerator InstantiateNewPlayer()
    {
        while (true)
        {
            for (int i = 0; i < m_players.Count; i++)
            {
                if(i  == player.id)
                {

                }
                else if (!m_players[i].body)
                {
                    
                    m_players[i].body = Instantiate(cubePrefab, new Vector3(0,0,0), Quaternion.identity);
                }
            }
        }
    }

    void SendToServer(string message)
    {
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void OnConnect()
    {
        Debug.Log("We are now connected to the server");

       
         HandshakeMsg m = new HandshakeMsg();
         m.player.id = player.id;
         SendToServer(JsonUtility.ToJson(m));
    }

    void OnData(DataStreamReader stream)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch(header.cmd){
            case Commands.HANDSHAKE:
            HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
            Debug.Log("Handshake message received!");
            break;
            case Commands.PLAYER_UPDATE:
            PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
            Debug.Log("Player update message received!");
            break;
            case Commands.SERVER_UPDATE:
            ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                m_players = suMsg.players;
                for (int i = 0; i < m_players.Count; i++)
                {
                    Debug.Log("(player " + m_players[i].id + ") " + "position: " + m_players[i].body.transform.position);
                }
            Debug.Log("Server update message received!");
            break;
            case Commands.ID_UPDATE:
            IDUpdateMsg idMsg = JsonUtility.FromJson<IDUpdateMsg>(recMsg);
            player.id = idMsg.id;
            Debug.Log("Client: Server returned me my new id: " + player.id.ToString());
            break;
            default:
            Debug.Log("Unrecognized message received!");
            break;
        }
    }

    void Disconnect()
    {
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
    }

    void OnDisconnect(){
        Debug.Log("Client got disconnected from server");
        m_Connection = default(NetworkConnection);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }   
    void Update()
    {
    
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                OnDisconnect();
            }

            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }
    }
}