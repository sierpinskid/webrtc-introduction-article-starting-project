using Unity.WebRTC;

namespace WebRTCTutorial.DTO
{
    /// <summary>
    /// DTO (Data Transfer Object) to send/receive SDP Offer or Answer through the network. This DTO maps to <see cref="RTCSessionDescription"/>
    /// </summary>
    public class SdpDTO : UnityJsonObjectBase<SdpDTO>
    {
        public RTCSdpType Type { get; set; }
        public string Sdp { get; set; }
    }
}