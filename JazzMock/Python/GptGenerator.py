import gpt_2_simple as gpt2
import zmq
import time

context = zmq.Context()
socket = context.socket(zmq.REP)
socket.bind("tcp://*:5556")

while True:
    message = socket.recv()
    print(f"received request {message}")

    time.sleep(1)

    socket.send(b"World")
