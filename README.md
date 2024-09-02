# ComfortCloud2Mqtt

This project has the objective to connect to the Panasonic Comfort Cloud and publishes the data on an MQTT broker, using the HomeAssistant discovery protocol, making this usable in at least the following products:
- HomeAssistant
- Openhab (tested in 4.2.1 where the HVAC discovery was added)

# Dependencies

## pcomfortcloud

The communication with Comfort Cloud is already made in another project: https://github.com/lostfields/python-panasonic-comfort-cloud

pcomfortcloud uses Python, while this project is written in C#, which uses Python.NET to interact with pcomfortcloud.

## Docker

This project is written to function within a docker container for easy deployment. See the examples below to see how to use the docker image.

A Docker Hub image is also available: https://hub.docker.com/r/dimitrivdw/comfortcloud2mqtt

# Examples
## Docker compose

    services:
      comfortcloud2mqtt:
        image: "dimitrivdw/comfortcloud2mqtt"
        restart: unless-stopped
        environment:
          MQTTHOSTNAME: "myHostNameOrIP"
          MQTTPORT: "1883"
          MQTTUSERNAME: "mqttUsername"
          MQTTPASSWORD: "mqttPassword"
          COMFORTCLOUDUSERNAME: "ComfortCloudUsername"
          COMFORTCLOUDPASSWORD: "ComfortCloudPassword"
