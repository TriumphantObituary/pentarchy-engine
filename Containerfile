# Containerfile
# Pin the exact .NET 10 SDK image for ultimate determinism
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS dev-env

WORKDIR /app

# Install basic development utilities inside the container
RUN apk add --no-cache bash git

# Keep the container alive for interactive terminal development
CMD ["bash"]