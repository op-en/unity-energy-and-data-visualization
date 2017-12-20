using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class CompareDataPoint : IComparer<DataPoint>
{
	static IComparer<DataPoint> comparer = new CompareDataPoint();

	public int Compare(DataPoint x, DataPoint y)
	{
		if (x == y)    return 0;
		if (x == null) return -1;
		if (y == null) return 1;
		if (x.Timestamp > y.Timestamp)
			return 1;
		if (x.Timestamp < y.Timestamp)
			return -1;

		return 0;
	}
}

public class DataSeriesBuffer : DataSeries {
	public bool debug = false;

	[Header("Timeseries properties")]
	public double StartTime;
	public double StopTime;
	public bool Relative = false;


	[Space(10)]
	public int BufferMaxSize;
	public bool isAsync = false;
	public bool hasIntegral = false;
	public bool isTextSeries = false;
	public int ReloadLimit = 0;

	[Header("Status")]
	public bool Enabled = true;
	public bool Record = false;
	[Space(10)]
	public bool BufferValid = false;
	public int CurrentIndex;
	public double CurrentTimestamp;
	public string CurrentDate;
	public double[] CurrentValues;
	//public double CurrentIntegral; 
	public string CurrentText;
	public int CurrentSize;



	[Header("Buffer")]
//	public int Pointer = 0;
//	private int lastPointer = -1;
//	public List<DataPoint> Viewer = null;
	public List<DataPoint> Data = new List<DataPoint>();

	public bool AutoRequestBuffer = false;
	[Range(0,100)]
	public float AutoRequestTheshhold = 10;
	private double NextBufferUpdate;
	private double PrevBufferUpdate;
	public Period RequestedPerod;



	[Header("CSV file")]
	public TextAsset File;
	public string Separeator = ",";


	//private GameTime SimulationTime = null;

	// Use this for initialization
	public void Start () {

		base.Start ();

		CurrentIndex = -2;

		//Auto set if not set allready.
		//if (Server == null)
		//	Server = MQTT.GetInstance ();

		RequestData ();


		if (AutoRequestBuffer)
			RequestData ();


		//UpdateSim (SimulationTime.time);
		//List<DataPoint> [0] = new DataPoint ();

		RegisterKeypoints ();
	}


	
	// Update is called once per frame
	void Update () {
		//UpdateSim (SimulationTime.time);
	}

	override public bool UpdateSim(double time) {
		
		int index = CurrentIndex;
		CurrentIndex = GetIndex (time);



		//Nothing has hapended we are still at the same row in the buffer. 
		if (CurrentIndex == index)
			return false;

		//if (NodeName == "Electricity baseline") {
		//	print("Break");
		//}


		CurrentValues = GetCurrentValues();


		//Add keypoints 
		if (SimulationTime != null) {


			if (Data.Count - 1 > CurrentIndex) {
				//Old version
				SimulationTime.AddKeypoint (Data [CurrentIndex + 1].Timestamp, this);

				//New version
				SetNext (Data [CurrentIndex + 1].Timestamp);
			}

			if (CurrentIndex >= 0)
				SetPrev (Data [CurrentIndex].Timestamp);
		}

		//Update previous and next. 


		//Handle out of range 
		if (CurrentIndex == -1 ) {
			CurrentTimestamp = double.NaN;
			CurrentDate = "Out of range";


			TrimDatapoints ();
			return false;

		}

		//Update editor properties
		CurrentTimestamp = Data [CurrentIndex].Timestamp;
		CurrentDate = SimulationTime.TimestampToDateTime(CurrentTimestamp).ToString("yyyy-MM-dd HH:mm:ss");


		//Send datapoint.
		if (Enabled == true) {

			//Copy
			DataPoint Point = Data [CurrentIndex].Clone ();

			//Send
			base.UpdateAllTargets (Point);

		}

		TrimDatapoints ();
		return true;
	}

	override public void UpdateAllTargets(DataPoint Data) {

		//Debug.Log ("Update");
		//Debug.Log (Data);

		if (Record)
			AddPoint (Data);
		//if (Enabled)
		//	base.UpdateAllTargets (Data);
		
	}

	public void AddPoint(DataPoint NewData) {
		int index;

		//Skip if not inside buffer. 
		if (NewData.Timestamp < this.getStartTime () || NewData.Timestamp > this.getStopTime ()) {
			//If limits are both zero we assume its not usex
			if (StartTime != 0 && StopTime != 0)
				return;

		}

		index = Data.BinarySearch (NewData, new CompareDataPoint() );
		//Debug.Log ("Binary search: " + index.ToString ());

		//Insert
		if (index < 0)
		{
			index = ~index;

			//if (index > List<DataPoint>.Count)
			//	index = List<DataPoint>.Count;
			
			//Debug.Log("INSERT DATA AT:" +  (index).ToString() );
			Data.Insert(index, NewData);
		}
		//Replace
		else if (index >= 0)
		{
			Data.RemoveAt(index);
			Data.Insert(index, NewData);
			//Debug.Log("REPLACE DATA");

		}

		//Maintain size limitation. 
		if (BufferMaxSize != 0 && Data.Count > BufferMaxSize)
			Data.RemoveRange (BufferMaxSize, BufferMaxSize - Data.Count);


		CurrentSize = Data.Count;

	}


	void AddPoints(DataPoint[] DataSet) {

		foreach(var item in DataSet )
		{
			AddPoint (item);
		}

		UpdateSim (SimulationTime.time);
			
	}

	void AddPoints(List<DataPoint> DataSet) {

		foreach(var item in DataSet )
		{
			AddPoint (item);
		}

		UpdateSim (SimulationTime.time);

	}

	public double getStartTime() {
		if (!Relative)
			return StartTime;

		if (SimulationTime == null)
			SimulationTime = GameTime.GetInstance ();

		return SimulationTime.time + StartTime;
		
	}




	public double getStopTime() {
		if (!Relative)
			return StopTime;

		return SimulationTime.time + StopTime; 
	}


	public bool RequestData () {
		double missing,stoptime;
		//if (Server == null)
		//	return false;

		//Server.Get(Name,StartTime,Absolute,BufferMaxSize);

	

		ServerObject server;

		foreach (Subscription sub in Sources) {

			if (sub.Source is ServerObject) {
				server = (ServerObject)sub.Source;

				//print("Is server");


				//If the buffer is empty request all. 
				if (RequestedPerod.Enabled == false) {

					if(debug) 
						print("Requesting entire period");
					server.GetPeriod (sub.Topic, getStartTime (), getStopTime (), this);
					RequestedPerod.FromTime = getStartTime ();
					RequestedPerod.ToTime = getStopTime ();
					RequestedPerod.Enabled = true;
				} else {
					stoptime = getStopTime ();
					missing =  stoptime - BufferEnd ();



					if (missing > 0) {
						server.GetPeriod (sub.Topic, BufferEnd (), stoptime, this);
						RequestedPerod.ToTime = stoptime;
					}

				}

			}
		}

		return true;
	}

	//Get index based on a timestamp
	public int GetIndex(double ts) {

		for (int i = Data.Count - 1; i >= 0 ; i--)
		{
			//print(TimeStamps [i].ToString ("F4") + " > " + ts.ToString ("F4"));

			if ((Data[i].Timestamp + TimeOffset) <= ts )
			{
				return i;
			}
		}

		return -1;
	}
		
	//Get the current value form the timeseries based on the current gametime. 
	public double GetCurrentValue () {
		double now = SimulationTime.time;

		int i = GetIndex (now);

		if (i == -1)
			return double.NaN;

		return ApplyModifiers(Data[i]).Values[0];
	}
		


    override public DataPoint GetDataAt(double ts)
    {
        int i = GetIndex(ts);

        if (i == -1)
            return null;

        return ApplyModifiers(Data[i]);
    }



    //TODO 
    public double InterpolateCurrentValue() {
		double now = (double)SimulationTime.time;

		return Data[GetIndex(now)].Values[0];
	}


	public void LoadFromCVSFile() {

		if (File == null)
			return;

		//string fileData  = System.IO.File.ReadAllText(FileName);
		string[] lines = File.text.Split("\n"[0]);

        Columns.Clear();
		Columns.AddRange((lines[0].Trim()).Split(Separeator[0]));

        Units.Clear();
		Units.AddRange((lines[1].Trim()).Split(Separeator[0]));
 

		NodeName = File.name;
		Relative = false;


		//TODO fill in when we have a buffer already... 
		//if (BufferValid == true)
		//	return;

		//Reload everything. 
		Data.Clear ();
		double tsmin = double.PositiveInfinity, tsmax=0;

		for (int i = 2; i < lines.Length; i++) {
			string[] Values = (lines[i].Trim()).Split(Separeator[0]);
			DataPoint NewData = new DataPoint();
			NewData.Timestamp = double.Parse( Values[0]) ;

			print (Values);

			NewData.Values = new double[Values.Length-1]; 

			for (int c = 1; c < Values.Length; c++) {
				
//				Debug.Log (data);
//				Debug.Log (data.Values[c]);
//				Debug.Log (Values [c]);

				NewData.Values[c-1] = double.Parse (Values [c]);
			}

			Data.Add (NewData);

			//Save min and max. 
			if (NewData.Timestamp > tsmax)
				tsmax = NewData.Timestamp;
			if (NewData.Timestamp < tsmin)
				tsmin = NewData.Timestamp;
		}

		StartTime = tsmin;
		StopTime = tsmax;

		BufferValid = true;



		
	}

	public bool TsWithinBuffer(double TimeStamp) {
	
		double start, stop;

		start = getStartTime ();
		stop = getStopTime ();

		if (TimeStamp > stop)
			return false;
		if (TimeStamp < start)
			return false;

		return true;
			
	}

	//Make sure that the number of datapoints does not exceed the max buffer size variable. 
	public void TrimDatapoints() {
		int excess,start,stop;
		DataPoint current = null;


		if (getStartTime () != 0 && getStopTime () != 0) {
		
			start = GetIndex (getStartTime ());
			stop = GetIndex (getStopTime ());

			if (CurrentIndex != -1 && CurrentIndex < Data.Count)
				current = Data [CurrentIndex];


			//Remove points outside
			if (start > 1) {
				Data.RemoveRange (0, start - 1);
				CurrentIndex -= start - 1;
			}

			if (stop > Data.Count - 2)
				stop = Data.Count - 1;
			

			Data.RemoveRange (stop + 1, Data.Count - 1 - stop);

		}






		//If zero or less then no restrictions apply
		if (BufferMaxSize < 1) {
			CurrentSize = Data.Count;
			return;
		}

		excess = Data.Count - BufferMaxSize;

		if (excess > 0)
			Data.RemoveRange (0, excess);

		CurrentSize = Data.Count;
	}
		

	override public List<DataPoint> GetPeriod(double From, double To,int extra) {
		int iFrom, iTo;

		iFrom = GetIndex (From);
		iTo = GetIndex (To);

		iFrom -= extra;
		iTo += extra;
	

		if (iFrom < 0)
			iFrom = 0;
		if (iTo < 0)
			iTo = 0;

		return GetRange(iFrom, iTo - iFrom + 1);

	}

	//
	public List<DataPoint> GetRange(int index, int count) {

		List<DataPoint> rawdata, newdata;

		if(index >= Data.Count)
			index = Data.Count - 1;

		if((count + index) > Data.Count)
			count = Data.Count - index;

		if(index < 0)
			return new List<DataPoint>();


		//Debug.Log ("I: " + index.ToString());
		//Debug.Log ("C: " + count.ToString());

		rawdata = Data.GetRange(index, count);
		newdata = ApplyModifiers(rawdata);

		return newdata;

	}

	//Get list of datapoints
	public override List<DataPoint> GetData() {
		return Data;
	}

	//Set list of datapoints
	public override void InsertData(DataPoint datapoint) {
		//Data.Add(datapoint);
		AddPoint(datapoint);

		//var index = Data.BinarySearch(datapoint);
		//if (index < 0) index = ~index;
		//Data.Insert(index, datapoint);
	}

	//Clear list of datapoints
	public void Clear() {
		Data.Clear ();
	}


	override public DataPoint GetFirst(){ 
		if (Data.Count > 0)
			return Data [0];
		else
			return null;
	}

	override public DataPoint GetLast(){ 
		int n = Data.Count;
		if (n > 0)
			return Data [n-1];
		else
			return null;

	}

	override public bool CopyPeriod(DataSeries Series,double From,double To,int extra){
		List<DataPoint> Points = Series.GetPeriod(From,To,extra);

		AddPoints(Points);

		return true;
	}

	public double BufferBegin()
	{
		if (Data.Count > 0)
			return Data [0].Timestamp;

		return double.NaN;
	}

	public double BufferEnd()
	{
		if (Data.Count > 0)
			return Data [Data.Count -1].Timestamp;

		return double.NaN;
	}


	public double GetBufferState()
	{
	
		double start, stop, begin, end, missing_start, missing_end, missing, total,real,future;

		start = getStartTime ();
		stop = getStopTime ();

		begin = BufferBegin ();
		end = BufferEnd ();

		missing_start = start - begin;
		missing_end = end - stop;

		if (missing_start < 0)
			missing_start = 0;

		if (missing_end < 0)
			missing_end = 0;

		missing = missing_start + missing_end;

		real = SimulationTime.RealWorldTime;

		future = real - stop;

		if (future < 0)
			future = 0;

		//Expected buffer lenght when full is from start to stop minus the part of the buffer that is in the future. 
		total = stop - start - future;

		return 100 * (missing_start + missing_end) / total;

	}

	public void CalculateNextBufferUnderrun(float percentage){
		double start, stop, begin, end, underrunlimit, missing_start, missing_end, missing, total,real,future;


	 
		start = getStartTime ();
		stop = getStopTime ();

		real = SimulationTime.RealWorldTime;

		future = real - stop;

		if (future < 0)
			future = 0;

		//Expected buffer lenght when full is from start to stop minus the part of the buffer that is in the future. 
		total = stop - start - future;


		underrunlimit = total * percentage;

		begin = BufferBegin ();
		end = BufferEnd ();

		NextBufferUpdate = end + underrunlimit;
		PrevBufferUpdate = begin - underrunlimit;


	}
		

}
