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

    results = gpt2.generate(sess, run_name=RUN_NAME, temperature=.9, nsamples=2, batch_size=2,
                            prefix="<|startoftext|>" + message.decode("utf-8") + "<|endoftext|>", length=250,
                            return_as_list=True, include_prefix=False, truncate="<|endoftext|>")
    socket.send(results[0].encode())
    print("sent response\n")
    time.sleep(.1)
