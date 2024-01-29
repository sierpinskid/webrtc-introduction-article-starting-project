namespace WebRTCTutorial.DTO
{
    /// <summary>
    /// DTO (Data Transfer Object) to send/receive ICE Candidate through the network. This DTO maps to <see cref="RTCIceCandidate"/>
    /// </summary>
    public class ICECanddidateDTO : UnityJsonObjectBase<ICECanddidateDTO>
    {
        public string Candidate { get; set; }
        public string SdpMid { get; set; }
        public int? SdpMLineIndex { get; set; }
    }
}