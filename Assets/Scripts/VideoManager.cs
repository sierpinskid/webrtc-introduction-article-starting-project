using System;
using System.Collections;
using System.Threading;
using Unity.WebRTC;
using UnityEngine;
using WebRTCTutorial.DTO;

namespace WebRTCTutorial
{
    public class VideoManager : MonoBehaviour
    {
        public event Action<Texture> RemoteVideoReceived;

        public void SetActiveCamera(WebCamTexture activeWebCamTexture)
        {
            var videoTrack = new VideoStreamTrack(activeWebCamTexture);
            _peerConnection.AddTrack(videoTrack);
        }
        
        protected void Awake()
        {
            StartCoroutine(WebRTC.Update());

            var config = new RTCConfiguration
            {
                iceServers = new RTCIceServer[]
                {
                    new RTCIceServer
                    {
                        urls = new string[]
                        {
                            // Google Stun server
                            "stun:stun.l.google.com:19302"
                        },
                    }
                },
            };

            _peerConnection = new RTCPeerConnection(ref config);
            
            // "Negotiation" is the exchange of SDP Offer/Answer. Peers describe what media they want to send and agree on, for example, what codecs to use 
            _peerConnection.OnNegotiationNeeded += OnNegotiationNeeded;
            
            // Triggered when a new network endpoint is found that could potentially be used to establish the connection
            _peerConnection.OnIceCandidate += OnIceCandidate;
            
            // Triggered when a new track is received
            _peerConnection.OnTrack += OnTrack;
            
            // Triggered when a new message is received from the other peer via WebSocket
            _webSocketClient.MessageReceived += OnWebSocketMessageReceived;
            
            Debug.Log("MAIN THREAD  " + Thread.CurrentThread.ManagedThreadId);
        }
        
        private VideoStreamTrack _videoTrack;

        private void OnTrack(RTCTrackEvent trackEvent)
        {
            //Debug.Log("OnTrack");
            Debug.Log("OnTrack THREAD  " + Thread.CurrentThread.ManagedThreadId);

            if (trackEvent.Track is VideoStreamTrack videoStreamTrack)
            {
                _videoTrack.OnVideoReceived += OnVideoReceived;
            }
            else
            {
                Debug.LogError($"Unhandled track of type: {trackEvent.Track.GetType()}. In this tutorial, we're handling only video tracks.");
            }
        }

        private void OnVideoReceived(Texture texture)
        {
            RemoteVideoReceived?.Invoke(texture);
        }

        private void OnNegotiationNeeded()
        {
            //Debug.Log("OnNegotiationNeeded");
            Debug.Log("OnNegotiationNeeded THREAD  " + Thread.CurrentThread.ManagedThreadId);
        }

        private void OnIceCandidate(RTCIceCandidate candidate)
        {
            //Debug.Log("OnIceCandidate");
            Debug.Log("OnIceCandidate THREAD  " + Thread.CurrentThread.ManagedThreadId);
            SendIceCandidateToOtherPeer(candidate);
        }

        [SerializeField]
        private WebSocketClient _webSocketClient;

        private RTCPeerConnection _peerConnection;

        private void OnWebSocketMessageReceived(string message)
        {
            var dtoWrapper = JsonUtility.FromJson<DTOWrapper>(message);
            switch (dtoWrapper.Type)
            {
                case DtoType.ICE:

                    var iceDto = JsonUtility.FromJson<ICECanddidateDTO>(message);
                    var ice = new RTCIceCandidate(new RTCIceCandidateInit
                    {
                        candidate = iceDto.Candidate,
                        sdpMid = iceDto.SdpMid,
                        sdpMLineIndex = iceDto.SdpMLineIndex
                    });
                    
                    _peerConnection.AddIceCandidate(ice);
                    Debug.Log($"Received ICE Candidate: {ice.Candidate}");
                    
                    break;
                case DtoType.SDP:

                    var sdpDto = JsonUtility.FromJson<SdpDTO>(message);
                    var sdp = new RTCSessionDescription
                    {
                        type = sdpDto.Type,
                        sdp = sdpDto.Sdp
                    };
                    
                    Debug.Log($"Received SDP offer of type: {sdp.type} and SDP details: {sdp.sdp}");

                    switch (sdp.type)
                    {
                        case RTCSdpType.Offer:
                            StartCoroutine(OnRemoteSdpOfferReceived(sdp));
                            break;
                        case RTCSdpType.Answer:
                            StartCoroutine(OnRemoteSdpAnswerReceived(sdp));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("Unhandled type of SDP message: " + sdp.type);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SendIceCandidateToOtherPeer(RTCIceCandidate iceCandidate)
        {
            var iceDto = new ICECanddidateDTO
            {
                Candidate = iceCandidate.Candidate,
                SdpMid = iceCandidate.SdpMid,
                SdpMLineIndex = iceCandidate.SdpMLineIndex
            };

            SendMessageToOtherPeer(iceDto, DtoType.ICE);
        }
        
        private void SendSdpToOtherPeer(RTCSessionDescription sdp)
        {
            var sdpDto = new SdpDTO
            {
                Type = sdp.type,
                Sdp = sdp.sdp
            };

            SendMessageToOtherPeer(sdpDto, DtoType.SDP);
        }

        private void SendMessageToOtherPeer<TType>(TType obj, DtoType type) 
            where TType : IJsonObject<TType>
        {
            var serializedPayload = JsonUtility.ToJson(obj);
            
            var dtoWrapper = new DTOWrapper
            {
                Type = type,
                Payload = serializedPayload
            };

            var serializedDto = JsonUtility.ToJson(dtoWrapper);
            
            _webSocketClient.SendWebSocketMessage(serializedDto);
        }

        private IEnumerator OnRemoteSdpOfferReceived(RTCSessionDescription remoteSdpOffer)
        {
            Debug.Log("Remote SDP Offer received. Set as local offer and send back the generated answer");
            
            // 1. Set the received offer as remote description
            var setRemoteSdpOperation = _peerConnection.SetRemoteDescription(ref remoteSdpOffer);
            yield return setRemoteSdpOperation;

            if (setRemoteSdpOperation.IsError)
            {
                Debug.LogError("Failed to set remote description");
                yield break;
            }
            
            // 2. Generate Answer
            var createAnswerOperation = _peerConnection.CreateAnswer();
            yield return createAnswerOperation;
            
            if (createAnswerOperation.IsError)
            {
                Debug.LogError("Failed to create answer");
                yield break;
            }

            var sdpAnswer = createAnswerOperation.Desc;
            
            // 3. Set the generated answer as local description

            var setLocalDspOperation = _peerConnection.SetLocalDescription(ref sdpAnswer);
            yield return setLocalDspOperation;

            if (setLocalDspOperation.IsError)
            {
                Debug.LogError("Failed to set local description");
                yield break;
            }
            
            // 4. Send the answer to the other peer
            SendSdpToOtherPeer(sdpAnswer);
            Debug.Log("Sent Sdp Answer");
        }
        
        private IEnumerator OnRemoteSdpAnswerReceived(RTCSessionDescription remoteSdpAnswer)
        {
            // 1. Set the received answer as remote description
            var setRemoteSdpOperation = _peerConnection.SetRemoteDescription(ref remoteSdpAnswer);
            yield return setRemoteSdpOperation;

            if (setRemoteSdpOperation.IsError)
            {
                Debug.LogError("Failed to set remote description");
                yield break;
            }
        }
    }
}