using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetworkMessages;

public class PlayerController : MonoBehaviour
{

    public float playerSpeed = 10;
    //NetworkObjects.NetworkPlayer player;


    // Start is called before the first frame update
    void Start()
    {
        //player = GameObject.Find("NetworkMan Client").GetComponent<NetworkObjects.NetworkPlayer>();
    }

    // Update is called once per frame
    void Update()
    {
       // if (gameObject.name != networkClient.myAddress)
       // {
      //      return;
      //  }
        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(Vector3.forward * Time.deltaTime * playerSpeed);
        }

        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(-Vector3.forward * Time.deltaTime * playerSpeed);
        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(Vector3.left * Time.deltaTime * playerSpeed);
        }

        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(-Vector3.left * Time.deltaTime * playerSpeed);
        }
    }
}
