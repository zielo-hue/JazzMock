﻿import gpt_2_simple as gpt2
import zmq
import time
import random

RUN_NAME = "test"

sess = gpt2.start_tf_sess()
gpt2.load_gpt2(sess, run_name=RUN_NAME)  # The name of your checkpoint
graph = gpt2.tf.compat.v1.get_default_graph()

context = zmq.Context()
socket = context.socket(zmq.REP)
socket.bind("tcp://*:5556")

newline = "\n"
requestCount = 0

while True:
    print(f"waiting... request count at {requestCount}")
    multi_message = socket.recv_multipart()
    args = int.from_bytes(multi_message[0], "big")
    message = multi_message[1]
    truncateArg = "<|endoftext|>"
    print(f"received request {message.decode('utf-8')}")

    requestCount += 1
    print(args)
    if args == 1: # from the genconvo command
        truncateArg = ""
    results = gpt2.generate(sess, run_name=RUN_NAME, temperature=.7, nsamples=2, batch_size=2,
                            prefix=message.decode(
                                "utf-8"), length=250,
                            return_as_list=True, include_prefix=False, truncate=truncateArg)
    for r in results:
        if r.isspace() or len(r.strip()) == 0:
            results.remove(r)

    print(*results, sep=" || ")
    # random_result = random.choice(results).encode()
    # socket.send(random_result)
    # socket.send_string(newline.join(results))
    socket.send_string(random.choice(results))

    print("sent response\n")

    if requestCount > 20:
        print("resetting graph...")
        requestCount = 0
        gpt2.reset_session(sess)
        sess = gpt2.start_tf_sess()
        gpt2.load_gpt2(sess, run_name=RUN_NAME)  # The name of your checkpoint
        graph = gpt2.tf.compat.v1.get_default_graph()

    time.sleep(.1)
