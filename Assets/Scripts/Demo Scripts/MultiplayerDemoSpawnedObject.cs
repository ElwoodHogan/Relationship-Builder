using Unity.Netcode;
using UnityEngine;

namespace VRCade.Multiplayer.Demo
{
	/// <summary>
	/// A network script attached to a object spawns in runtime.
	/// </summary>
	public class MultiplayerDemoSpawnedObject : NetworkBehaviour
	{
		public static MultiplayerDemoSpawnedObject Instance;
		[SerializeField] private NetworkVariable<int> networkVariableInt;

		void Awake()
		{
			Instance = this;
		}

		/*
		public void NetworkStart()
		{
			Debug.Log("MultiplayerDemoSpawnedObject:NetworkStart");
			if (IsServer) {
				InvokeRepeating(nameof(ChangeNetworkVariableInt), 10, 30);
			}
		}*/

		void ChangeNetworkVariableInt()
		{
			networkVariableInt.Value = Random.Range(1, 999);
		}

		public void OnSyncClick()
		{
			Debug.Log("MultiplayerDemoSpawnedObject:OnSyncClick");
			if (IsServer) {
				SyncClientRpc();
			} else {
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
}
