// See https://aka.ms/new-console-template for more information
using Python.Runtime;
Program program = new Program();

Console.ReadLine();


public partial class Program : IComfortCloudEventHandler, IMQTTEventHandler
{
    private MqttHandler mqtt;
    private ComfortCloudHandler comfortCloud;

    public Program()
    {
        mqtt = new MqttHandler();
        comfortCloud = new ComfortCloudHandler();

        mqtt.Hostname = Environment.GetEnvironmentVariable("MQTTHOSTNAME") ?? "";
        int mqttPort = 1883;
        mqtt.Port = mqttPort;
        if(int.TryParse(Environment.GetEnvironmentVariable("MQTTPORT"),out mqttPort))
            mqtt.Port = mqttPort;

        mqtt.Username = Environment.GetEnvironmentVariable("MQTTUSERNAME") ?? "";
        mqtt.Password = Environment.GetEnvironmentVariable("MQTTPASSWORD") ?? "";
        mqtt.EventHandler = this;

        mqtt.Start().GetAwaiter().GetResult();

        comfortCloud.Username = Environment.GetEnvironmentVariable("COMFORTCLOUDUSERNAME") ?? "";
        comfortCloud.Password = Environment.GetEnvironmentVariable("COMFORTCLOUDPASSWORD") ?? "";
        comfortCloud.EventHandler = this;

        comfortCloud.Start();
        
        Thread.Sleep(Timeout.Infinite);
    }

    public void DeviceUpdated(Device device)
    {
        mqtt.ReportDevice(device);
    }

    public void SetMode(string deviceId, string mode)
    {
        comfortCloud.SetMode(deviceId, mode);
    }
}