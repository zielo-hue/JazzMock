import gpt_2_simple as gpt2
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

while True:
    print("waiting...")
    message = socket.recv()
    print(f"received request {message}")

    results = gpt2.generate(sess, run_name=RUN_NAME, temperature=.7, nsamples=2, batch_size=2,
                            prefix="<|startoftext|>" + message.decode("utf-8").strip() + "<|endoftext|>\n<|startoftext|>", length=250,
                            return_as_list=True, include_prefix=False, truncate="<|endoftext|>")
    for r in results:
        if r.isspace() or len(r.strip()) == 0:
            results.remove(r)

    print(*results, sep=" || ")
    random_result = random.choice(results).encode()
    socket.send(random_result)
    print("sent response\n")
    time.sleep(.1)
