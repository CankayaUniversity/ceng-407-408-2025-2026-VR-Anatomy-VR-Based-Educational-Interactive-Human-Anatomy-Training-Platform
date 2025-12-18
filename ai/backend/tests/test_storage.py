import os
from dotenv import load_dotenv
from azure.storage.blob import BlobServiceClient

load_dotenv(override=True)

svc = BlobServiceClient.from_connection_string(os.environ["AZURE_STORAGE_CONNECTION_STRING"])
cc = svc.get_container_client(os.environ["AZURE_STORAGE_CONTAINER"])

print("Blobs in container:", os.environ["AZURE_STORAGE_CONTAINER"])
for b in cc.list_blobs():
    print("-", b.name)