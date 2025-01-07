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
    DateTime _lastUpdateSent = DateTime.MinValue;
    Dictionary<string, List<string>> _kwargsToBuild = new Dictionary<string, List<string>>();
 
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
                    DateTime LastChecked = DateTime.MinValue;
                    while (true)
                    {
                        try
                        {
                            if ((DateTime.Now - LastChecked).TotalSeconds > 59)
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
                            lock (_kwargsToBuild)
                            {
                                if (_kwargsToBuild.Count > 0 && (DateTime.Now - _lastUpdateSent).TotalSeconds > 3)
                                {
                                    using (Py.GIL())
                                    {
                                        foreach (var deviceArgs in _kwargsToBuild)
                                        {
                                            scope.Exec("kwargs = {}");
                                            foreach (string argsToUse in deviceArgs.Value)
                                            {
                                                scope.Exec(argsToUse);
                                            }
                                            scope.Exec("session.set_device('" + deviceArgs.Key + "',**kwargs)");
                                        }
                                        _kwargsToBuild.Clear();
                                    }
                                }
                            }
                        }
                        catch(Exception exc)
                        {
                            Console.WriteLine("Error while getting data: " + exc);
                        }
                        Thread.Sleep(3000);
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
        lock(_kwargsToBuild)
        {
            _lastUpdateSent = DateTime.Now;

            if(!_kwargsToBuild.ContainsKey(deviceId))
            {
                _kwargsToBuild.Add(deviceId, new List<string>());
            }
            
            if (operationMode != "off")
            {
                _kwargsToBuild[deviceId].Add("kwargs['mode'] = pcomfortcloud.constants.OperationMode['" + FirstLetterUppercase(operationMode) + "']");
            }

            _kwargsToBuild[deviceId].Add("kwargs['power'] = pcomfortcloud.constants.Power['" + (operationMode == "off" ? "Off" : "On") + "']");
        }
        // using (Py.GIL())
        // {
        //     scope.Exec("kwargs = {}");
            
           
        //     scope.Exec("session.set_device('" + deviceId + "',**kwargs)");
        // }
    }

    public void SetTargetTemperature(string deviceId, decimal temperature)
    {

        lock(_kwargsToBuild)
        {
            _lastUpdateSent = DateTime.Now;

            if(!_kwargsToBuild.ContainsKey(deviceId))
            {
                _kwargsToBuild.Add(deviceId, new List<string>());
            }
            
            _kwargsToBuild[deviceId].Add("kwargs['temperature'] = " + temperature);
        }
        // using (Py.GIL())
        // {
        //     scope.Exec("kwargs = {}");
        //     scope.Exec("kwargs['temperature'] = " + temperature);
        //     scope.Exec("session.set_device('" + deviceId + "',**kwargs)");
        // }
    }

    public void SetFanspeed(string deviceId, string fanSpeed)
    {
        lock(_kwargsToBuild)
        {
            _lastUpdateSent = DateTime.Now;

            if(!_kwargsToBuild.ContainsKey(deviceId))
            {
                _kwargsToBuild.Add(deviceId, new List<string>());
            }
            
            _kwargsToBuild[deviceId].Add("kwargs['fanSpeed'] = pcomfortcloud.constants.fanSpeed['" + fanSpeed + "']");
        }
        // using (Py.GIL())
        // {
        //     scope.Exec("kwargs = {}");
        //     scope.Exec("kwargs['fanSpeed'] = pcomfortcloud.constants.fanSpeed['" + fanSpeed + "']");
        //     scope.Exec("session.set_device('" + deviceId + "',**kwargs)");
        // }
    }

    public string FirstLetterUppercase(string text)
    {
        return text.First().ToString().ToUpper() + text.Substring(1).ToLower();
    }
}
