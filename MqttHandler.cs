// See https://aka.ms/new-console-template for more information
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MQTTnet;
using MQTTnet.Client;

internal class MqttHandler
{
    public string Hostname { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }

    public string Prefix { get; set; } = "homeassistant";

    public IMQTTEventHandler EventHandler { get; set; }

    private List<string> deviceIds = new List<string>();

    private IMqttClient mqttClient;
    private MqttFactory mqttFactory;

    public MqttHandler()
    {
        
    }

    public async Task Start() {
        try {
            mqttFactory = new MqttFactory();

            mqttClient = mqttFactory.CreateMqttClient();
            
            // Use builder classes where possible in this project.
            var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(Hostname,Port,System.Net.Sockets.AddressFamily.InterNetwork).WithCredentials(Username,Password).Build();

            mqttClient.ApplicationMessageReceivedAsync += ApplicationMessageReceived;
            mqttClient.DisconnectedAsync += Disconnected;

            // This will throw an exception if the server is not available.
            // The result from this message returns additional data which was sent 
            // from the server. Please refer to the MQTT protocol specification for details.
            var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            Console.WriteLine("The MQTT client is connected.");
        }
        catch(Exception exc)
        {
            Console.WriteLine("Error while connecting to MQTT broker: " + exc);
        }
    
    }

    private async Task Disconnected(MqttClientDisconnectedEventArgs args)
    {
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(Hostname,Port,System.Net.Sockets.AddressFamily.InterNetwork).WithCredentials(Username,Password).Build();
        await mqttClient.ConnectAsync(mqttClientOptions,CancellationToken.None);
    }

    private async Task ApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs args)
    {
        string[] splitted = args.ApplicationMessage.Topic.Split("/", StringSplitOptions.RemoveEmptyEntries);
        if(args.ApplicationMessage.Topic.StartsWith(Prefix + "/climate/") && deviceIds.Contains(splitted[2]) && splitted[3] == "command")
        {
            switch(splitted[4])
            {
                case "mode":
                    EventHandler.SetMode(splitted[2], Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment));
                    break;
            }
        }
    }

    public async Task Stop() {
        var mqttClientDisconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();
        await mqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);
    }

    internal async Task ReportDevice(Device device)
    {
        string climatePrefix = Prefix + "/climate/" + device.Id;
        MqttDiscoveryClimateDevice devicePayload = new MqttDiscoveryClimateDevice()
        {
            action_topic = climatePrefix + "/state/action",
            current_temperature_topic = climatePrefix + "/state/currentTemperature",
            fan_mode_command_topic = climatePrefix + "/command/fanMode",
            fan_mode_state_topic = climatePrefix + "/state/fanMode",
            mode_command_topic = climatePrefix + "/command/mode",
            mode_state_topic = climatePrefix + "/state/mode",
            name = device.Name,
            object_id = device.Id,
            power_command_topic = climatePrefix + "/command/power",
            swing_mode_command_topic = climatePrefix + "/command/swingMode",
            swing_mode_state_topic = climatePrefix + "/state/swingMode",
            temperature_command_topic = climatePrefix + "/command/temperature",
            temperature_state_topic = climatePrefix + "/state/temperature",
            unique_id = device.Id
        };
        devicePayload.device.manufacturer = "Panasonic";
        devicePayload.device.model = device.Model;
        devicePayload.device.name = device.Name;
        devicePayload.device.identifiers = device.Id;
        devicePayload.availability_topic = climatePrefix + "/state/availability";

        //send discovery message
        string payload = JsonSerializer.Serialize(devicePayload);

        if (!deviceIds.Contains(device.Id))
        {
            await mqttClient.SubscribeAsync(climatePrefix + "/command/mode", MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
            deviceIds.Add(device.Id);
        }

        MqttApplicationMessage message = new MqttApplicationMessage() { Topic = "homeassistant/climate/" + device.Id + "/config", PayloadSegment = Encoding.UTF8.GetBytes(payload), Retain = true };
        await mqttClient.PublishAsync(message);

        message = new MqttApplicationMessage() { Topic = climatePrefix + "/state/mode", PayloadSegment = Encoding.UTF8.GetBytes(device.Power ? device.Mode.ToLower() : "off") };
        await mqttClient.PublishAsync(message);

        message = new MqttApplicationMessage() { Topic = climatePrefix + "/state/availability", PayloadSegment = Encoding.UTF8.GetBytes("online") };
        await mqttClient.PublishAsync(message);

        message = new MqttApplicationMessage() { Topic = climatePrefix +"/state/currentTemperature", PayloadSegment = Encoding.UTF8.GetBytes(device.CurrentTemperature.ToString(CultureInfo.InvariantCulture)) };
        await mqttClient.PublishAsync(message);

        // message = new MqttApplicationMessage() { Topic = climatePrefix + "/state/swingMode", PayloadSegment = Encoding.UTF8.GetBytes(device.SwingMode) };
        // await mqttClient.PublishAsync(message);

        message = new MqttApplicationMessage() { Topic = climatePrefix + "/state/fanMode", PayloadSegment = Encoding.UTF8.GetBytes(device.FanMode) };
        await mqttClient.PublishAsync(message);

        
    }
}
