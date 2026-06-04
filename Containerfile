# Containerfile
# Pin the exact .NET 10 SDK image for ultimate determinism
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS dev-env

WORKDIR /app

# Configure the system PATH to discover .NET global tools natively
ENV PATH="${PATH}:/root/.dotnet/tools"

# Install basic development utilities inside the container
RUN apk add --no-cache bash git
# Automate the report generator tool installation right during the image build phase
RUN dotnet tool install --global dotnet-reportgenerator-globaltool

# Keep the container alive for interactive terminal development
CMD ["bash"]