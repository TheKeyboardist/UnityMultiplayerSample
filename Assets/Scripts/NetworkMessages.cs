﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkMessages
{
    public enum Commands
    {
        PLAYER_UPDATE,
        SERVER_UPDATE,
        HANDSHAKE,
        PLAYER_INPUT,
        ID_UPDATE
    }

    [System.Serializable]
    public class NetworkHeader
    {
        public Commands cmd;
    }

    [System.Serializable]
    public class HandshakeMsg:NetworkHeader
    {
        public NetworkObjects.NetworkPlayer player;
        public HandshakeMsg()
        {      // Constructor
            cmd = Commands.HANDSHAKE;
            player = new NetworkObjects.NetworkPlayer();
        }
    }
    
    [System.Serializable]
    public class PlayerUpdateMsg:NetworkHeader
    {
        public NetworkObjects.NetworkPlayer player;
        
        public PlayerUpdateMsg()
        {      // Constructor
            cmd = Commands.PLAYER_UPDATE;
            player = new NetworkObjects.NetworkPlayer();
        }

    };

    public class PlayerInputMsg:NetworkHeader
    {
        public Input myInput;

        public PlayerInputMsg()
        {
            cmd = Commands.PLAYER_INPUT;
            myInput = new Input();
        }
    }
    [System.Serializable]
    public class  ServerUpdateMsg:NetworkHeader
    {
        public List<NetworkObjects.NetworkPlayer> players;
        public ServerUpdateMsg()
        {      // Constructor
            cmd = Commands.SERVER_UPDATE;
            players = new List<NetworkObjects.NetworkPlayer>();
        }
    }
    [System.Serializable]
    public class IDUpdateMsg: NetworkHeader
    {
        public int id;
        public IDUpdateMsg()
        {
            cmd = Commands.ID_UPDATE;
        }

    }

} 

namespace NetworkObjects
{
    [System.Serializable]
    public class NetworkObject
    {
        public int id;
    }
    [System.Serializable]
    public class NetworkPlayer : NetworkObject
    {
        public Color cubeColor; 
        //public Vector3 cubPos;
        public GameObject body;
        public NetworkPlayer()
        {
            cubeColor = new Color();
            id = 0;
        }
    }
}
