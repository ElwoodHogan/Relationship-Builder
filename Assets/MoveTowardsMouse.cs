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

public class MoveTowardsMouse : ExtendedNetworkBehaviour
{

    private void OnMouseDrag()
    {
        DragThisServerRPC(Vector3.MoveTowards(transform.position, ScreenToWorld(Input.mousePosition, -1), 100f));
        
    }

    [ServerRpc(RequireOwnership = false)]
    public void DragThisServerRPC(Vector3 pos)
    {
        print(pos);
        transform.position = pos;
    }
}
