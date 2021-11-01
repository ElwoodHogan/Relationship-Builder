using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Networking;
using Netcode.Transports;
using Netcode.Transports.SteamP2P;
using UnityEngine.UI;
using Steamworks;

public class MenuAI : MonoBehaviour
{
    public GameObject HostOrJoinPanel, InvitePanel;

    [SerializeField] Dropdown friendLobbyiesDropdown, inviteFriendDropdown;

    CSteamID lobbyID;
    CSteamID hostSteamID;

    Callback<LobbyCreated_t> lobbyCreated;
    Callback<LobbyMatchList_t> lobbyMatchList;
    Callback<LobbyEnter_t> lobbyEnter;
    Callback<LobbyDataUpdate_t> lobbyDataUpdate;
    Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;

    List<CSteamID> friendSteamIDList = new List<CSteamID>();
    List<CSteamID> friendLobbyIDList = new List<CSteamID>();

    private void Start()
    {
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated); // Fires on creating a lobby in host
        lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEntered); // Fires on entering a lobby in both host and client
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested); // Fires on clicking the invite popup in Steam and game is already running in client

        InvokeRepeating(nameof(RequestFriendLobbyList), 1, 1); // Keep trying for every 1s

        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            //Debug.Log("Found argument " + i + ": " + args[i]);
            if (args[i] == "+connect_lobby")
            {
                Debug.Log("Found connect lobby argument " + i + ": " + args[i]);
                lobbyID = (CSteamID)ulong.Parse(args[i + 1]);
                InvokeRepeating("WaitToJoinLobby", 1, 1); // Keep trying for every 1s
            }
        }
    }
    void WaitToJoinLobby()
    {
        if (SteamManager.Initialized)
        {
            SteamMatchmaking.JoinLobby(lobbyID);
            CancelInvoke("WaitToJoinLobby");
        }
    }
    public void Host()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => {
            Debug.Log($"Client connected, clientId={clientId}");
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += (clientId) => {
            Debug.Log($"Client disconnected, clientId={clientId}");
        };

        NetworkManager.Singleton.OnServerStarted += () => {
            Debug.Log("Server started");
        };

        NetworkManager.Singleton.StartHost();
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, 2);
        hostSteamID = SteamUser.GetSteamID();
        StwitchPanel();
    }

    public void Join()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

        NetworkManager.Singleton.GetComponent<SteamP2PTransport>().ConnectToSteamID = hostSteamID.m_SteamID;

        Debug.Log($"Joining room hosted by {NetworkManager.Singleton.GetComponent<SteamP2PTransport>().ConnectToSteamID}");
        /*
        //clicked join
        if (IpInput.text.Length <=0) NetworkManager.Singleton.GetComponent<UNetTransport>().ConnectAddress = "127.0.0.1";
        else NetworkManager.Singleton.GetComponent<UNetTransport>().ConnectAddress = IpInput.text;
        NetworkManager.Singleton.StartClient();
        gameObject.SetActive(false);*/

        StwitchPanel();
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

    /*
    void OnLobbyListObtained(LobbyMatchList_t result)
    {
        var options = new List<Dropdown.OptionData>();
        Debug.Log("Found " + result.m_nLobbiesMatching + " public lobbies!");
        for (int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            lobbyIDList.Add(lobbyId);
            SteamMatchmaking.RequestLobbyData(lobbyId);
            options.Add(new Dropdown.OptionData("Lobby a " + i));
        }
        publicLobbiesDropdown.AddOptions(options);
    }*/

    /*
    void OnLobbyDataUpdated(LobbyDataUpdate_t result)
    {
        for (int i = 0; i < lobbyIDList.Count; i++)
        {
            if (lobbyIDList[i].m_SteamID == result.m_ulSteamIDLobby)
            {
                string lobbyName = SteamMatchmaking.GetLobbyData((CSteamID)lobbyIDList[i].m_SteamID, "name");
                publicLobbiesDropdown.options[i].text = lobbyName;
                if (i == 0)
                    publicLobbiesDropdown.captionText.text = lobbyName;
                return;
            }
        }
    }*/

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
            Join();
        }
    }

    void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t pCallback)
    {
        Debug.Log("[" + GameLobbyJoinRequested_t.k_iCallback + " - GameLobbyJoinRequested] - " + pCallback.m_steamIDLobby + " -- " + pCallback.m_steamIDFriend);
        SteamMatchmaking.JoinLobby(pCallback.m_steamIDLobby);
    }

    void StwitchPanel()
    {
        HostOrJoinPanel.SetActive(false);
        InvitePanel.SetActive(true);
    }

    void RequestFriendLobbyList()
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

                Debug.Log("Friend: " + SteamFriends.GetFriendPersonaName(friendSteamID) + " - " + friendSteamID + " - Level " + SteamFriends.GetFriendSteamLevel(friendSteamID));
                if (SteamFriends.GetFriendGamePlayed(friendSteamID, out FriendGameInfo_t friendGameInfo) && friendGameInfo.m_steamIDLobby.IsValid())
                {
                    // friendGameInfo.m_steamIDLobby is a valid lobby, you can join it or use RequestLobbyData() get its metadata
                    Debug.Log(SteamFriends.GetFriendPersonaName(friendSteamID) + " is hosting a lobby!");
                    friendLobbyOptions.Add(new Dropdown.OptionData(SteamFriends.GetFriendPersonaName(friendSteamID) + "'s Room"));
                    friendLobbyIDList.Add(friendGameInfo.m_steamIDLobby);
                }
                else
                {
                    Debug.Log(SteamFriends.GetFriendPersonaName(friendSteamID) + " is not hosting a lobby, ignore.");
                }
            }
            friendLobbyiesDropdown.AddOptions(friendLobbyOptions);
            inviteFriendDropdown.AddOptions(inviteFriendOptions);

            CancelInvoke("RequestFriendLobbyList");
        }
    }

    public void InviteFriendToLobby()
    {
        var friendSteamID = friendSteamIDList[inviteFriendDropdown.value];

        Debug.Log("Inviting " + SteamFriends.GetFriendPersonaName(friendSteamID) + " (" + friendSteamID + ")");
        //bool success = SteamFriends.InviteUserToGame(friendSteamID, "Hey, join me playing game togather!");
        bool success = SteamMatchmaking.InviteUserToLobby(lobbyID, friendSteamID);
        if (success)
            Debug.Log("Successfully invite " + SteamFriends.GetFriendPersonaName(friendSteamID));
        else
            Debug.Log("Failed to invite " + SteamFriends.GetFriendPersonaName(friendSteamID));
    }
}
