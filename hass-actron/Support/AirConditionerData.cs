﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HMX.HASSActron
{
	public struct AirConditionerData
	{
		public bool bOn;
		public bool bESPOn;
		public int iMode;
		public int iFanSpeed;
		public double dblSetTemperature;
		public double dblRoomTemperature;
		public int iFanContinuous;
		public int iCompressorActivity;
		public string strErrorCode;
		public bool bZone1;
		public bool bZone2;
		public bool bZone3;
		public bool bZone4;
		public DateTime dtLastUpdate;
		public bool bPendingCommand;
		public bool bPendingZone;
	}
}