using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;

public class RTCCapture : MonoBehaviour
{
    private SignallingClient signallingClient;
    private string channelName;
    private string userID;
    private RenderTexture sendTexture;
    private Camera cam;
    private VideoStreamTrack track;

    private Unity.WebRTC.RTCPeerConnection peerConnection;
    private bool componentReady;

    async void Start()
    {
        var gfxType = SystemInfo.graphicsDeviceType;
        var format = WebRTC.GetSupportedRenderTextureFormat(gfxType);
        sendTexture = new RenderTexture(1920, 1080, 0, format);

        cam = gameObject.GetComponent<Camera>();

        track = new VideoStreamTrack(sendTexture);

        var connConfig = new Unity.WebRTC.RTCConfiguration();
        connConfig.iceServers = new RTCIceServer[] { new() { urls = new string[] { "stun:stun1.l.google.com:19302", "stun:stun2.l.google.com:19302" } } };

        peerConnection = new Unity.WebRTC.RTCPeerConnection(ref connConfig);

        //peerConnection.OnConnectionStateChange += e => { Debug.Log(e); };
        peerConnection.OnNegotiationNeeded = () => { Debug.Log("negotiation needed"); };
        peerConnection.OnTrack += (RTCTrackEvent e) => 
        { 
            var jsoned = JsonConvert.SerializeObject(e.Track);
            Debug.Log(jsoned);
        };
        peerConnection.OnIceCandidate = (RTCIceCandidate candidate) =>
        {
            var messageToSend = new
            {
                messageType = "candidate",
                content = new
                {
                    candidate = candidate.Candidate,
                    sdpMid = candidate.SdpMid,
                    sdpMLineIndex = candidate.SdpMLineIndex,
                }
            };

            signallingClient.SendMessage(channelName, messageToSend);
        };

        channelName = "123";
        userID = "abc";
        signallingClient = new SignallingClient(userID, ProcessMessage);

        await signallingClient.Login(userID, "def");
        await signallingClient.JoinChannel(channelName);

        componentReady = true;
        StartCoroutine(signallingClient.Update(2));
        StartCoroutine(WebRTC.Update());
        //StartCoroutine(LogStats());
    }

    public IEnumerator LogStats()
    {
        while (componentReady)
        {
            var stats = peerConnection.GetStats();
            yield return stats;

            var jsonStats = JsonConvert.SerializeObject(stats.Value.Stats);
            Debug.Log(jsonStats);

            yield return new WaitForSeconds(1f);
        }
    }

    void Update()
    {
        var temp = cam.targetTexture;
        cam.targetTexture = sendTexture;
        cam.Render();
        cam.targetTexture = temp;
    }

    public IEnumerator ProcessMessage(string message)
    {
        var messageObj = JsonConvert.DeserializeObject<JObject>(message);

        var actionType = messageObj["actionType"].ToString();
        switch (actionType)
        {
            case "user joined":
                Debug.Log("user joined");
                yield break;
            case "user left":
                Debug.Log("user left");
                yield break;
            case "message":
                break;
            default:
                Debug.Log($"unknown action type: {actionType}");
                yield break;
        }

        var messageType = messageObj["data"]["messageType"].ToString();
        var desc = new Unity.WebRTC.RTCSessionDescription();
        switch (messageType)
        {
            case "offer":
                Debug.Log("got offer");
                desc.type = Unity.WebRTC.RTCSdpType.Offer;
                desc.sdp = messageObj["data"]["content"]["sdp"].ToString();
                yield return peerConnection.SetRemoteDescription(ref desc);

                var sender = peerConnection.AddTrack(track);
                var senderParams = sender.GetParameters();
              
                var jsonedParams = JsonConvert.SerializeObject(senderParams);
                Debug.Log(jsonedParams);

                var answer = peerConnection.CreateAnswer();
                yield return answer;

                desc = answer.Desc;
                yield return peerConnection.SetLocalDescription(ref desc);

                var userID = messageObj["userID"].ToString();
                signallingClient.SetOtherUser(userID);

                var content = new { sdp = desc.sdp, type = "answer" };

                var messageToSend = new
                {
                    messageType = "answer",
                    content = content
                };
                yield return signallingClient.SendMessage(channelName, messageToSend);
                break;

            case "answer":
                Debug.Log("got answer");
                desc.type = Unity.WebRTC.RTCSdpType.Answer;
                desc.sdp = messageObj["data"]["content"]["sdp"].ToString();
                yield return peerConnection.SetRemoteDescription(ref desc);
                break;

            case "candidate":
                var iceCandidateInit = new Unity.WebRTC.RTCIceCandidateInit();
                iceCandidateInit.candidate = messageObj["data"]["content"]["candidate"].ToString();
                iceCandidateInit.sdpMid = messageObj["data"]["content"]["sdpMid"].ToString();
                iceCandidateInit.sdpMLineIndex = messageObj["data"]["content"]["sdpMLineIndex"].Value<int>();

                var iceCandidate = new Unity.WebRTC.RTCIceCandidate(iceCandidateInit);

                Debug.Log(iceCandidate);

                peerConnection.AddIceCandidate(iceCandidate);
                break;

            default:
                Debug.Log($"unknown message type: {messageType}");
                break;
        }

        Debug.Log($"message {messageObj} processed");

        yield break;
    }

    private async void OnDestroy()
    {
        componentReady = false;

        StopAllCoroutines();

        await signallingClient?.LeaveChannel(channelName);
        signallingClient.Dispose();

        peerConnection.Close();
    }
}
