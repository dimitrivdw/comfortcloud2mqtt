public interface IMQTTEventHandler {
    void SetMode(string deviceId, string mode);
    void SetFanSpeed(string deviceId, string fanspeed);
    void SetTemperature(string deviceId, decimal temperature);
}