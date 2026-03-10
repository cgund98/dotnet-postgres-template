FROM mcr.microsoft.com/dotnet/sdk:10.0

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    git \
    make \
    bash \
    curl \
    ca-certificates \
    unzip \
    less \
    postgresql-client && \
    rm -rf /var/lib/apt/lists/*

RUN usermod -l workspace -d /home/workspace -m $(id -nu 1000) && \
    groupmod -n workspace $(id -ng 1000)

# Install AWS CLI v2
RUN curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip" && \
    unzip -q awscliv2.zip && \
    ./aws/install && \
    rm -rf awscliv2.zip aws

ENV AWS_PAGER=""

# Workspace uses its own build directory to avoid conflicts with the host
RUN mkdir -p /tmp/build && chown workspace:workspace /tmp/build

WORKDIR /workspace
RUN chown -R workspace:workspace /workspace

USER workspace

CMD ["/bin/bash"]
