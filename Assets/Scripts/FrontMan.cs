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
using Steamworks;
public class FrontMan : ExtendedNetworkBehaviour
{
    public static FrontMan FM;
    public Camera MainCam;

    public uint appid;

    public Text counter;
    public NetworkVariable<float> clicks = new NetworkVariable<float>();
    private void Awake()
    {
        FM = this;
        clicks.Value = 0;
    }

    private void Update()
    {
        counter.text = clicks.Value + "";
    }
}
