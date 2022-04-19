using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;

public class TestScript : MonoBehaviour
{
    [SerializeField] private RawImage recvImage;
    [SerializeField] private RawImage renderImage;

    RenderTexture renderTexture;
    RTCPeerConnection sender;
    RTCPeerConnection receiver;

    private void Start()
    {
        WebRTC.Initialize();

        renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
        renderTexture.Create();

        renderImage.texture = renderTexture;

        var webcamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 640, 480, 30);
        webcamTexture.Play();

        var track = new VideoStreamTrack(webcamTexture);
        var recvStrm = new MediaStream();
        recvStrm.OnAddTrack += (e) =>
        {
            if (e.Track is VideoStreamTrack videoTrack)
            {
                videoTrack.OnVideoReceived += tex => recvImage.texture = tex;
            }
        };

        RTCConfiguration conf = default;
        sender = new RTCPeerConnection(ref conf)
        {
            OnIceCandidate = (candidate) => receiver.AddIceCandidate(candidate),
            OnNegotiationNeeded = () => StartCoroutine(Negotiation())
        };
        sender.AddTrack(track);

        receiver = new RTCPeerConnection(ref conf)
        {
            OnIceCandidate = (candidate) => sender.AddIceCandidate(candidate),
            OnTrack = (e) => recvStrm.AddTrack(e.Track)
        };
        receiver.AddTransceiver(TrackKind.Video).Direction = RTCRtpTransceiverDirection.RecvOnly;

        StartCoroutine(WebRTC.Update());
        StartCoroutine(Screenshot());
    }

    private void OnDestroy()
    {
        if (renderTexture.IsCreated())
        {
            DestroyImmediate(renderTexture);
        }
        WebRTC.Dispose();
    }

    IEnumerator Negotiation()
    {
        var offerSdp = sender.CreateOffer();
        yield return offerSdp;

        var desc = offerSdp.Desc;
        yield return sender.SetLocalDescription(ref desc);
        yield return receiver.SetRemoteDescription(ref desc);

        var answerSdp = receiver.CreateAnswer();
        yield return answerSdp;

        desc = answerSdp.Desc;
        yield return receiver.SetLocalDescription(ref desc);
        yield return sender.SetRemoteDescription(ref desc);
    }

    IEnumerator Screenshot()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshotIntoRenderTexture(renderTexture);
        }
    }
}
