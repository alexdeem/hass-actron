﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HMX.HASSActron.Controllers
{
	[Route("rest/{version:required}/block/{device:required}")]
    public class DeviceController : Controller
	{
		[Route("commands")]
		public IActionResult Command(string version, string device)
		{
			AirConditionerCommand command;
			ContentResult result;
			string strCommandType;

			Logging.WriteDebugLog("DeviceController.Command() Client: {0}:{1}", HttpContext.Connection.RemoteIpAddress.ToString(), HttpContext.Connection.RemotePort.ToString());

			HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", new Microsoft.Extensions.Primitives.StringValues("Accept, Content-Type, Authorization, Content-Length, X-Requested-With, X-Ninja-Token"));
			HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", new Microsoft.Extensions.Primitives.StringValues("GET,PUT,POST,DELETE,OPTIONS"));
			HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", new Microsoft.Extensions.Primitives.StringValues("*"));

			command = AirConditioner.GetCommand(out strCommandType);

			if (strCommandType != "4" & strCommandType != "5")
				return new EmptyResult();
			else
			{
				result = new ContentResult();

				result.ContentType = "application/json";
				result.StatusCode = 200;

				if (strCommandType == "4")
				{
					result.Content = string.Format("{{\"DEVICE\":[{{\"G\":\"0\",\"V\":2,\"D\":4,\"DA\":{{\"amOn\":{0},\"tempTarget\":{1},\"fanSpeed\":{2},\"mode\":{3}}}}}]}}",
						command.amOn ? "true" : "false",
						command.tempTarget.ToString("F1"),
						command.fanSpeed.ToString(),
						command.mode.ToString()
					);

					Logging.WriteDebugLog("DeviceController.Command() Command: {0}", result.Content);

				}
				else if (strCommandType == "5")
				{
					result.Content = string.Format("{{\"DEVICE\":[{{\"G\":\"0\",\"V\":2,\"D\":5,\"DA\":{{\"enabledZones\":[{0}]}}}}]}}",
						command.enabledZones
					);

					Logging.WriteDebugLog("DeviceController.Command() Command: {0}", result.Content);
				}

				return result;
			}
		}

		[Route("data")]
		public IActionResult Data(string version, string device)
		{
			AirConditionerData data = new AirConditionerData();
			AirConditionerDataHeader header;
			AirConditionerDataHeader6 header6;
			Dictionary<string, object> dDataField;
			DataResponse response = new DataResponse();
			StreamReader reader;
			string strData;
			Newtonsoft.Json.Linq.JArray aZones;

			Logging.WriteDebugLog("DeviceController.Data() Client: {0}:{1}", HttpContext.Connection.RemoteIpAddress.ToString(), HttpContext.Connection.RemotePort.ToString());

			HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", new Microsoft.Extensions.Primitives.StringValues("Accept, Content-Type, Authorization, Content-Length, X-Requested-With, X-Ninja-Token"));
			HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", new Microsoft.Extensions.Primitives.StringValues("GET,PUT,POST,DELETE,OPTIONS"));
			HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", new Microsoft.Extensions.Primitives.StringValues("*"));

			reader = new StreamReader(HttpContext.Request.Body);
			strData = reader.ReadToEnd();
			reader.Dispose();

			// Logging.WriteDebugLog("DeviceController.Data() Data: {0}", strData);

			try
			{
				header = JsonConvert.DeserializeObject<AirConditionerDataHeader>(strData);
				switch (header.D)
				{
					case 6:
						Logging.WriteDebugLog("DeviceController.Data() Data: {0}", strData);

						header6 = JsonConvert.DeserializeObject<AirConditionerDataHeader6>(strData);

						dDataField = header6.DA;

						data.iCompressorActivity = int.Parse(dDataField["compressorActivity"].ToString());
						data.strErrorCode = dDataField["errorCode"].ToString();
						data.iFanContinuous = int.Parse(dDataField["fanIsCont"].ToString());
						data.iFanSpeed = int.Parse(dDataField["fanSpeed"].ToString());
						data.bOn = bool.Parse(dDataField["isOn"].ToString());
						data.bESPOn = bool.Parse(dDataField["isInESP_Mode"].ToString());
						data.iMode = int.Parse(dDataField["mode"].ToString());
						data.dblRoomTemperature = double.Parse(dDataField["roomTemp_oC"].ToString());
						data.dblSetTemperature = double.Parse(dDataField["setPoint"].ToString());

						aZones = (Newtonsoft.Json.Linq.JArray) dDataField["enabledZones"];
						data.bZone1 = (aZones[0].ToString() == "1");
						data.bZone2 = (aZones[1].ToString() == "1");
						data.bZone3 = (aZones[2].ToString() == "1");
						data.bZone4 = (aZones[3].ToString() == "1");

						AirConditioner.PostData(data);

						break;
				}
			}
			catch (Exception eException)
			{
				Logging.WriteDebugLogError("DeviceController.Data()", eException, "Unable to parse air conditioner data.");
			}

			response.result = 1;
			response.error = null;
			response.id = 0;

			return new ObjectResult(response);
		}
	}
}