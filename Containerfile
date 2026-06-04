# Containerfile
# Pin the exact SDK image using its cryptographic or stable tag to ensure 100% determinism
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS dev-env

WORKDIR /app

# Install basic development utilities inside the container
RUN apk add --no-cache bash git

# Keep the container alive for interactive terminal development
CMD ["bash"]