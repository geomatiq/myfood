using System;
using System.Diagnostics;
using System.Threading.Tasks;
using myfoodapp.Core.Common;
using myfoodapp.Core.Model;
using System.Net.NetworkInformation;

namespace myfoodapp.Core.Business
{
    public class SigfoxIntegrationTestingManager
    {
        private UserSettingsManager userSettingsManager = UserSettingsManager.GetInstance;
        private LogManager lg = LogManager.GetInstance;
        private DatabaseModel databaseModel = DatabaseModel.GetInstance;

        private AtlasSensorManager sensorManager;
        private SigfoxInterfaceManager sigfoxManager;       

        public SigfoxIntegrationTestingManager()
        {
        }

        public void Run()
        {
            var watch = Stopwatch.StartNew();
            var captureDateTime = DateTime.Now;

            var clockManager = ClockManager.GetInstance;
            var userSettings = userSettingsManager.GetUserSettings();

            lg.AppendLog(Log.CreateLog("[UTEST00] Sigfox Integration Test start", LogType.Information));
            if (clockManager != null)
            {
                Task.Run(async () =>
                {
                    clockManager.InitClock();
                    await Task.Delay(3000);
                }).Wait();

                captureDateTime = clockManager.ReadDate();

                clockManager.Dispose();
            }

            sigfoxManager = SigfoxInterfaceManager.GetInstance;

            lg.AppendLog(Log.CreateLog("[UTEST01] Sigfox Init started", LogType.Information));

            if(userSettings.connectivityType == ConnectivityType.Sigfox)
            {
                var taskSigfox = Task.Run(async () => 
                                        { 
                                            sigfoxManager.InitInterface(); 
                                            await Task.Delay(1000);
                                        });
                taskSigfox.Wait();
            }

            sensorManager = AtlasSensorManager.GetInstance;

            lg.AppendLog(Log.CreateLog("[UTEST03] Atlas Sensors init", LogType.Information));
            var taskSensor = Task.Run(async () => 
                                    { 
                                        sensorManager.InitSensors(false); 
                                        await Task.Delay(1000);
                                    });
            taskSensor.Wait();

            lg.AppendLog(Log.CreateLog("[UTEST04] Air Temp/Hum Sensor init", LogType.Information));
            var humTempManager = HumidityTemperatureManager.GetInstance;

            var taskHumManager = Task.Run(async () =>
            {
                humTempManager.Connect();
                await Task.Delay(1000);
            });
            taskHumManager.Wait();

            try
            {

            var elapsedMs = watch.ElapsedMilliseconds;

            TimeSpan t = TimeSpan.FromMilliseconds(elapsedMs);

            var watchMesures = Stopwatch.StartNew();

            if (sensorManager.isSensorOnline(SensorTypeEnum.waterTemperature))
            {
                lg.AppendLog(Log.CreateLog("[UTEST10] Water Temperature capturing", LogType.Information));

                decimal capturedValue = 0;
                capturedValue = sensorManager.RecordSensorsMeasure(SensorTypeEnum.waterTemperature, false);

                if (capturedValue > -20 && capturedValue < 80)
                {
                    sensorManager.SetWaterTemperatureForPHSensor(capturedValue);

                    var task = Task.Run(async () =>
                    {
                        await databaseModel.AddMesure(captureDateTime, capturedValue, SensorTypeEnum.waterTemperature);
                    });
                    task.Wait();

                    lg.AppendLog(Log.CreateLog(String.Format("[UTEST11] Water Temperature captured : {0}", capturedValue), LogType.Information));
                    var status = sensorManager.GetSensorStatus(SensorTypeEnum.waterTemperature, false);
                    lg.AppendLog(Log.CreateLog(String.Format("[UTEST12] Water Temperature status : {0}", status), LogType.System));
                }
                else
                    lg.AppendLog(Log.CreateLog(String.Format("[UTEST13] Water Temperature value out of range - {0}", capturedValue), LogType.Warning));
            }

            if (sensorManager.isSensorOnline(SensorTypeEnum.pH))
            {
                lg.AppendLog(Log.CreateLog("[UTEST13] PH capturing", LogType.Information));

                decimal capturedValue = 0;
                capturedValue = sensorManager.RecordpHMeasure(false);

                if (capturedValue > 1 && capturedValue < 12)
                {
                    var task = Task.Run(async () =>
                    {
                        await databaseModel.AddMesure(captureDateTime, capturedValue, SensorTypeEnum.pH);
                    });
                    task.Wait();

                    lg.AppendLog(Log.CreateLog(String.Format("[UTEST14] PH captured : {0}", capturedValue), LogType.Information));
                    var status = sensorManager.GetSensorStatus(SensorTypeEnum.pH, false);
                    lg.AppendLog(Log.CreateLog(String.Format("[UTEST15] PH status : {0}", status), LogType.System));
                }
                else
                    lg.AppendLog(Log.CreateLog(String.Format("PH value out of range - {0}", capturedValue), LogType.Warning));
            }

            try
            {
                lg.AppendLog(Log.CreateLog("[UTEST24] Air Temperature capturing", LogType.Information));

                decimal capturedValue = (decimal)humTempManager.Temperature;

                if (capturedValue > 0 && capturedValue < 100)
                {
                    var taskTemp = Task.Run(async () =>
                    {
                        await databaseModel.AddMesure(captureDateTime, capturedValue, SensorTypeEnum.airTemperature);
                    });
                    taskTemp.Wait();

                    lg.AppendLog(Log.CreateLog(String.Format("[UTEST25] Air Temperature captured : {0}", capturedValue), LogType.Information));
                }
                else
                    lg.AppendLog(Log.CreateLog(String.Format("[UTEST26] Air Temperature out of range - {0}", capturedValue), LogType.Warning));
            }
            catch (Exception ex)
            {
                lg.AppendLog(Log.CreateErrorLog("Exception on Air Temperature Sensor", ex));
            }

            try
            {
                lg.AppendLog(Log.CreateLog("[UTEST27] Humidity capturing", LogType.Information));

                decimal capturedValue = (decimal)humTempManager.Humidity;

                if (capturedValue > 0 && capturedValue < 100)
                {
                    var taskHum = Task.Run(async () =>
                    {
                        await databaseModel.AddMesure(captureDateTime, capturedValue, SensorTypeEnum.humidity);
                    });
                    taskHum.Wait();

                    lg.AppendLog(Log.CreateLog(String.Format("[UTEST28] Air Humidity captured : {0}", capturedValue), LogType.Information));
                }
                else
                    lg.AppendLog(Log.CreateLog(String.Format("[UTEST29] Air Humidity out of range - {0}", capturedValue), LogType.Warning));
            }
            catch (Exception ex)
            {
                lg.AppendLog(Log.CreateErrorLog("Exception on Air Humidity Sensor", ex));
            }

            lg.AppendLog(Log.CreateLog(String.Format("[UTEST30] Measures captured in {0} sec.", watchMesures.ElapsedMilliseconds / 1000), LogType.System));

            watchMesures.Restart();

            string messageSignature = String.Empty;

            var taskSig = Task.Run(async () =>
                {
                    messageSignature = await databaseModel.GetLastMesureSignature();
                });
            taskSig.Wait();

            if (userSettings.connectivityType == ConnectivityType.Wifi && NetworkInterface.GetIsNetworkAvailable())
            {
                Task.Run(async () =>
                {
                    await HttpClientHelper.SendMessage(userSettings.hubMessageAPI, 
                                                        messageSignature, 
                                                        userSettings.productionSiteId);
                }).Wait();                                       
            }
            else
            {
                sigfoxManager.SendMessage(messageSignature, userSettings.sigfoxVersion);
            }

            lg.AppendLog(Log.CreateLog(String.Format("[UTEST31] Data sent to Azure via Sigfox in {0} sec.", watchMesures.ElapsedMilliseconds / 1000), LogType.System));
            
            }
        catch (Exception ex)
        {
            lg.AppendLog(Log.CreateErrorLog("Exception on Sigfox Integration Test", ex));
        }

        watch.Stop();
        }
    }
}