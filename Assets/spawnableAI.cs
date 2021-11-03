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

public class spawnableAI : NetworkBehaviour
{
	public static spawnableAI Instance;
	[SerializeField] private NetworkVariable<int> networkVariableInt;

	void Awake()
	{
		Instance = this;
		//print("doned");
	}

	void ChangeNetworkVariableInt()
	{
		networkVariableInt.Value = UnityEngine.Random.Range(1, 999);
	}

	public void OnSyncClick()
	{
		Debug.Log("MultiplayerDemoSpawnedObject:OnSyncClick");
		if (IsServer)
		{
			SyncClientRpc();
		}
		else
		{
			SyncServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	void SyncServerRpc()
	{
		Debug.Log("MultiplayerDemoSpawnedObject:SyncServerRpc");
	}

	[ClientRpc]
	void SyncClientRpc()
	{
		Debug.Log("MultiplayerDemoSpawnedObject:SyncClientRpc");
	}
}
