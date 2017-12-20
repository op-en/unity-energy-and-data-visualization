using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class DataSeriesModifier : DataSeries {


	public enum Manipulation {
		sum,
		diff,
		min,
		max,
		mult,
		div
	};

	[Header("DataSeriesModifier properties")]
	public Manipulation operation;

	public List<DataSeries> SourceSeries;

	public BasicDataSeries inspect;
	public List<DataPoint> result;

	public DataPoint CurrentData;


	public void Start(){
		Subscription sub;

		base.Start ();

		foreach(DataSeries serie in SourceSeries) {

			sub = new Subscription ();
			sub.Source = serie;
			sub.Target = this;
			serie.Subscribe(sub);
		}

	}

	//
	public void Test() {
		double now = GameTime.GetInstance().time;

		//result = GetPeriod (now - 3*3600, now);

		DataPoint a, b;

		a = GetDataAt(now - 3 * 3600);
		b = GetDataAt(now);



		print(a.Values);
		print(b.Values);

	}

	virtual public void TimeDataUpdate(Subscription Sub, DataPoint data) {

		Subscribe(Sub);

		//if (NodeName == "AC&Ventilation") {
		//	print ("UPDATE: " + (data.Timestamp - Sub.LastTransmission.Timestamp) );
		//}

		Sub.LastTransmission = data;

		DataPoint point;

		point = GetDataAt(data.Timestamp);

		if (CurrentData.Equals (point))
			return;

		CurrentData = point;

		UpdateAllTargets(point);
	}

	//
	override public DataPoint GetDataAt(double ts) {
		BasicDataSeries Series = new BasicDataSeries(); ;


		if(SourceSeries.Count == 1)
			return ApplyModifiers(SourceSeries[0].GetDataAt(ts));

		foreach(DataSeries serie in SourceSeries) {
			Series.Data.Add(serie.GetDataAt(ts));
		}

		if(operation == Manipulation.sum) {
			return ApplyModifiers(Series.Sum());
		} else if(operation == Manipulation.div) {
			return ApplyModifiers(Series.Div());
		}

		print("Waring! Dataseries operation not implemented.");

		return null;
	}


	//
	override public List<DataPoint> GetPeriod(double From, double To,int extra) {

		BasicDataSeriesCollection result = new BasicDataSeriesCollection();
		BasicDataSeries Series;

		if(SourceSeries.Count == 1)
			return ApplyModifiers(SourceSeries[0].GetPeriod(From, To,extra));

		foreach(DataSeries serie in SourceSeries) {
			Series = new BasicDataSeries();
			Series.Data = serie.GetPeriod(From, To,extra);
			result.Collection.Add(Series);
		}

		if(operation == Manipulation.sum) {
			return ApplyModifiers(result.GetStaircaseSumOfSeries().Data);
		} else if(operation == Manipulation.div) {
			return ApplyModifiers(result.GetStaircaseDivOfSeries().Data);
		}

		print("Waring! Dataseries operation not implemented.");
		return null;
	}

	//TODO
	public override List<DataPoint> GetData() {
		return null;
	}

	//TODO
	public override void InsertData(DataPoint datapoint) {
	}
}
