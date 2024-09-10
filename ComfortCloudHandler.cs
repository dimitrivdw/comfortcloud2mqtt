// See https://aka.ms/new-console-template for more information
using System.Globalization;
using Python.Runtime;

internal class ComfortCloudHandler
{
    public string Username { get; set; }
    public string Password { get; set; }

    public IComfortCloudEventHandler EventHandler { get; set; }

    private dynamic _session;
    dynamic comfortcloud;
    dynamic constants;

    PyModule scope;
 
    public ComfortCloudHandler()
    {

    }

    public async Task Start()
    {
        try
        {
            Console.WriteLine("Starting python...");

            var pathToVirtualEnv = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"venv"));
            Console.WriteLine("venv = " + pathToVirtualEnv);
            Environment.SetEnvironmentVariable("PYTHONHOME", "/root/miniconda3/lib");
            Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", "/root/miniconda3/lib/libpython3.12.so");

            PythonEngine.Initialize();

            PythonEngine.PythonHome = pathToVirtualEnv;
            PythonEngine.PythonPath = PythonEngine.PythonPath + ";" + Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.Process);
            Console.WriteLine(PythonEngine.PythonPath);
            
            PythonEngine.BeginAllowThreads();

            using (Py.GIL())
            {
                scope = Py.CreateScope();
                comfortcloud = Py.Import("pcomfortcloud");
                scope.Exec("import pcomfortcloud");

                scope.Exec("clientsession = pcomfortcloud.Session('" + Username + "','" + Password + "')");
                scope.Exec("clientsession.login()");
                scope.Exec("session = pcomfortcloud.ApiClient(clientsession)");
            }

            await Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        try
                        {
                            using (Py.GIL())
                            {

                                scope.Exec("devices = session.get_devices()");
                                dynamic devices = scope.Eval("devices");
                                //dynamic devices = _session.get_devices();

                                foreach (dynamic device in devices)
                                {

                                    Device d = new Device()
                                    {
                                        Id = device["id"],
                                        Name = device["name"],
                                        Model = device["model"],
                                    };

                                    dynamic deviceResult = scope.Eval("session.get_device('" + d.Id + "')");
                                    d.Power = deviceResult["parameters"]["power"].ToString() == "Power.On";
                                    d.Mode = deviceResult["parameters"]["mode"].ToString().ToLower().Replace("operationmode.", "");
                                    d.CurrentTemperature = decimal.Parse(deviceResult["parameters"]["temperatureInside"].ToString(), CultureInfo.InvariantCulture);
                                    d.SetTemperature = decimal.Parse(deviceResult["parameters"]["temperature"].ToString(), CultureInfo.InvariantCulture);
                                    d.FanMode = deviceResult["parameters"]["fanSpeed"].ToString().ToLower().Replace("fanspeed.", "");


                                    EventHandler.DeviceUpdated(d);
                                }
                            }
                        }
                        catch(Exception exc)
                        {
                            Console.WriteLine("Error while getting data: " + exc);
                        }
                        Thread.Sleep(60000);
                    }

                }, TaskCreationOptions.LongRunning);



            //Console.WriteLine(session.dump(devices[0]["id"]));
        }
        catch(Exception exc)
        {
            Console.WriteLine(exc);
        }
    }

    public void SetMode(string deviceId, string operationMode)
    {
        using (Py.GIL())
        {
            scope.Exec("kwargs = {}");
            if (operationMode != "off")
            {
                scope.Exec("kwargs['mode'] = pcomfortcloud.constants.OperationMode['" + FirstLetterUppercase(operationMode) + "']");
            }
            scope.Exec("kwargs['power'] = pcomfortcloud.constants.Power['" + (operationMode == "off" ? "Off" : "On") + "']");
            scope.Exec("session.set_device('" + deviceId + "',**kwargs)");
        }
    }

    public void SetTargetTemperature(string deviceId, decimal temperature)
    {
        using (Py.GIL())
        {
            scope.Exec("kwargs = {}");
            scope.Exec("kwargs['temperature'] = " + temperature);
            scope.Exec("session.set_device('" + deviceId + "',**kwargs)");
        }
    }

    public void SetFanspeed(string deviceId, string fanSpeed)
    {
        using (Py.GIL())
        {
            scope.Exec("kwargs = {}");
            scope.Exec("kwargs['fanSpeed'] = pcomfortcloud.constants.fanSpeed['" + fanSpeed + "']");
            scope.Exec("session.set_device('" + deviceId + "',**kwargs)");
        }
    }

    public string FirstLetterUppercase(string text)
    {
        return text.First().ToString().ToUpper() + text.Substring(1).ToLower();
    }
}
