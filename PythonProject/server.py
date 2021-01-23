# pip install pyzmq opencv-python

import cv2
import zmq

context = zmq.Context()
socket = context.socket(zmq.REP)
socket.bind('tcp://*:5555')

capture = cv2.VideoCapture(0)

while True:
    _, frame = capture.read()
    data = {
        'bool': True,
        'int': 123,
        'str': 'Hello there!',
        'image': cv2.imencode('.jpg', frame)[1].ravel().tolist()
    }
    socket.recv()
    socket.send_json(data)  # socket.send(json.dumps(data).encode())

    cv2.imshow('Webcam', frame)
    cv2.waitKey(delay=1)
