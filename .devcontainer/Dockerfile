FROM --platform=linux/amd64 mcr.microsoft.com/vscode/devcontainers/python:0-bullseye
RUN curl -L https://dot.net/v1/dotnet-install.sh | \
    bash -s - -c 2.1 --install-dir /opt/dotnet && \
    ln -s /opt/dotnet/dotnet /usr/local/bin/dotnet && \
    pip install pre-commit
