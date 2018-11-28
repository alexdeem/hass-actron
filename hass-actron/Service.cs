using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace HMX.HASSActron
{
    internal class Service
    {
		private static string _strServiceName = "hass-actron";

		public static void Start()
        {
			IConfigurationRoot configuration;
			IWebHost webHost;

			Logging.WriteDebugLog("ServiceCore.Start()");

			try
			{
				configuration = new ConfigurationBuilder().AddJsonFile("/data/options.json", false, true).Build();
			}
			catch (Exception eException)
			{
				Logging.WriteDebugLogError("Program.Main()", eException, "Unable to build configuration instance.");
				return;
			}

			MQTT.StartMQTT(configuration["MQTTBroker"] ?? "localhost", _strServiceName, configuration["MQTTUser"] ?? "", configuration["MQTTPassword"] ?? "", MQTTProcessor);

			AirConditioner.Configure(configuration);

			try
			{
				webHost = new WebHostBuilder().UseKestrel().UseStartup<ASPNETCoreStartup>().UseConfiguration(configuration).UseUrls($"http://*:80/").Build();
			}
			catch (Exception eException)
			{
				Logging.WriteDebugLogError("Program.Main()", eException, "Unable to build Kestrel instance.");
				return;
			}

			webHost.Run();
		}

		private static void MQTTRegister()
		{
			Logging.WriteDebugLog("ServiceCore.MQTTRegister()");

			MQTT.SendMessage("homeassistant/climate/actron/aircon/config", "{{\"name\":\"Air Conditioner\",\"icon\":\"mdi:air-conditioner\",\"optimistic\":\"false\",\"modes\":[\"off\",\"auto\",\"cool\",\"fan_only\",\"heat\"],\"fan_modes\":[\"high\",\"medium\",\"low\"],\"mode_command_topic\":\"actron/aircon/mode/set\",\"temperature_command_topic\":\"actron/aircon/temperature/set\",\"fan_mode_command_topic\":\"actron/aircon/fan/set\",\"min_temp\":\"16\",\"max_temp\":\"25\",\"fan_mode_state_topic\":\"actron/aircon/fanmode\",\"temperature_state_topic\":\"actron/aircon/settemperature\",\"mode_state_topic\":\"actron/aircon/mode\",\"current_temperature_topic\":\"actron/aircon/temperature\",\"availability_topic\":\"{0}/status\"}}", _strServiceName.ToLower());

			foreach (int iZone in AirConditioner.Zones.Keys)
			{
				MQTT.SendMessage(string.Format("homeassistant/switch/actron/airconzone{0}/config", iZone), "{{\"name\":\"{0} Zone\",\"icon\":\"mdi:air-conditioner\",\"state_topic\":\"actron/aircon/zone{1}\",\"command_topic\":\"actron/aircon/zone{1}/set\",\"payload_on\":\"ON\",\"payload_off\":\"OFF\",\"state_on\":\"ON\",\"state_off\":\"OFF\",\"availability_topic\":\"{2}/status\"}}", AirConditioner.Zones[iZone].Name, iZone, _strServiceName.ToLower());
				MQTT.Subscribe("actron/aircon/zone{0}/set", iZone);
			}

			MQTT.Subscribe("actron/aircon/mode/set");
			MQTT.Subscribe("actron/aircon/fan/set");
			MQTT.Subscribe("actron/aircon/temperature/set");
		}

		private static void MQTTProcessor(string strTopic, string strPayload)
		{
			long lRequestId = 0;
			double dblTemperature = 0;

			Logging.WriteDebugLog("ServiceCore.MQTTProcessor() {0}", strTopic);

			switch (strTopic)
			{
				case "actron/aircon/zone1/set":
					AirConditioner.ChangeZone(lRequestId, 1, strPayload == "ON" ? true : false);
					break;

				case "actron/aircon/zone2/set":
					AirConditioner.ChangeZone(lRequestId, 2, strPayload == "ON" ? true : false);
					break;

				case "actron/aircon/zone3/set":
					AirConditioner.ChangeZone(lRequestId, 3, strPayload == "ON" ? true : false);
					break;

				case "actron/aircon/zone4/set":
					AirConditioner.ChangeZone(lRequestId, 4, strPayload == "ON" ? true : false);
					break;

				case "actron/aircon/mode/set":
					Logging.WriteDebugLog("ServiceCore.MQTTProcessor() {0}: {1}", strTopic, strPayload);

					switch (strPayload)
					{
						case "off":
							AirConditioner.ChangeMode(lRequestId, AirConditionerMode.None);

							break;

						case "auto":
							AirConditioner.ChangeMode(lRequestId, AirConditionerMode.Automatic);

							break;

						case "cool":
							AirConditioner.ChangeMode(lRequestId, AirConditionerMode.Cooling);

							break;

						case "heat":
							AirConditioner.ChangeMode(lRequestId, AirConditionerMode.Heating);

							break;

						case "fan_only":
							AirConditioner.ChangeMode(lRequestId, AirConditionerMode.FanOnly);

							break;
					}

					break;

				case "actron/aircon/fan/set":
					Logging.WriteDebugLog("ServiceCore.MQTTProcessor() {0}: {1}", strTopic, strPayload);

					switch (strPayload)
					{
						case "low":
							AirConditioner.ChangeFanSpeed(lRequestId, FanSpeed.Low);

							break;

						case "medium":
							AirConditioner.ChangeFanSpeed(lRequestId, FanSpeed.Medium);

							break;

						case "high":
							AirConditioner.ChangeFanSpeed(lRequestId, FanSpeed.High);

							break;
					}

					break;

				case "actron/aircon/temperature/set":
					Logging.WriteDebugLog("ServiceCore.MQTTProcessor() {0}: {1}", strTopic, strPayload);

					if (double.TryParse(strPayload, out dblTemperature))
						AirConditioner.ChangeTemperature(lRequestId, dblTemperature);

					break;
			}
		}
    }

}