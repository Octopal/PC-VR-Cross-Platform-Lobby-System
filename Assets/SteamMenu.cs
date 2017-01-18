﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using TMPro;

public class SteamMenu : MonoBehaviour {

    private Callback<LobbyCreated_t> Callback_lobbyCreated;
    private Callback<LobbyMatchList_t> Callback_lobbyList;
    private Callback<LobbyEnter_t> Callback_lobbyEnter;
    private Callback<LobbyDataUpdate_t> Callback_lobbyInfo;

    ulong current_lobbyID;
    List<CSteamID> lobbyIDS;

    [Header("Lobby Menu")]
    public TextMeshProUGUI awaitMsg;
    public TextMeshProUGUI lobbyHeader;
    public Button lobbyBack;

    [Header("Main Menu")]
    public TextMeshProUGUI gameTitle;
	public Button createLobby;
	public Button viewLobbies;
	public Button quit;

	[Header("UI Components")]
    public GameObject lobby;
    public GameObject lobbyJoin;
	public GameObject user;
    public List<GameObject> users;
    public List<GameObject> lobbies;
    

	TextMeshProUGUI userText;
	Image userImage;
	int userInt;
	uint width, height;
	Texture2D downloadedAvatar;
	Rect rect = new Rect(0, 0, 184, 184);
	Vector2 pivot = new Vector2(0.5f, 0.5f);

	void Start(){
		lobbyIDS = new List<CSteamID>();
        Callback_lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        Callback_lobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbiesList);
        Callback_lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        Callback_lobbyInfo = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyInfo);

        if (SteamAPI.Init())
            Debug.Log("Steam API init -- SUCCESS!");
        else
            Debug.Log("Steam API init -- failure ...");

		createLobby.onClick.AddListener(CreateLobby);
		viewLobbies.onClick.AddListener(ViewLobbies);
		quit.onClick.AddListener(Quit);
        lobbyBack.onClick.AddListener(Back);
	}

		IEnumerator Fetchuser(CSteamID id){
		GameObject userClone = Instantiate(user, Vector3.zero, user.transform.rotation);
		userClone.transform.SetParent(this.transform.parent);
		userText = userClone.GetComponentInChildren<TextMeshProUGUI>();
		userImage = userClone.GetComponent<Image>();
		userInt = SteamFriends.GetLargeFriendAvatar(id);
		userText.SetText(SteamFriends.GetFriendPersonaName(id));
		while(userInt == -1){
			yield return null;
		}
		if(userInt > 0){
			SteamUtils.GetImageSize(userInt, out width, out height);
		}
		if(width > 0 && height > 0){
			byte[] avatarStream = new byte[4 * (int)width * (int)height];
			SteamUtils.GetImageRGBA(userInt, avatarStream, 4 * (int)width * (int)height);
			downloadedAvatar = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
			downloadedAvatar.LoadRawTextureData(avatarStream);
			downloadedAvatar.Apply();

			userImage.sprite = Sprite.Create(downloadedAvatar, rect, pivot);
		}
        
     users.Add(userClone);
	}

	void CreateLobby(){
        ToggleLobby();        
        ToggleMain();
		ToggleAwaitCallbackMsg("creating lobby...");
		SteamAPICall_t try_toHost = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 4);
	}

	void ViewLobbies(){
		ToggleAwaitCallbackMsg("finding available lobbies...");
        ToggleLobby();        
        ToggleMain();
		SteamAPICall_t try_getList = SteamMatchmaking.RequestLobbyList();
	}

	void Quit(){
		Application.Quit();
	}

    void Back(){
        SteamMatchmaking.LeaveLobby((CSteamID)current_lobbyID);
        foreach(GameObject user in users)
        Destroy(user);
        users.Clear();
        foreach(GameObject lobby in lobbies)
        Destroy(lobby);
        lobbies.Clear();
        ToggleLobby();        
        ToggleMain();
    }

    void RefreshUsers(){
        int newPos = 0;
        foreach(GameObject user in users){
        user.transform.localPosition = new Vector3(-200,90+newPos,0);
        newPos -= 75;
        }
    }

    void RefreshLobbies(){
        for(int i = 0; i < lobbyIDS.Count; i++){
            Debug.Log("Lobby " + " :: " + SteamMatchmaking.GetLobbyData(lobbyIDS[i], "name"));
            int newPos = 0;
            lobbies.Add(Instantiate(lobby, Vector3.zero, lobby.transform.rotation));
            TextMeshProUGUI[] texts = lobbies[i].GetComponentsInChildren<TextMeshProUGUI>();
                
            foreach(TextMeshProUGUI textMesh in texts)
            {
                if(textMesh.gameObject.transform.parent != null)
                    textMesh.text = SteamMatchmaking.GetNumLobbyMembers(lobbyIDS[i]).ToString() + "/4";
                else
                    textMesh.text = SteamMatchmaking.GetLobbyData(lobbyIDS[i], "name");
                
            }

            GameObject joinButton = Instantiate(lobbyJoin, Vector3.zero, lobbyJoin.transform.rotation);
            lobbies[i].transform.SetParent(this.transform.parent);
            joinButton.transform.SetParent(lobbies[i].transform);
            lobbies[i].transform.localPosition = new Vector3(-70,90+newPos,0);
            joinButton.transform.localPosition = new Vector3(470,0,0);
            joinButton.GetComponent<JoinLobbyButton>().joinID = lobbyIDS[i];
        }
    }

    void ToggleLobby(){
        lobbyHeader.gameObject.SetActive(!lobbyHeader.gameObject.activeSelf);
        lobbyBack.gameObject.SetActive(!lobbyBack.gameObject.activeSelf);
    }

    void ToggleMain(){
        gameTitle.gameObject.SetActive(!gameTitle.gameObject.activeSelf);
        createLobby.gameObject.SetActive(!createLobby.gameObject.activeSelf);
        viewLobbies.gameObject.SetActive(!viewLobbies.gameObject.activeSelf);
        quit.gameObject.SetActive(!quit.gameObject.activeSelf);
    }

    void ToggleAwaitCallbackMsg(string msg = ""){
        awaitMsg.text = msg;
        awaitMsg.gameObject.SetActive(!awaitMsg.gameObject.activeSelf);
    }

	void Update()
	{
        SteamAPI.RunCallbacks();
        // Command - List lobby members
        if (Input.GetKeyDown(KeyCode.Q))
        {
            int numPlayers = SteamMatchmaking.GetNumLobbyMembers((CSteamID)current_lobbyID);

            Debug.Log("\t Number of players currently in lobby : " + numPlayers);
            for (int i = 0; i < numPlayers; i++)
            {
                Debug.Log("\t Player(" + i + ") == " + SteamFriends.GetFriendPersonaName(SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)current_lobbyID, i)));
                Debug.Log("\t Player(" + i + ") == " + SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)current_lobbyID, i));
            }
        }
    }

    void OnLobbyCreated(LobbyCreated_t result)
    {
        ToggleAwaitCallbackMsg();
        if (result.m_eResult == EResult.k_EResultOK)
            Debug.Log("Lobby created -- SUCCESS!");
        else{
            Debug.Log("Lobby created -- failure ...");
            return;
        }
        string gameName = SteamFriends.GetPersonaName() + "'s game";
        SteamMatchmaking.SetLobbyData((CSteamID)result.m_ulSteamIDLobby, "name", gameName);
    }

    void OnGetLobbiesList(LobbyMatchList_t result)
    {
        ToggleAwaitCallbackMsg();
        lobbyIDS.Clear();
        for(int i=0; i< result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            lobbyIDS.Add(lobbyID);
        }
            lobbyHeader.text = "List of Lobbies";
            RefreshLobbies();
    }

    void OnGetLobbyInfo(LobbyDataUpdate_t result)
    {
        for(int i=0; i<lobbyIDS.Count; i++)
        {
            if (lobbyIDS[i].m_SteamID == result.m_ulSteamIDLobby)
            {
                Debug.Log("Lobby " + i+" :: " +SteamMatchmaking.GetLobbyData((CSteamID)lobbyIDS[i].m_SteamID, "name"));
                return;
            }
        }
       
    }

    void OnLobbyEntered(LobbyEnter_t result)
    {
        foreach(GameObject lobby in lobbies)
            Destroy(lobby);
        lobbies.Clear();
        current_lobbyID = result.m_ulSteamIDLobby;
        lobbyHeader.text = SteamMatchmaking.GetLobbyData((CSteamID)current_lobbyID, "name");
        if (result.m_EChatRoomEnterResponse == 1){
            SteamMenu steamMenu = GetComponent<SteamMenu>();
            int numPlayers = SteamMatchmaking.GetNumLobbyMembers((CSteamID)current_lobbyID);
            for (int i = 0; i < numPlayers; i++)
            {   
                StartCoroutine(Fetchuser(SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)current_lobbyID, i)));
            }
        RefreshUsers();
        }
        else
            Debug.Log("Failed to join lobby.");
    }
}
