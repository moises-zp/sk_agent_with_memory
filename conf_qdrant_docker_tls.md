# 🚀 Qdrant Configuration with mTLS (Docker and Self-Signed Certificates)

This procedure guides you through creating self-signed certificates and building a Qdrant Docker image configured to use TLS/HTTPS and Mutual TLS Authentication (mTLS).

## 1. Environment and Certificate Preparation (CA, Server, and Client)

Create the directory structure and navigate to the certificates folder.
```bash
cd ~
mkdir -p qdrant/certs
cd qdrant/certs
```

### A. Create the Certificate Authority (CA)

Generate the private key (ca.key) and the self-signed root certificate (ca.crt).
```bash
# CA private key
openssl genrsa -out ca.key 4096

# Self-signed certificate (valid for 10 years)
openssl req -x509 -new -nodes -key ca.key -sha256 -days 3650 \
  -subj "/CN=Qdrant-Local-CA" \
  -out ca.crt
```

### B. Create Server Certificate (Qdrant)

Generate the private key, certificate signing request (CSR), and sign the certificate with the CA, including the Subject Alternative Name (SAN) for localhost and 127.0.0.1.
```bash
# Server key
openssl genrsa -out server.key 2048

# Certificate request (CSR)
openssl req -new -key server.key -out server.csr -subj "/CN=localhost"

# Extension with Subject Alternative Name (SAN)
cat > server-ext.cnf <<EOF
subjectAltName = @alt_names
[alt_names]
DNS.1 = localhost
IP.1 = 127.0.0.1
EOF

# Sign with CA
openssl x509 -req -in server.csr -CA ca.crt -CAkey ca.key \
  -CAcreateserial -out server.crt -days 365 -sha256 \
  -extfile server-ext.cnf
```

### C. Create Client Certificate (.NET Agent)

Generate the client certificate required for mTLS authentication.
```bash
openssl genrsa -out client.key 2048
openssl req -new -key client.key -out client.csr -subj "/CN=qdrant-client"
openssl x509 -req -in client.csr -CA ca.crt -CAkey ca.key \
  -CAcreateserial -out client.crt -days 365 -sha256
```

## 2. Dockerfile for Qdrant (mTLS)

Navigate to the Qdrant main directory (`~/qdrant`) and create the Dockerfile.
```bash
cd ..
# Here you create the Dockerfile
# nano Dockerfile
```

### Dockerfile Content
```dockerfile
# 1. Official Qdrant base image
FROM qdrant/qdrant:latest

# 2. User Configuration and Directory Creation
USER root
# Create the /tls directory where certificates will be copied
RUN mkdir -p /tls /tls/config /qdrant/storage /qdrant/snapshots

# 3. Copy Files to Container
# Copy certificates and keys
# Copy all contents from 'certs' to '/tls'.
COPY certs/ /tls/

# 4. Permissions and Ownership Assignment
# Non-root users need access. User 1000 is the default Qdrant user.
RUN chown -R 1000:1000 /tls /qdrant/storage /qdrant/snapshots && \
    chmod 700 /tls && \
    # Restrictive permissions for private key (read-only by owner)
    chmod 600 /tls/server.key && \
    # Read permissions for server certificate and CA
    chmod 644 /tls/server.crt /tls/ca.crt

# 5. Environment Variables (Override YAML configuration)

# Enable TLS
ENV QDRANT__SERVICE__ENABLE_TLS=true
# Enable Client Authentication (mTLS)
ENV QDRANT__SERVICE__ENABLE_CLIENT_AUTH=true

# KEY: Certificate paths using the QDRANT__TLS__ namespace
ENV QDRANT__TLS__CERT=/tls/server.crt
ENV QDRANT__TLS__KEY=/tls/server.key
ENV QDRANT__TLS__CA_CERT=/tls/ca.crt

# Storage path
ENV QDRANT__STORAGE__PATH=/qdrant/storage

# 6. Switch to Non-root User and Expose Ports
USER 1000

EXPOSE 6333 6334

# 7. Entrypoint
ENTRYPOINT ["/qdrant/qdrant"]
```

## 3. Container Build and Execution

Run these commands from the `~/qdrant` folder.
```bash
# 1. Create the image
docker build -t iqdrant-mtls:latest .

# 2. Start the container (REST and gRPC Ports)
docker run -d --name cqdrant-mtls -p 6333:6333 -p 6334:6334 iqdrant-mtls:latest

# 3. View logs (the server should start without 'file not found' errors)
docker logs -f cqdrant-mtls
```

## 4. Optional: Client Certificate Preparation for .NET (PKCS12)

The .NET runtime, especially on macOS, works better with a PKCS12 file (.pfx) that contains both the client certificate and its private key. This step is essential for mTLS authentication in your client application (such as your Semantic Kernel Agent).

Run this command from the `~/qdrant/certs/` folder:
```bash
cd qdrant/certs

# Combine client.crt and client.key into client.pfx
# You will be prompted for a password to protect the file. Use 'qdrant'
openssl pkcs12 -export -out client.pfx -inkey client.key -in client.crt -certfile ca.crt
```

The `client.pfx` file (along with the `ca.crt`) must be used by your .NET application to connect to Qdrant.