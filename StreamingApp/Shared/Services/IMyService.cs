using System;
using MagicOnion;
using MessagePack;
using System.Numerics;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace StreamingApp.Shared.Services
{
    public interface IMyService : IService<IMyService>
    {
        UnaryResult<string> ReturnConnection();
    }

    [MessagePackObject]
    public class PenLight
    {
        [Key(0)]
        public string PenLightId { get; set; }
        [Key(1)]
        public UnityEngine.Vector3 Position { get; set; }
        [Key(2)] 
        public UnityEngine.Quaternion Rotation { get; set; }
        [Key(3)] 
        public bool Trail { get; set; }
        [Key(4)]
        public bool Handle { get; set; }
    }
    [MessagePackObject]
    public class Player
    {
        [Key(5)]
        public string PlayerId { get; set; }
        [Key(6)]
        public UnityEngine.Vector3 Position { get; set; }
        [Key(7)]
        public UnityEngine.Quaternion Rotation { get; set; }
        [Key(8)]
        public string Auth { get; set; }
    }
    [MessagePackObject]
    public class User
    {
        [Key(9)]
        public Player Player { get; set; }
        [Key(10)]
        public PenLight PenLightL { get; set; }
        [Key(11)]
        public PenLight PenLightR { get; set; }
    } 

    //server -> client
    public interface IGamingHubReceiver
    {
        void OnJoin(Player player);
        void OnLeave(Player player);
        void OnMove(Player player);
        //about PenLight
        void OnCreatePenLight(PenLight penLight);
        void OnMovePenLight(PenLight penLight);
        void OnDeletePenLight(PenLight penLight);

        void OnPenLightStatus(string playerId, bool color, bool trail);
        //Staging
        void OnLiveStart(float timeM, float timeS);
        void OnStageScore(float score);
    }

    //client -> server
    public interface IGamingHub : IStreamingHub<IGamingHub, IGamingHubReceiver>
    {
        Task<User[]> JoinAsync(string roomName, string playerId, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation);
        Task LeaveAsync();
        Task MoveAsync(UnityEngine.Vector3 position, UnityEngine.Quaternion rotation);
        //about PenLight
        Task CreatePenLightAsync(PenLight penLight);
        Task DeletePenLightAsync(PenLight penLight);
        Task MovePenLightAsync(PenLight penLight);
        Task PenLightStatusAsync(bool color, bool trail);
        //Staging
        Task LiveStartAsync();
        Task StageScoreAsync(float score);
    }
}
