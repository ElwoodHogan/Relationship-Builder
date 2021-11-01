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
using Netcode.Transports.SteamP2P;
using Steamworks;
using static FrontMan;

public class CanvasAI : NetworkBehaviour
{
    public Dropdown friendLobbyiesDropdown;
    CSteamID lobbyID;
    CSteamID hostSteamID;

	[SerializeField] GameObject spawnedObjectPerfab;

	Callback<LobbyEnter_t> lobbyEnter;
	Callback<LobbyCreated_t> lobbyCreated;

	List<CSteamID> lobbyIDList = new List<CSteamID>();
	List<CSteamID> friendSteamIDList = new List<CSteamID>();
	List<CSteamID> friendLobbyIDList = new List<CSteamID>();


	private void Start()
    {

		lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEntered); // Fires on entering a lobby in both host and client
		lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated); // Fires on creating a lobby in host

	}

	public void StartHost()
	{
		NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => {
			Debug.Log($"Client connected, clientId={clientId}");
		};

		NetworkManager.Singleton.OnClientDisconnectCallback += (clientId) => {
			Debug.Log($"Client disconnected, clientId={clientId}");
		};

		NetworkManager.Singleton.OnServerStarted += () => {
			Debug.Log("Server started");
			GameObject spawnedObject = GameObject.Instantiate(spawnedObjectPerfab);
			spawnedObject.GetComponent<NetworkObject>().Spawn();
		};

		NetworkManager.Singleton.StartHost();

		hostSteamID = SteamUser.GetSteamID();

		SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4);
	}

	public void StartClient()
	{
		NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
		NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

		NetworkManager.Singleton.GetComponent<SteamP2PTransport>().ConnectToSteamID = hostSteamID.m_SteamID;

		Debug.Log($"Joining room hosted by {NetworkManager.Singleton.GetComponent<SteamP2PTransport>().ConnectToSteamID}");

		//SceneManager.LoadScene("MultiplayerDemo");
		//SceneManager.sceneLoaded += (scene, mode) => {
		//	NetworkManager.Singleton.StartClient();
		//	SwitchToGameplay();
		//};
		NetworkManager.Singleton.StartClient();
	}

	void ClientConnected(ulong clientId)
	{
		Debug.Log($"I'm connected, clientId={clientId}");
	}

	void ClientDisconnected(ulong clientId)
	{
		Debug.Log($"I'm disconnected, clientId={clientId}");
		NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnected;   // remove these else they will get called multiple time if we reconnect this client again
		NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnected;
	}

	public void Disconnect()
	{
		SteamMatchmaking.LeaveLobby(lobbyID);
		SteamNetworking.CloseP2PSessionWithUser(hostSteamID);
	}

	void OnApplicationQuit()
	{
		Disconnect();
	}

	void OnLobbyCreated(LobbyCreated_t result)
	{
		if (result.m_eResult == EResult.k_EResultOK)
			Debug.Log("Lobby created successfully - LobbyID=" + result.m_ulSteamIDLobby);
		else
			Debug.Log("Lobby created failed - LobbyID=" + result.m_ulSteamIDLobby);
		lobbyID = (CSteamID)result.m_ulSteamIDLobby;
		string personalName = SteamFriends.GetPersonaName();
		SteamMatchmaking.SetLobbyData((CSteamID)result.m_ulSteamIDLobby, "name", personalName + "'s Room");
	}
	void OnLobbyEntered(LobbyEnter_t result)
	{
		lobbyID = (CSteamID)result.m_ulSteamIDLobby;
		if (result.m_EChatRoomEnterResponse == 1)
			Debug.Log($"Successfully joined lobby {SteamMatchmaking.GetLobbyData((CSteamID)result.m_ulSteamIDLobby, "name")}!");
		else
			Debug.Log("Failed to join lobby.");

		int playerCount = SteamMatchmaking.GetNumLobbyMembers((CSteamID)result.m_ulSteamIDLobby);

		// Join host's game directly
		if (playerCount > 1)
		{
			var ownerSteamID = SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)result.m_ulSteamIDLobby, 0);
			hostSteamID = ownerSteamID;
			StartClient();
		}
	}

	public void RequestFriendLobbyList()
	{
		if (SteamManager.Initialized)
		{
			int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
			Debug.Log("Found " + friendCount + " friends!");
			var friendLobbyOptions = new List<Dropdown.OptionData>();
			var inviteFriendOptions = new List<Dropdown.OptionData>();
			for (int i = 0; i < friendCount; i++)
			{
				CSteamID friendSteamID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);

				friendSteamIDList.Add(friendSteamID);
				inviteFriendOptions.Add(new Dropdown.OptionData(SteamFriends.GetFriendPersonaName(friendSteamID)));

				//Debug.Log("Friend: " + SteamFriends.GetFriendPersonaName(friendSteamID) + " - " + friendSteamID + " - Level " + SteamFriends.GetFriendSteamLevel(friendSteamID));
				if (SteamFriends.GetFriendGamePlayed(friendSteamID, out FriendGameInfo_t friendGameInfo) && friendGameInfo.m_steamIDLobby.IsValid())
				{
					// friendGameInfo.m_steamIDLobby is a valid lobby, you can join it or use RequestLobbyData() get its metadata
					//Debug.Log(SteamFriends.GetFriendPersonaName(friendSteamID) + " is hosting a lobby!");
					friendLobbyOptions.Add(new Dropdown.OptionData(SteamFriends.GetFriendPersonaName(friendSteamID) + "'s Room"));
					friendLobbyIDList.Add(friendGameInfo.m_steamIDLobby);
				}
				else
				{
					//Debug.Log(SteamFriends.GetFriendPersonaName(friendSteamID) + " is not hosting a lobby, ignore.");
				}
			}
			friendLobbyiesDropdown.AddOptions(friendLobbyOptions);
			//inviteFriendDropdown.AddOptions(inviteFriendOptions);
		}
	}

	public void JoinFriendLobby()
	{
		SteamMatchmaking.JoinLobby(friendLobbyIDList[friendLobbyiesDropdown.value]);
	}

	public void OnSyncSpawnedObjectClick()
	{
		spawnableAI.Instance.OnSyncClick();
	}
}
