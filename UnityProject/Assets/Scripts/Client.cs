using AsyncIO;
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
        receiveThread = new Thread((object callback) => 
        {
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
    private readonly ConcurrentQueue<Action> runOnMainThread = new ConcurrentQueue<Action>();
    private Receiver receiver;
    private Texture2D tex;
    public RawImage image;

    public void Start()
    {
        tex = new Texture2D(2, 2, TextureFormat.RGB24, mipChain: false);
        image.texture = tex;

        ForceDotNet.Force();  // If you have multiple sockets in the following threads
        receiver = new Receiver();
        receiver.Start((Data d) => runOnMainThread.Enqueue(() =>
            {
                Debug.Log(d.str);
                tex.LoadImage(d.image);
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