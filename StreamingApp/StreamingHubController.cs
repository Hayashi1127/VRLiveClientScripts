using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using JetBrains.Annotations;
using MagicOnion.Client;
using Newtonsoft.Json.Serialization;
using StreamingApp.Shared.Services;
using UnityEngine;
using Unity.VisualScripting;
//using UnityEditor.PackageManager;

namespace StreamingApp
{
    public class StreamingHubController : MonoBehaviour, IGamingHubReceiver
    {
        public GameObject _Audience;
        public GameObject _PenLight;
        public WorldTimer _WorldTimer;
        public calcStageScore _calcStageScore;

        private Channel _channel;
        //private IMyService _service;
        private bool _connectFlag;
        private Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> penLights = new Dictionary<string, GameObject>();
        private IGamingHub _client = null;

        [SerializeField] GameObject _origin_self;
        private Player _self;
        private PenLight _penLightL=null;
        private PenLight _penLightR=null;

        //update

        public void Update()
        {
            if (_connectFlag && _origin_self.transform.hasChanged)
            {
                MoveAsync(_origin_self.transform.position, _origin_self.transform.rotation);
                _origin_self.transform.hasChanged = false;
            }
        }
        //getter
        public string getAuth()
        {
            if (_self != null)
            { 
                return _self.Auth;
            }
            return "user";
        }
        // create Connection
        public async void Connect()
        {
            //create random player name
            var characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var charsarr = new char[8];
            var random = new System.Random();
            for (int i = 0; i < charsarr.Length; i++)
            {
                charsarr[i] = characters[random.Next(characters.Length)];
            }
            string id = new String(charsarr);
            _channel = new Channel("localhost", 5000, ChannelCredentials.Insecure);

            //string id = "uouo";
            await ConnectAsync(id, _origin_self.transform.position, _origin_self.transform.rotation);
        }
        private async ValueTask ConnectAsync(string id, Vector3 position, Quaternion rotation)
        {
            //_service = MagicOnionClient.Create<IMyService>(_channel);
            this._client = await StreamingHubClient.ConnectAsync<IGamingHub, IGamingHubReceiver>(_channel, this);
            var authFlag = await JoinAsync("testRoom", id);
            string auth = "audience";
            if (authFlag)
            {
                auth = "admin";
            }

            _self = new Player()
            {
                PlayerId = id,
                Position = position,
                Rotation = rotation,
                Auth = auth
            };
            _connectFlag = true;
        }
        async void OnDestroy()
        {
            if (_channel != null)
            {
                await _channel.ShutdownAsync();
            }
        }
        
        //client -> server
        private async Task<bool> JoinAsync(string roomName, string playerName)
        {
            //memo: add connection fault process
            var roomUsers = await _client.JoinAsync(roomName, playerName, Vector3.zero, Quaternion.identity);
            if (roomUsers.Length == 1)
            {
                return true;
            }
            foreach (var user in roomUsers)
            {
                //Debug.Log(user.PlayerId); 
                (this as IGamingHubReceiver).OnJoin(user.Player);
                    if (user.PenLightL != null)
                    {
                        (this as IGamingHubReceiver).OnCreatePenLight(user.PenLightL);
                    }
                    else if (user.PenLightR != null)
                    {
                        (this as IGamingHubReceiver).OnCreatePenLight(user.PenLightR);
                    }
            }
            return false;
        }
        public void Leave()
        {
            _client.LeaveAsync();
            _client.DisposeAsync();
            OnDestroy();
        }

        public Task MoveAsync(UnityEngine.Vector3 position, Quaternion rotation)
        {
            return _client.MoveAsync(position, rotation);
        }

        //create MessagePackObject PenLight
        public void m_CreatePenLight(Vector3 position, Quaternion rotation, bool handle)
        {
            if (handle)
            {
                _penLightL = new PenLight()
                {
                    PenLightId = _self.PlayerId + "L",
                    Position = position,
                    Rotation = rotation,
                    Trail = true,
                    Handle = true,
                };
                _client.CreatePenLightAsync(_penLightL);
            }
            else
            {
                _penLightR = new PenLight()
                {  
                    PenLightId = _self.PlayerId + "R",
                    Position = position,
                    Rotation = rotation,
                    Trail = true,
                    Handle = false,
                };
                _client.CreatePenLightAsync(_penLightR);
            }
        }

        public void DeletePenLight(bool handle)
        {
            if (handle)
            {
                _client.DeletePenLightAsync(_penLightL);
            }
            else
            {
                _client.DeletePenLightAsync(_penLightR);
            }
        }

        public void MovePenLight(Vector3 position, Quaternion rotation, bool handle)
        {
            if (handle)
            {
                _penLightL.Position = position;
                _penLightR.Rotation = rotation;
                _client.MovePenLightAsync(_penLightL);
            }
            else
            {
                _penLightL.Position = position;
                _penLightR.Rotation = rotation;
                _client.MovePenLightAsync(_penLightR);
            }
            Debug.Log("move penLight");
        }

        public void PenLightStatusAsync(bool color, bool trail)
        {
            _client.PenLightStatusAsync(color, trail);
        }

        public void LiveStart()
        {
            _client.LiveStartAsync();
        }

        public Task StageScoreAsync(float score)
        {
            return _client.StageScoreAsync(score);
        }

        //server -> client
        void IGamingHubReceiver.OnJoin(Player player)
        {
            if (player.PlayerId != _self.PlayerId)
            {
                GameObject newCommer = Instantiate(_Audience, player.Position, player.Rotation);
                //memo: add avoiding same name process
                players.TryAdd(player.PlayerId, newCommer);
            }

            Debug.Log("Join Player:" + player.PlayerId + " Auth:" + player.Auth);
        }

        void IGamingHubReceiver.OnLeave(Player player)
        {
            Debug.Log("Leave Player:" + player.PlayerId + " Auth:" + player.Auth);
            if (player.PlayerId == "admin")
            {
                Debug.Log("Leave admin, Please Leave All User");
            }
            if (players.TryGetValue(player.PlayerId, out GameObject audience))
            {
                players.Remove(player.PlayerId);
                GameObject.Destroy(audience);
            }
        }

        void IGamingHubReceiver.OnMove(Player player)
        {
            if (players.TryGetValue(player.PlayerId, out GameObject audience))
            {
                audience.transform.SetPositionAndRotation(player.Position, player.Rotation);
            }
        }

        //about PenLight
        void IGamingHubReceiver.OnCreatePenLight(PenLight penLight)
        {
            GameObject newPenLight = Instantiate(_PenLight, penLight.Position, penLight.Rotation);
            penLights.TryAdd(penLight.PenLightId, newPenLight);
        }

        void IGamingHubReceiver.OnMovePenLight(PenLight penLight)
        {
            if (penLights.TryGetValue(penLight.PenLightId, out GameObject pLight))
            {
                pLight.transform.SetPositionAndRotation(penLight.Position, penLight.Rotation);
            }
        }

        void IGamingHubReceiver.OnDeletePenLight(PenLight penLight)
        {
            if (players.TryGetValue(penLight.PenLightId, out GameObject pLight))
            {
                players.Remove(penLight.PenLightId);
                GameObject.Destroy(pLight);
            }
        }

        void IGamingHubReceiver.OnPenLightStatus(string playerId, bool color, bool trail)
        {
            
        }
        void IGamingHubReceiver.OnLiveStart(float timeM, float timeS)
        {
            Debug.Log("on live start");
            _WorldTimer.setLiveStart(timeM, timeS);
        }

        void IGamingHubReceiver.OnStageScore(float score)
        {
            _calcStageScore.addStageScore(score/(players.Count+1));
        }
    }
}