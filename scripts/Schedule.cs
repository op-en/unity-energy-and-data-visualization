using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class WeekdaysSelector{
	public bool Monday = false;
	public bool Tuesday = false;
	public bool Wenesday = false;
	public bool Thursday = false;
	public bool Friday = false;
	public bool Saturday = false;
	public bool Sunday = false;

	public bool IsChecked(DayOfWeek Day){
		if (Day == DayOfWeek.Monday)
			return Monday;

		if (Day == DayOfWeek.Tuesday)
			return Tuesday;

		if (Day == DayOfWeek.Wednesday)
			return Wenesday;

		if (Day == DayOfWeek.Thursday)
			return Thursday;

		if (Day == DayOfWeek.Friday)
			return Friday;

		if (Day == DayOfWeek.Saturday)
			return Saturday;

		if (Day == DayOfWeek.Sunday)
			return Sunday;

		return false;
	}

	public bool NoneSelected(){
		return !(Monday || Tuesday || Wenesday || Thursday || Friday || Saturday || Sunday);
	}
}

public class Schedule : DataSeries {

	[Header("Restart Scheduler")]
	[Space(10)]
	public string StartTime;
	public string StopTime;
	[SerializeField]
	double StartTimeEpoc = double.NaN;
	[SerializeField]
	double StopTimeEpoc = double.NaN;

	[Space(10)]



	public WeekdaysSelector Weekdays = new WeekdaysSelector();

	[Space(10)]
	public bool RedLetterDays = false;
	public bool ExludeRedLetterDays = false;

	DayOfWeek Day;
	double TimestampOfDay = double.NaN;




	// Use this for initialization
	void Start () {

		ParseTimes ();
		//Enabled = false;


		//print(SimulationTime.GetTimestampForDay(0));


		RegisterKeypoints ();
		base.Start ();


	}

	public bool IsActive(double ts){

		bool redletter = SimulationTime.IsRedLetterDay (ts);

		if (ExludeRedLetterDays && redletter)
			return false;

		if (RedLetterDays && redletter)
			return true;

		return Weekdays.IsChecked(SimulationTime.GetDayOfWeek (ts));
	}

	DataPoint CreateDataPoint (double ts,double value){
		DataPoint dp = new DataPoint();
		dp.Values = new double[1];
		dp.Values[0] = value;
		dp.Timestamp = ts;
		return dp;
	}

	double FindNextActiveDay(double ts){
		ts += 24*60*60;

		return FindActiveDay (ts,1);
	}
		

	double FindActiveDay(double ts,int searchdir){

		//Will never be active. 
		if (Weekdays.NoneSelected() && !(RedLetterDays && !ExludeRedLetterDays) ){			
			return double.NaN;
		}

		//Work backwards until we find the last active day. 
		while (!IsActive(ts)){
			ts += 24*60*60 * searchdir;
		}

		return SimulationTime.GetTimestampForDay (ts);
	}

	double FindPrevActiveDay(double ts){
		ts -= 24*60*60;

		return FindActiveDay(ts,-1);
	}

	public override DataPoint GetDataAt(double ts) {

		double ActiveDay;
		double tsTimestampOfDay;
		double start,stop;

		ActiveDay = FindActiveDay(ts,-1);

		//Will never be active. 
		if (double.IsNaN(ActiveDay)){			
			return CreateDataPoint(0,0);
		}

		start = ActiveDay + StartTimeEpoc;
		stop = ActiveDay + StopTimeEpoc;

		//We are on an active day and in an active period. 
		if (ts>= start && ts < stop)
			return CreateDataPoint(start,1);

		//We are after the active period
		if (ts >= stop)
			return CreateDataPoint(stop,0);
	

		//We are before the active period. 
		ActiveDay = FindPrevActiveDay(ActiveDay);

		//Will never be active. 
		if (double.IsNaN(ActiveDay)){			
			return CreateDataPoint(0,0);
		}

		stop = ActiveDay + StopTimeEpoc;

		return CreateDataPoint(stop,0);

	}

	public DataPoint GetPrevDataPoint(double ts) {

		double ActiveDay;
		double tsTimestampOfDay;
		double start,stop;

		ActiveDay = FindActiveDay(ts,-1);

		//Will never be active. 
		if (double.IsNaN(ActiveDay)){			
			return CreateDataPoint(0,0);
		}

		start = ActiveDay + StartTimeEpoc;
		stop = ActiveDay + StopTimeEpoc;

		if (ts > stop)
			return CreateDataPoint(start,1);

		ActiveDay = FindPrevActiveDay(ActiveDay);

		//Will never be active. 
		if (double.IsNaN(ActiveDay)){			
			return CreateDataPoint(0,0);
		}


		//We are after the active period
		if (ts > start) {
			
			return CreateDataPoint(ActiveDay + StopTimeEpoc,0);
		}
		 
		start = ActiveDay + StartTimeEpoc;

		return CreateDataPoint(start,1);

	}

	public DataPoint GetNextDataPoint(double ts) {

		double ActiveDay;
		double tsTimestampOfDay;
		double start,stop;

		ActiveDay = FindActiveDay(ts,1);

		//Will never be active. 
		if (double.IsNaN(ActiveDay)) {			
			return CreateDataPoint(0,0);
		}

		start = ActiveDay + StartTimeEpoc;
		stop = ActiveDay + StopTimeEpoc;

		//We are on an active day and in an active period. 
		if (ts < start )
			return CreateDataPoint(start,1);

		//We are after the active period
		if (ts < stop)
			return CreateDataPoint(stop,0);


		//We are before the active period. 
		ActiveDay = FindNextActiveDay(ActiveDay);

		//Will never be active. 
		if (double.IsNaN(ActiveDay)) {			
			return CreateDataPoint(double.PositiveInfinity,0);
		}

		start = ActiveDay + StartTimeEpoc;

		return CreateDataPoint(start,1);

	}



	double GetTimeOfNextEvent(double ts) {
		
		return GetNextDataPoint (ts).Timestamp;

	}

	double GetTimeOfPrevEvent(double ts) {

		return GetPrevDataPoint (ts).Timestamp;

	}

	override public List<DataPoint> GetPeriod(double From, double To,int extra) {

		List<DataPoint> Result = new List<DataPoint> ();
		DataPoint dp;
		double ts = From;

		//Add the extras.
		for (int i = 0; i < extra; i++) {
			ts = GetTimeOfPrevEvent (ts);
		}

		for (int i = 0; i < extra; i++) {

			dp = GetNextDataPoint (ts);

			ts = dp.Timestamp;

			if (double.IsNaN(ts))
				return Result;

			Result.Add (dp);

			if (ts <= To)
				i = -1;
		}

		return Result;
	}
		



	void ParseTimes(){
		StartTimeEpoc = ParseTime (StartTime);
		StopTimeEpoc = ParseTime (StopTime);
	}

	double ParseTime(string Str){

		char separator=':';

		string[] parts = Str.Split(separator);


		if (parts.Length == 3)
			return double.Parse(parts[0]) * 3600 + double.Parse(parts[1]) * 60 + double.Parse(parts[2]);
		else if (parts.Length == 2)
			return double.Parse(parts[0]) * 3600 + double.Parse(parts[1]) * 60;
		else if (parts.Length == 1)
			return double.Parse(parts[0]);

		return double.Parse(Str);
	}



	public void UpdateTimePropterties(){
		Day = SimulationTime.GetDayOfWeek (0);
		TimestampOfDay = SimulationTime.GetTimestampForDay (0);
	}


}
