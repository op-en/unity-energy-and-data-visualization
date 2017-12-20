using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

//
public class DataSeries : DataModifier {

	[Header("Interpolation parameters")]
	public int meterindex = 1;
	public int rateindex = 0;
	public double RateMeterConversionFactor = 1 / 3600;



	public void Awake(){
		base.Awake ();
	}


	public void Start(){
		base.Start ();

	}

	public static DataSeries GetSeriesByName(string name){
		

		DataSeries[] series = FindObjectsOfType(typeof(DataSeries)) as DataSeries[];
		foreach (DataSeries serie in series) {
			if (serie.transform.name == name || serie.NodeName == name)
				return serie;
		}

		return null;

	}

	//
	virtual public List<DataPoint> GetPeriod(double From, double To,int extra) {
		return null;
	}

	public List<DataPoint> GetPeriod(double From, double To) {
		return GetPeriod( From,  To, 0);
	}


		

	//
	public virtual List<DataPoint> GetData() {
		return null;
	}

	//
	public virtual void InsertData(DataPoint datapoint) {
		
	}

	public virtual void InsertData(List<DataPoint> datapoint) {
		
	}

	public double InterpolateDailyConsumption(int day) 
	{
		//Calculate first and last time on the day.
		double Starts,Ends,StartValue,EndValue;




		Starts = SimulationTime.GetFirstTimeOfDay(day);
		Ends = SimulationTime.GetFirstTimeOfDay(day+1);

		StartValue = InterpolateValueAt (Starts);
		EndValue = InterpolateValueAt (Ends);

		return EndValue - StartValue;
	}

	public double InterpolateValueAt(double time)
	{
		DataPoint data = GetDataAt (time);

		if (data == null)
			return double.NaN;

		if (data.Timestamp == time)
			return data.Values [meterindex];

		double DeltaTime = time - data.Timestamp;

		return data.Values [meterindex] + DeltaTime * data.Values [rateindex] * RateMeterConversionFactor;

	}


	//
	virtual public DataPoint GetDataAt(double ts) {
		return null;
	}

	//
	public double[] GetCurrentValues() {
		double now = SimulationTime.time;

		DataPoint dp = GetDataAt (now);

		if (dp == null)
			return null;

		return dp.Values;
	}

	//
	public double[] GetValuesAt(double ts) {
		return GetDataAt(ts).Values;
	}

	virtual public bool CopyPeriod(DataSeries Series,double From,double To,int extra){
		
		print("Warning: CopyPeriod is ignorded since data series: " + NodeName + " is read only!");
		return false;
	}

	public bool CopyPeriod(DataSeries Series,double From,double To){
		return CopyPeriod (Series, From, To, 0);
	}

	//
	public List<DataPoint> ApplyModifiers(List<DataPoint> points) {
		List<DataPoint> modified_data;

		modified_data = new List<DataPoint>();

		//if (TimeOffset == 0)
		//	return rawdata;

		foreach(DataPoint point in points) {
			modified_data.Add(ApplyModifiers(point));
		}

		return modified_data;
	}

	virtual public DataPoint GetFirst(){ 
		return null;
	}

	virtual public DataPoint GetLast(){ 
		return null;
	}

	public double FirstTimestamp(){ 
		DataPoint dp = GetFirst ();

		if (dp == null)
			return Double.NaN;
		
		return dp.Timestamp;
	}

	public double LastTimestamp(){ 
		DataPoint dp = GetLast ();

		if (dp == null)
			return Double.NaN;

		return dp.Timestamp;
	}
}
