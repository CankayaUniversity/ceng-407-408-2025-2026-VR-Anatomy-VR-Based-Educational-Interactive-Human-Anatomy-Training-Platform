import os
from dotenv import load_dotenv
from openai import AzureOpenAI

load_dotenv(override=True)

client = AzureOpenAI(
    api_key=os.environ["AZURE_OPENAI_API_KEY"],
    azure_endpoint=os.environ["AZURE_OPENAI_ENDPOINT"].rstrip("/"),
    api_version=os.environ["AZURE_OPENAI_API_VERSION"],
)

resp = client.chat.completions.create(
    model=os.environ["AZURE_OPENAI_CHAT_DEPLOYMENT"],
    messages=[{"role": "user", "content": "Sadece OK yaz."}],
)

print(resp.choices[0].message.content)