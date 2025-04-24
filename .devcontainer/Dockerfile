FROM mcr.microsoft.com/dotnet/sdk:6.0

# Install required packages
RUN apt-get update && \
    apt-get install -y \
        curl \
        git \
        procps \
        unzip \
        && rm -rf /var/lib/apt/lists/*

# Create a non-root user
ARG USERNAME=vscode
ARG USER_UID=1000
ARG USER_GID=$USER_UID

RUN groupadd --gid $USER_GID $USERNAME \
    && useradd --uid $USER_UID --gid $USER_GID -m $USERNAME \
    && apt-get update \
    && apt-get install -y sudo \
    && echo $USERNAME ALL=\(root\) NOPASSWD:ALL > /etc/sudoers.d/$USERNAME \
    && chmod 0440 /etc/sudoers.d/$USERNAME

# Install development tools
RUN dotnet tool install -g dotnet-ef --version 6.0.26
ENV PATH $PATH:/root/.dotnet/tools

# Configure Bash
RUN echo "export PATH=\$PATH:/root/.dotnet/tools" >> /home/$USERNAME/.bashrc

# Set the default user
USER $USERNAME

# Set the working directory
WORKDIR /workspace
