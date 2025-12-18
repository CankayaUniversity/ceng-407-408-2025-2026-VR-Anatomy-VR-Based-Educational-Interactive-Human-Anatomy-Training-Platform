import os
from dotenv import load_dotenv
from azure.core.credentials import AzureKeyCredential
from azure.search.documents import SearchClient

load_dotenv(override=True)

client = SearchClient(
    endpoint=os.environ["AZURE_SEARCH_ENDPOINT"].rstrip("/"),
    index_name=os.environ["AZURE_SEARCH_INDEX"],
    credential=AzureKeyCredential(os.environ["AZURE_SEARCH_KEY"]),
)

results = client.search(search_text="kalp", top=1)
r = next(results, None)

print("== Search OK ==")
if r is None:
    print("No results.")
    raise SystemExit

d = dict(r)
print("Fields:", list(d.keys()))

print("\n--- Candidate text fields (string olanlar) ---")
for k, v in d.items():
    if isinstance(v, str) and len(v) > 0:
        snippet = v[:300].replace("\n", " ")
        print(f"{k} (len={len(v)}): {snippet}")

print("\n--- Skipping big vector/list fields ---")
for k, v in d.items():
    if isinstance(v, list) and len(v) > 20 and all(isinstance(x, (int, float)) for x in v[:20]):
        print(f"{k}: <vector length={len(v)}>")

print("\nMeta:")
print("@search.score:", d.get("@search.score"))