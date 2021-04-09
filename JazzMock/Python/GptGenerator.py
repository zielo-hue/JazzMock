import gpt_2_simple as gpt2
import zmq
import time

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

    results = gpt2.generate(sess, run_name=RUN_NAME, temperature=.9, nsamples=1, batch_size=1,
                            prefix=message, length=250,
                            return_as_list=False, include_prefix=False, truncate="\n\n")

    socket.send(results.encode('utf-8'))
    print("sent response\n")
    time.sleep(.1)
