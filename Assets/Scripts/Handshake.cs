using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Sirenix.OdinInspector;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine.EventSystems;
using static FrontMan;

public class Handshake : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseDown()
    {
        GetComponent<Animator>().Play("Base Layer.HandshakeBounce");
        ClickedServerRpc();
    }

    [ServerRpc(RequireOwnership =false)]
    void ClickedServerRpc()
    {
        FM.clicks.Value++;
    }
}
