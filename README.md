# Byee

A self-hosted, secure, one-time file transfer service with client-side encryption.

## Features

- **One-Time Downloads**: Files are instantly unavailable once download starts, deleted after completion
- **Client-Side Encryption**: Files are encrypted locally before upload using `age`
- **Streaming**: No temp files - encryption/upload and download/decryption stream directly
- **Word-Based IDs**: Human-readable file IDs like `porcupine-rat-forest-experience`
- **Cross-Platform**: Works on Linux, macOS, Windows, and Alpine
- **Progress Tracking**: Real-time progress bars with speed in MB/s

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

### Send a file
```bash
byee ./file.zip
# Output: byee receive porcupine-rat-forest-experience AGE-SECRET-KEY-1XXXXX
```

### Receive a file
```bash
byee receive porcupine-rat-forest-experience AGE-SECRET-KEY-1XXXXX
# Prompts: Download file.zip (1.5 GB)? [y/N]: y
```

## How It Works

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
11. Server deletes the encrypted file

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `BYEE_PUBLIC_URL` | Public URL of the instance (required) | - |
| `BYEE_STORAGE_PATH` | Path to store encrypted files | `./data` |
| `BYEE_MAX_FILE_SIZE` | Maximum file size in bytes | `107374182400` (100GB) |

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | Auto-detect platform, return installer |
| `/install/{platform}` | GET | Platform-specific installer |
| `/client/{platform}` | GET | Platform-specific client script |
| `/upload` | POST | Upload encrypted file |
| `/download/{id}` | GET | Download encrypted file (one-time) |

## Security

- Server **never** sees unencrypted files
- Encryption key is only known to sender and receiver
- Files are deleted immediately after download (or failed download)
- No accounts, no logs of content

## Building from Source

```bash
dotnet build src/Byee.Server/Byee.Server.csproj -c Release
```

## License

MIT
