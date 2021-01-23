using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class Receiver
{
    private readonly Thread receiveThread;
    private bool running;

    public Receiver()
    {
        receiveThread = new Thread((object callback) => {
            using (var socket = new RequestSocket())
            {
                socket.Connect("tcp://localhost:5555");

                while (running)
                {
                    socket.SendFrameEmpty();
                    string message = socket.ReceiveFrameString();
                    Data data = JsonUtility.FromJson<Data>(message);
                    ((Action<Data>)callback)(data);
                }
            }
            NetMQConfig.Cleanup();
        });
    }

    public void Start(Action<Data> callback)
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

public class Client : MonoBehaviour
{
    private readonly ConcurrentQueue<Action> RunOnMainThread = new ConcurrentQueue<Action>();
    private Receiver receiver;
    private Texture2D tex;
    public RawImage image;

    public void Start()
    {
        tex = new Texture2D(2, 2, TextureFormat.RGB24, mipChain: false);
        image.texture = tex;

        receiver = new Receiver();
        receiver.Start((Data d) => RunOnMainThread.Enqueue(() =>
            {
                Debug.Log(d.str);
                tex.LoadImage(d.image);
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