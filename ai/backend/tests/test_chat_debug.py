import os
from dotenv import load_dotenv
from openai import AzureOpenAI

load_dotenv(override=True)

endpoint = os.environ["AZURE_OPENAI_ENDPOINT"].rstrip("/")
version = os.environ["AZURE_OPENAI_API_VERSION"]
deployment = os.environ["AZURE_OPENAI_CHAT_DEPLOYMENT"]
key = os.environ["AZURE_OPENAI_API_KEY"]

print("ENDPOINT:", repr(endpoint))
print("API_VERSION:", repr(version))
print("DEPLOYMENT:", repr(deployment))
print("KEY_LEN:", len(key))

client = AzureOpenAI(
    api_key=key,
    azure_endpoint=endpoint,
    api_version=version,
)

resp = client.chat.completions.create(
    model=deployment,
    messages=[{"role": "user", "content": "Sadece 'OK' yaz."}],
)

print("RESPONSE:", resp.choices[0].message.content)