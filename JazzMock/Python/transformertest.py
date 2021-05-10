from transformers import AutoTokenizer, TFAutoModelWithLMHead, pipeline, set_seed

generator = pipeline('text-generation', model='distilgpt2')
set_seed(42)
generation = generator("I like transgender people because ", max_length=70, num_return_sequences=5)
for thing in generation:
    print(thing)

# tokenizer = AutoTokenizer.from_pretrained("distilgpt2")

#model = TFAutoModelWithLMHead.from_pretrained("distilgpt2")

#text = "Some text"
#encoded_input = tokenizer(text, return_tensors="tf")
#output = model(encoded_input)
#print(output)