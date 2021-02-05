using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ReceiverFastImg
{
    private readonly Thread receiveThread;
    private bool running;

    public ReceiverFastImg()
    {
        receiveThread = new Thread((object callback) =>
        {
            using (var socket = new PullSocket())
            {
                socket.Connect("tcp://localhost:5555");

                while (running)
                {
                    byte[] rawImage = socket.ReceiveFrameBytes();
                    ((Action<byte[]>)callback)(rawImage);
                }
            }
        });
    }

    public void Start(Action<byte[]> callback)
    {
        running = true;
        receiveThread.Start(callback);
    }

    public void Stop()
    {
        running = false;
        receiveThread.Join();
    }
}

public class ClientFastImg : MonoBehaviour
{
    private readonly ConcurrentQueue<Action> runOnMainThread = new ConcurrentQueue<Action>();
    private ReceiverFastImg receiver;
    private Texture2D tex;
    public RawImage image;

    public void Start()
    {
        tex = new Texture2D(960, 720, TextureFormat.RGB24, mipChain: false);
        image.texture = tex;

        ForceDotNet.Force();
        receiver = new ReceiverFastImg();
        receiver.Start((byte[] rawImage) => runOnMainThread.Enqueue(() =>
            {
                tex.LoadRawTextureData(rawImage);
                tex.Apply(updateMipmaps: false);
            }
        ));
    }

    public void Update()
    {
        if (!runOnMainThread.IsEmpty)
        {
            Action action;
            while (runOnMainThread.TryDequeue(out action))
            {
                action.Invoke();
            }
        }
    }

    private void OnDestroy()
    {
        receiver.Stop();
        NetMQConfig.Cleanup();
    }
}