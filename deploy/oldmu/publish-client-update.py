"""Publish changed OldMu/Client/ entries from a patch ZIP into a signed, cumulative feed."""

import argparse
import base64
import hashlib
import json
import os
import subprocess
import tempfile
import zipfile
from pathlib import Path


def publish(patch, directory, key):
    directory.mkdir(parents=True, exist_ok=True)
    manifest = directory / "manifest.json"
    files = {}
    if manifest.exists():
        files = json.loads(base64.b64decode(json.loads(manifest.read_bytes())["Payload"]))

    with zipfile.ZipFile(patch) as archive:
        for entry in archive.infolist():
            if entry.is_dir() or not entry.filename.startswith("OldMu/Client/"):
                continue
            name = entry.filename.removeprefix("OldMu/Client/")
            if any(part in ("", ".", "..") for part in name.split("/")) or "\\" in name or ":" in name:
                raise ValueError(f"Invalid client path: {name}")
            data = archive.read(entry)
            digest = hashlib.sha256(data).hexdigest()
            blob = directory / (digest + ".bin")
            if not blob.exists():
                replace(blob, data)
            files[name] = digest

    if not files:
        raise ValueError("No OldMu/Client/ files in release")
    payload = json.dumps(files, sort_keys=True).encode()
    signature = subprocess.run(["openssl", "dgst", "-sha256", "-sign", str(key)],
                               input=payload, capture_output=True, check=True).stdout
    envelope = {"Payload": base64.b64encode(payload).decode(), "Signature": base64.b64encode(signature).decode()}
    replace(manifest, json.dumps(envelope).encode())
    print(f"Published {len(files)} client files")


def replace(path, data):
    with tempfile.NamedTemporaryFile(dir=path.parent, delete=False) as output:
        output.write(data)
    os.chmod(output.name, 0o644)
    os.replace(output.name, path)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for argument in ("patch", "directory", "key"):
        parser.add_argument(argument, type=Path)
    args = parser.parse_args()
    publish(args.patch, args.directory, args.key)
