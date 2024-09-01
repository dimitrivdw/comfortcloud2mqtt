using System.Text.Json.Serialization;

public class MqttDiscoveryClimateDevice {
    public string action_topic { get; set; }
    //public MqttDiscoveryAvailability availability { get; set; } = new MqttDiscoveryAvailability();
    public string availability_mode { get; set; } = "latest";
    public string availability_topic { get; set; }
    public string current_temperature_topic { get; set; }
    public MqttDevice device { get; set; } = new MqttDevice();
    public string fan_mode_command_topic { get; set; }
    public string fan_mode_state_topic { get; set; }
    public decimal max_temp { get; set; } = 30;
    public decimal min_temp { get; set; } = 16;
    public string mode_command_topic { get; set; }
    public string mode_state_topic { get; set; }
    public string name { get; set; }
    public string object_id { get; set; }
    public string power_command_topic { get; set; }
    public decimal precision { get; set; } = 0.5m;
    // public string preset_mode_command_topic { get; set; }
    // public string preset_mode_state_topic { get; set; }
    // public string[] preset_modes { get; set; } = ""
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string swing_mode_command_template { get; set; }

    public string swing_mode_state_topic { get; set; }
    public string[] swing_modes { get; set; } = ["auto","left","right","up","down"];

    public string[] fan_modes { get; set; } = ["auto", "low", "lowmid", "mid", "highmid", "high"];

    public string temperature_command_topic { get; set; }
    public string temperature_state_topic { get; set; }
    public string temperature_unit { get; set; } = "C";
    public decimal temp_step { get; set; } = 0.5m;
    public string unique_id { get; set; }
    public string swing_mode_command_topic { get; set; }
}

public class MqttDiscoveryAvailability {
    public string payload_available { get; set; } = "online";
    public string payload_not_available { get; set; } = "offline";
    public string topic { get; set; }
}

public class MqttDevice {
    public string manufacturer { get; set; }
    public string model { get; set; }
    public string name { get; set; }
    public string identifiers { get; set; }
}
