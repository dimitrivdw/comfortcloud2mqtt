FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:35792ea4ad1db051981f62b313f1be3b46b1f45cadbaa3c288cd0d3056eefb83 AS build-env
WORKDIR /App

# Copy everything
COPY ./ ./

# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /App
COPY --from=build-env /App/out .

ENV PATH="/root/miniconda3/bin:$PATH"
ENV PATH="/app:$PATH"
ENV PATH="/root/miniconda3/lib/:$PATH"
ARG PATH="/root/miniconda3/bin:$PATH"
RUN apt-get update && apt upgrade -y
RUN apt-get install software-properties-common -y
RUN apt-get install -y wget && rm -rf /var/lib/apt/lists/*
RUN wget https://repo.anaconda.com/miniconda/Miniconda3-py312_24.3.0-0-Linux-x86_64.sh && mkdir /root/.conda && bash Miniconda3-py312_24.3.0-0-Linux-x86_64.sh -b && rm -f Miniconda3-py312_24.3.0-0-Linux-x86_64.sh 
RUN pip install -Iv --no-input pygrib==2.1.5
RUN pip install pcomfortcloud
RUN pip install BeautifulSoup4


ENTRYPOINT ["dotnet", "comfortcloud2mqtt.dll"]