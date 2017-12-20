using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;



public class GameTime : SimulationObject {
	private static GameTime _instance;
	public static GameTime GetInstance() {
		return _instance;
	}

	public enum TimeContext { None, GameTime, RealWorldTime };

	[Serializable]
	public class KeyAction:DataEvent
	{
		public SimulationObject target;
	}

	public class CompareKeyAction : IComparer<KeyAction>
	{
		static IComparer<KeyAction> comparer = new CompareKeyAction();

		public int Compare(KeyAction x, KeyAction y)
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


    public double RealWorldTime;
    [Space(10)]
    [HideInInspector]
    public float simulationDeltaTime;
    [Space(10)]
    
	[Header("Clock")]
    public double StartTime;
	public bool StartInRealtime;
	public bool StopAtRealtime;
	public double offset;
	public double time = Double.NaN;
	public double TargetTime = Double.NaN;
	public double TargetOffset = Double.NaN;
	public string CurrentDate;
    public double _skipToOffset = -1;

    DateTime dateTimeCurrent;
    DateTime dateTimeLastUpdate;

    double lastupdate=0;
	public List<KeyAction> KeyActions = new List<KeyAction>();
	public List<SimulationObject> SimulationObjects = new List<SimulationObject>();
	[SerializeField]
	private SimulationObject closestPrev = null;
	[SerializeField]
	private SimulationObject closestNext = null;

	[Range(0.0f, 100.0f)]
	public float VisualTimeScale = 1.0f;

	[Range(0.0f, 10000.0f)]
	public float SimulationTimeScaleFactor = 1.0f;

	public bool LockScales;



	[Space(10)]
	public List<double> Hollidays = new List<double>();
	public bool RedLetterDay = false;
	public bool Weekend = false;

	[Space(10)]

	private float prevVisualTimeScale,prevSimulationTimeScale;
	private float normalVisualTimeScale,normalSimulationTimeScale;
	private double target = double.NaN;
	public bool speeding = false;

	public void Awake () {
		base.Awake ();
		RegisterKeypoints ();

		UpdateRealWorldTime ();

		if (StartInRealtime) {
			SetTime (RealWorldTime);
			SetTargetTime (RealWorldTime);
		} else {
			SetTime (StartTime);
			SetTargetTime (StartTime);
		}

		//First instance.
		if (_instance == null)
			_instance = this;

		prevVisualTimeScale = VisualTimeScale;
		prevSimulationTimeScale = SimulationTimeScaleFactor;

	}

	// Use this for initialization
	public void Start () {

		FindClosest (time);
        
	}

	void UpdateRealWorldTime(){
		TimeSpan t = DateTime.UtcNow - new DateTime (1970, 1, 1);
		RealWorldTime = t.TotalSeconds;
	}

    //Add a reference to an object that implements simulationObject. The UpdateSim function of the passed object will be called when provided timestamp is passed. If provided timestamp is in history, the updatesim will get called immmeditely (kind of).
	public bool AddKeypoint(double TimeStamp,SimulationObject target)
	{

	//	print ("DEPRICTED");
		return false;

        //If trying to add a key action that should already have ben run, then run it immediately instead and return
        //if (TimeStamp < time) {
        //    target.UpdateSim(TimeStamp);
        //    return false;
        //}

        KeyAction keypoint = new KeyAction ();

		keypoint.Timestamp = TimeStamp;
		keypoint.target = target;
		
		KeyActions.Add (keypoint);
		KeyActions.Sort (new CompareKeyAction() );

		return true;
	}

	//This changes time and all assosiated properties. 
	void SetTime(double ts){
		offset = ts - Time.time - StartTime;
		time = ts;
		lastupdate = Time.time;
		RedLetterDay = IsRedLetterDay ();
		Weekend = IsWeekend ();
		CurrentDate = TimestampToDateTime(time).ToString("yyyy-MM-dd HH:mm:ss");
	}

	void SetTargetTime(double ts){
		TargetOffset = ts - Time.time - StartTime;
	}

	//This is the simulation time that we want to target at this moment.
	double CalculateTargetTime(){


		//double tmpoffset,tmptargettime;

		//Calculate difference between simulation and visual timescales and apply to offset.
		double now = Time.time;

		//The delta since last update in terms of the visual timescale. 
		double delta = now - lastupdate;

		//Add the time that should have elapsed.
		TargetOffset += (delta/VisualTimeScale * (SimulationTimeScaleFactor - VisualTimeScale));

		//This is the time that we should be at. 
		TargetTime = StartTime + TargetOffset + Time.time;

		if (StopAtRealtime && TargetTime > RealWorldTime) {
			TargetTime = RealWorldTime;
			TargetOffset = RealWorldTime - Time.time - StartTime;
		}

		return TargetTime;
	}

	void HandleSliders(){
		//Did scales change? 
		if (prevVisualTimeScale != VisualTimeScale && LockScales)
			SimulationTimeScaleFactor = VisualTimeScale;
		else if (prevSimulationTimeScale != SimulationTimeScaleFactor && LockScales)
			VisualTimeScale = SimulationTimeScaleFactor;

		prevVisualTimeScale = VisualTimeScale;
		prevSimulationTimeScale = SimulationTimeScaleFactor;
	}
		

	// Update is called once per frame
	void Update () {

		//Update realworld time.
		UpdateRealWorldTime();

		//Check if speed sliders changed
		HandleSliders ();

		//Calculate the time that we should be at. 
		CalculateTargetTime ();



		double oldtime = time;


        //Do all key actions requiered until the new time
        //DoKeyActions(new_time);
		ProgressSimulation (time,TargetTime,0,false);


		if (StopAtRealtime && time >= RealWorldTime) {
			Time.timeScale = 1;
		} else {
			Time.timeScale = VisualTimeScale;
		}

        simulationDeltaTime = (float) (time - oldtime);

        dateTimeLastUpdate = dateTimeCurrent;
        dateTimeCurrent = TimestampToDateTime(time);
        //CurrentDate = dateTimeCurrent.ToString("yyyy-MM-dd HH:mm:ss");

        
	}

	override public bool UpdateSim(double time) {
		
		if (!double.IsNaN(target)){
			print ("Returning to normal timescale");
			print (normalVisualTimeScale);
			VisualTimeScale = normalVisualTimeScale;
			prevVisualTimeScale = normalVisualTimeScale;
			SimulationTimeScaleFactor = normalSimulationTimeScale;
			prevSimulationTimeScale = normalSimulationTimeScale;

			SetNext(double.PositiveInfinity);

			target = double.NaN;
			speeding = false;

			return true;
		
		}

		return false;
	}

	//Sets a temporary speed that is mainained until a certain point in time is reached. 
	public void SpeedTo(double ts, float VisualSpeedFactor, float SimulationSpeedFactor) {


		if (double.IsNaN (target)) {
			normalVisualTimeScale = VisualTimeScale;
			normalSimulationTimeScale = SimulationTimeScaleFactor;

		}

		speeding = true;

		target = ts;
		VisualTimeScale = VisualTimeScale * VisualSpeedFactor;
		SimulationTimeScaleFactor = SimulationTimeScaleFactor * SimulationSpeedFactor;
		prevVisualTimeScale = VisualTimeScale;
		prevSimulationTimeScale = SimulationTimeScaleFactor;

		SetNext (ts);
	
	}


	public void SimulateForward(double ts){
		SimulateTo (time+ts);
	}

	public void SimulateTo(double ts){
		SetTargetTime (ts);
		ProgressSimulation (time,ts,0,false);
	}

	public void JumpForward(double ts){
		JumpTo (time+ts);
	}

	public void JumpTo(double ts){
		SetTargetTime (ts);
		ProgressSimulation (time,ts,0,true);
	}
		
	public void JumpToRealtime(){
		TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
		RealWorldTime = t.TotalSeconds;


		SetTargetTime (RealWorldTime);
		ProgressSimulation (time,RealWorldTime,0,true);
	

		if (StopAtRealtime) {

			VisualTimeScale = 1;
			Time.timeScale = 1;
			SimulationTimeScaleFactor = 1;
			prevVisualTimeScale = VisualTimeScale;
			prevSimulationTimeScale = SimulationTimeScaleFactor;
		}
	}

	public string GetDay(int i){
		DateTime date = TimestampToDateTime (time + (86400 * i));
		return date.DayOfWeek.ToString();
	}

	public DayOfWeek GetDayOfWeek(int i){
		DateTime date = TimestampToDateTime (time + (86400 * i));
		return date.DayOfWeek;
	}

	public DayOfWeek GetDayOfWeek(double ts){
		DateTime date = TimestampToDateTime (ts);
		return date.DayOfWeek;
	}
	public double GetTimestampForDay(double ts){
		DateTime date = TimestampToDateTime (ts);
		TimeSpan span = (date.Date - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());

		//return the total seconds (which is a UNIX timestamp)
		return (double)span.TotalSeconds;
	}

	public double GetTimestampForDay(int i){
		DateTime date = TimestampToDateTime (time + (86400 * i));
		TimeSpan span = (date.Date - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());

		//return the total seconds (which is a UNIX timestamp)
		return (double)span.TotalSeconds;
	}

	public bool IsRedLetterDay(double timestamp)
	{
		DateTime date = TimestampToDateTime (timestamp);

		if (date.DayOfWeek == DayOfWeek.Sunday)
			return true;

		foreach (double ts in Hollidays) {
			if (TimestampToDateTime (ts).Date == date.Date)
				return true;
		}

		return false;
	}

	public bool IsRedLetterDay()
	{
		return IsRedLetterDay (time);
	}


	// NEW Interface or callback on specific times. 
	public bool register(SimulationObject Obj){

		if (SimulationObjects.Contains(Obj))
			return false;

		SimulationObjects.Add (Obj);

		UpdatePrev (Obj);
		UpdateNext (Obj);

		return true;
	}

	void UpdateAllExpiered(){
		foreach (SimulationObject Obj in SimulationObjects) {
			if (Obj.GetNext () <= time || Obj.GetPrev () > time)
				Obj.UpdateSim (time);
		}

		closestPrev = FindPrevClosest (time);
		closestNext = FindNextClosest (time);

	}

	// oldtime is where the simulation is at now
	// newtime is the time we want the simulation to progress to
	// maxtime (not implemented yet) is the longst realworld time that the simulation update is allowed to take. 
	// The function will return the time it progressed to is this one is set and the progression is continues on the next frame. 
	// skipto decides if we progress by updating on all keypoints or just jump to the the new time. 
	// If a playback of simulation data or realtime data occurs we can jump. If we however is generating simulation data we can not skip since it will break the simulation. 
	private double ProgressSimulation(double oldtime, double newtime,double maxtime ,bool skipto){

		bool forward = ((newtime - oldtime) > 0);
		double keytime;
		int i=0;
			

		if (skipto) {
			SetTime (newtime);
			UpdateAllExpiered ();
			return newtime;
		}
			
		if (forward) { 
			
			while (closestNext != null && closestNext.NeedUpdate (newtime)) {
				i++;

				if (i > 100) {
					print ("break");
				}

				keytime = closestNext.GetNext ();

				if (keytime < time)
					keytime = time;

				if (keytime > newtime)
					break;
				
				SetTime (keytime);
				closestNext.ResetNext ();
				closestNext.UpdateSim (time);
				closestNext = FindNextClosest (time);
			}
			closestPrev = FindPrevClosest (time);

		} else {
			while (closestPrev != null && closestPrev.NeedUpdate (newtime)) {

				i++;

				if (i > 100) {
					print ("break");
				}

				keytime = closestPrev.GetPrev ();

				if (keytime > time)
					keytime = time;

				if (keytime < newtime)
					break;

				SetTime (keytime);
				closestNext.ResetPrev ();
				closestPrev.UpdateSim (time);
				closestPrev = FindPrevClosest (time);
			}

			closestNext = FindNextClosest (time);
		}
			
		SetTime (newtime);


		return newtime;
	}

	public void FindClosest (double ts){
		closestNext = FindNextClosest (time);
		closestPrev = FindPrevClosest (time);
	}

	public SimulationObject FindNextClosest (double ts){
		double Current, ClosestTs = double.PositiveInfinity;
		SimulationObject Closest = null;

		foreach (SimulationObject Obj in SimulationObjects) {
		
			Current = Obj.GetNext ();

			if (Current < ClosestTs) {
				ClosestTs = Obj.GetNext ();
				Closest = Obj;
			}
		}

		return Closest;
	}

	public SimulationObject FindPrevClosest (double ts){
		double Current, ClosestTs = double.NegativeInfinity;
		SimulationObject Closest = null;

		foreach (SimulationObject Obj in SimulationObjects) {

			Current = Obj.GetPrev ();

			if (Current > ClosestTs) {
				ClosestTs = Obj.GetPrev ();
				Closest = Obj;
			}
		}

		return Closest;
	}


	public bool UpdatePrev(SimulationObject obj){

		//In the furture 
		//if (obj.GetPrev () > time)
		//	return false;

		if (closestPrev == null || obj.GetPrev () > closestPrev.GetPrev ())
			closestPrev = obj;

		return true;
	}

	public bool UpdateNext(SimulationObject obj){

		//In the past 
		//if (obj.GetNext () < time)
		//	return false;

		if (closestNext == null || obj.GetNext () < closestNext.GetNext ())
			closestNext = obj;

		return true;
	}

	private void DoKeyActions(double newtime) { 

		KeyAction ka = null;
		double oldtime,delta;

        //TimeProfiler tp = new TimeProfiler("Do key actions", true);

        while (KeyActions.Count > 0 ) {
            //All remaning are in the future (assuming that the list is sorted). 
            if (KeyActions[0].Timestamp > newtime) {
                break;
            }

            //tp.IncreaseCounter(true);
 
			ka = KeyActions[0];

            //Set gameTime to the time for the key action. In case game time are referenced somewhere when executing UpdateSim.
			oldtime = time;
            time = ka.Timestamp;

			delta = time - oldtime;

			if (delta < 0)
				print ("!!!!!!!!!!!!! Game time delta:" + delta);

            //Execute the event. 
            ka.target.UpdateSim(time);



            //Remove
            KeyActions.Remove (ka);

		}
        //tp.MillisecondsSinceCreated(true);

        return;
	}

	private double DateTimeToTimestamp(DateTime value)
	{
		//create Timespan by subtracting the value provided from
		//the Unix Epoch
		TimeSpan span = (value - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());

		//return the total seconds (which is a UNIX timestamp)
		return (double)span.TotalSeconds;
	}

	////Convert timestring from schedule to timestamp
	//public double ScheduleToTimestamp(string hourMinute) {
	//	DateTime curTime = TimestampToDateTime(time);
	//	return ScheduleToTS(curTime.Year, curTime.Month, curTime.Day, hourMinute);
	//}

	////
	//public double ScheduleToTimestamp(int dOff, string hourMinute) {
	//	DateTime curTime = TimestampToDateTime(time);
	//	return ScheduleToTS(curTime.Year, curTime.Month, curTime.Day + dOff, hourMinute);
	//}

	////
	//public double ScheduleToTimestamp(int mOff, int dOff, string hourMinute) {
	//	DateTime curTime = TimestampToDateTime(time);
	//	return ScheduleToTS(curTime.Year, curTime.Month + mOff, curTime.Day + dOff, hourMinute);
	//}

	//Returns a timestamp derived from year, month, day and an hourminute string
	public double ScheduleToTS(int year, int month, int day, string hourMinute) {
		string[] timeParse = hourMinute.Split(':');
		int hour = int.Parse(timeParse[0]);
		int minute = int.Parse(timeParse[1]);

		//create Timespan by subtracting the value provided from
		//the Unix Epoch
		DateTime value = new DateTime(year, month, day, hour, minute, 0).ToLocalTime();
		TimeSpan span = (value - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());

		//return the total seconds (which is a UNIX timestamp)
		return (double)span.TotalSeconds;
	}

	//Returns a timestamp derived from an hour-minute string and a day offset
    //The returned timestamp is based on the hour-minute from the day of the referenceStamp
    //offset by dayoffset.
	public double ScheduleToTS(double referenceStamp, int dayOffset, string hourMinute) {
		DateTime curTime = TimestampToDateTime(referenceStamp); //Ok we want a stamp from this day (+- dayoffset) corresponding to the hour:minute string
		curTime = curTime.AddDays(dayOffset);
		return ScheduleToTS(curTime.Year, curTime.Month, curTime.Day, hourMinute);
	}

	public DateTime TimestampToDateTime(double value)
	{
		//create Timespan by subtracting the value provided from
		//the Unix Epoch
		DateTime date = ( new DateTime(1970, 1, 1, 0, 0, 0, 0) + new TimeSpan(0,0,(int)value));
		//return the total seconds (which is a UNIX timestamp)
		return date;
	}

	public double GetFirstTimeOfDay(double ts){
		DateTime day = TimestampToDateTime(ts).Date;
		return DateTimeToTimestamp (day);
	}

	public double GetFirstTimeOfDay(){
		return GetFirstTimeOfDay (time);
	}

	public double GetFirstTimeOfDay(int i){
		return GetFirstTimeOfDay (time + (3600.0*24 * i));
	
	}

	public DateTime GetDateTime()
	{
		return TimestampToDateTime(time);
	}

	//
	public double GetTotalSeconds() {

		return (double)time;
	}

	//
	public string GetViewTime() {

		return TimestampToDateTime(time).ToString("HH:mm");
	}

	//
	public float GetMinutes() {
		return TimestampToDateTime(time).Minute + TimestampToDateTime(time).Hour * 60;
	}

	public void Offset(float delta)
	{
		offset = offset + delta;
	}

	public void SetStartTime(double NewTime) {
		offset = NewTime;
	}



	public int GetYear() {
		return GetDateTime().Year;
	}

	//
	public int GetMonth() {
		return GetDateTime().Month;
	}

	//
	public int GetDay() {
		return GetDateTime().Day;
	}

	//
	public string GetTimeWithFormat(string format) {
		return TimestampToDateTime(time).ToString(format);
	}

    public int GetNewMonths() {
        if(dateTimeCurrent.Year == 1 || dateTimeLastUpdate.Year == 1) {
            return 0;
        }
        return (dateTimeCurrent.Year - dateTimeLastUpdate.Year) * 12 + (dateTimeCurrent.Month - dateTimeLastUpdate.Month);
    }

    public bool IsWeekend() {
		DateTime date = TimestampToDateTime (time);

		if (date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Saturday)
			return true;

		foreach (double ts in Hollidays) {
			if (TimestampToDateTime (time).Date == date.Date)
				return true;
		}

		return false;
    }

    public bool IsWeekendTomorrow() {
		DateTime date = TimestampToDateTime (time + 8640);

		if (date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Saturday)
			return true;

		foreach (double ts in Hollidays) {
			if (TimestampToDateTime (time + 8640).Date == date.Date)
				return true;
		}

		return false;
    }



}

