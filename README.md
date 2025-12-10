# Byee

A self-hosted, one-time file transfer service with optional client-side encryption.

## Features

- **One-Time Downloads**: Files are instantly unavailable once download starts, deleted after completion
- **Optional Client-Side Encryption**: Client encrypts files by default using `age`, but can be disabled
- **Streaming**: No temp files - encryption/upload and download/decryption stream directly
- **Word-Based IDs**: Human-readable file IDs like `porcupine-rat-forest-experience`
- **Cross-Platform**: Works on Linux, macOS, Windows, and Alpine
- **Progress Tracking**: Real-time progress bars with speed in MB/s
- **Simple API**: Can be used directly with curl or any HTTP client

## Quick Start

### Server Deployment (Docker)

```bash
docker run -d \
  -p 8080:8080 \
  -e BYEE_PUBLIC_URL=https://byee.example.com \
  -v byee-data:/app/data \
  git.juzo.io/juzo/byee:latest
```

Or with docker-compose (see `docker/docker-compose.example.yml`).

### Client Installation

```bash
# Linux/macOS - one command install
curl -fsSL https://byee.example.com | sh

# Windows PowerShell
irm https://byee.example.com | iex
```

## Usage

### Send a file (encrypted by default)
```bash
byee ./file.zip
# Output: byee receive porcupine-rat-forest-experience A2K8X9M4P1Q7
```

### Send without encryption
```bash
byee --no-encrypt ./file.zip
# Output: byee receive porcupine-rat-forest-experience
```

### Receive a file
```bash
byee receive porcupine-rat-forest-experience A2K8X9M4P1Q7
# Prompts: Download file.zip (1.5 GB)? [y/N]: y
```

## How It Works

### With Encryption (Default)
1. **Sender** runs `byee ./file.zip`
2. Client generates an `age` encryption key
3. File is **streamed** through encryption directly to the server (no temp files)
4. Server stores encrypted file, returns word-based ID
5. Client displays: `byee receive <id> <key>`

6. **Receiver** runs the command
7. Server confirms file exists, returns metadata (filename, size)
8. File is **instantly claimed** - no one else can access it
9. User confirms download
10. File streams from server through decryption directly to disk
11. Server deletes the file

### Without Encryption
When using `--no-encrypt`, files are uploaded and downloaded as-is. The server stores the raw file.

## Direct API Usage

The server can be used directly without the client, using curl or any HTTP client:

### Upload a file
```bash
curl -X POST https://byee.example.com/upload \
  -H "Content-Type: application/octet-stream" \
  -H "X-Byee-Filename: myfile.zip" \
  -H "X-Byee-Size: 1234567" \
  --data-binary @myfile.zip
# Response: {"id":"porcupine-rat-forest-experience","command":"byee receive porcupine-rat-forest-experience <KEY>"}
```

### Get file info and claim
```bash
curl https://byee.example.com/download/porcupine-rat-forest-experience?info=true
# Response: {"filename":"myfile.zip","size":1234567,"size_human":"1.18 MB","claim_token":"..."}
```

### Download (with claim token)
```bash
curl -o myfile.zip https://byee.example.com/download/porcupine-rat-forest-experience \
  -H "X-Byee-Claim-Token: <token-from-info>"
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `BYEE_PUBLIC_URL` | Public URL of the instance (required) | - |
| `BYEE_STORAGE_PATH` | Path to store files | `./data` |
| `BYEE_MAX_FILE_SIZE` | Maximum file size in bytes | `107374182400` (100GB) |

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | Auto-detect platform, return installer |
| `/install/{platform}` | GET | Platform-specific installer |
| `/client/{platform}` | GET | Platform-specific client script |
| `/upload` | POST | Upload file (encrypted or raw) |
| `/download/{id}` | GET | One-time download |
| `/download/{id}?info=true` | GET | Get file info and claim |
| `/health` | GET | Health check |

## Security Notes

- **With encryption (default)**: Server never sees unencrypted content. Encryption key is only known to sender and receiver.
- **Without encryption**: Server stores raw files. Use HTTPS to protect data in transit.
- Files are deleted immediately after download (or failed/cancelled download)
- No accounts, no content logs

## Building from Source

```bash
dotnet build src/Byee.Server/Byee.Server.csproj -c Release
```

## License

BSD-3-Clause
