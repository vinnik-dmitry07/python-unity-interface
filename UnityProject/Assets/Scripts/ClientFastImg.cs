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
        receiveThread = new Thread(Run);
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

    void Run(object callback)
    {
        using (var socket = new PullSocket())
        {
            socket.Connect("tcp://localhost:5555");

            byte[] rawImage;
            while (running)
            {
                bool success = socket.TryReceiveFrameBytes(out rawImage);
                if (success) ((Action<byte[]>)callback)(rawImage);
            }
        }
        NetMQConfig.Cleanup();
    }
}

public class ClientFastImg : MonoBehaviour
{
    public readonly ConcurrentQueue<Action> RunOnMainThread = new ConcurrentQueue<Action>();
    private ReceiverFastImg receiver;
    private Texture2D tex;
    public RawImage image;

    public void Start()
    {
        tex = new Texture2D(960, 720, TextureFormat.RGB24, mipChain: false);
        image.texture = tex;

        receiver = new ReceiverFastImg();
        receiver.Start((byte[] rawImage) => RunOnMainThread.Enqueue(() =>
            {
                tex.LoadRawTextureData(rawImage);
                tex.Apply(updateMipmaps: false);
            }
        ));
    }

    public void Update()
    {
        if (!RunOnMainThread.IsEmpty)
        {
            Action action;
            while (RunOnMainThread.TryDequeue(out action))
            {
                action.Invoke();
            }
        }
    }

    private void OnDestroy()
    {
        receiver.Stop();
    }
}