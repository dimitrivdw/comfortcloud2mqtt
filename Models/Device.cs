public class Device {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Model { get; set; }
    public bool Power { get; set; }
    public string Mode { get; set; }
    public decimal CurrentTemperature { get; set; }
    public dynamic SetTemperature { get; set; }
    public string SwingMode { get; set; }
    public string FanMode { get; set; }
}