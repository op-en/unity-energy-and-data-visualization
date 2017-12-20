using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Manipulation  {

    public string Name;
    public double From = double.NaN;
    public double To = double.NaN;
    public bool Active;
    public bool Applied = true;


    
    public enum DataType
    {
        RateOnly,
        RateCounter
    }

    [Header("Setting")]
    public DataType Type;

    //Rescales the datastream. 
    public double[] rescale;
    //A relative offset removes a certain percentage of the original data from the result (result - originaldata * relativeoffset). 
    public double[] relativeoffsets;
    //Absoule offset removes the specified value from the results (original_data - absolute_offsets).
    public double[] absoluteoffsets;

    [Header("State")]
    public double RateOffset = 0;
    public double CounterOffset = 0;
    public double LastUpdate = double.NaN;

    [Tooltip("The time factor used by the counter. If power as rate and Wh as counter the time factor will be 3600 as the hour in Wh is 3600s.")]
    public double TimeFactor = 3600;

    public bool isActive(double timestamp)
    {
        if (!Active)
            return false;

        if (!double.IsNaN(From) && timestamp < From)
            return false;

        if (!double.IsNaN(To) && timestamp > To)
            return false;

        return true;
    }

    public void Activate(double now)
    {
        From = now;
        To = double.NaN;
        Active = true;
    }

    public void DeActivate(double now)
    {
        From = now;
        To = now;
        Active = false;
    }



    public double[] RateCalculateOffset(double timestamp, double[] data, double[] temp_res)
    {
        double[] offset = new double[data.Length];

        for (int i = 0; i < offset.Length; i++)
        {
            offset[i] = 0;
        }

        if (!isActive(timestamp))
            return offset;

        for (int i = 0; i < offset.Length; i++)
        {
            if (rescale.Length < i)
                offset[i] += temp_res[i] * (rescale[i] - 1);

            if (relativeoffsets.Length < i)
                offset[i] += data[i] * relativeoffsets[i];

            if (absoluteoffsets.Length < i)
                offset[i] += absoluteoffsets[i];

        }


        return offset;
    }

    public void AddOffsets(DataPoint Original, DataPoint Result)
    {
        double[] offsets;

        if (Type == DataType.RateOnly)
            offsets = RateCalculateOffset(Result.Timestamp, Original.Values, Result.Values);
        else if (Type == DataType.RateCounter)
            offsets = RateCounterCalculateOffset(Result.Timestamp, Original.Values, Result.Values);
        else
            return;

        for (int i = 0; i < offsets.Length;i++)
        {
            Result.Values[i] += offsets[i];
        }

    }


    public double[] RateCounterCalculateOffset(double timestamp, double[] data, double[] temp_res)
    {
        double[] offset = new double[data.Length];

        if (data.Length < 1)
            return offset;

        for (int i = 0; i < offset.Length; i++)
        {
            offset[i] = 0;
        }

		//if (!Applied)
		//	return;


        if (isActive(timestamp))
        {
            offset[0] += temp_res[0] * (rescale[0] - 1);
            offset[0] += data[0] * relativeoffsets[0];
            offset[0] += absoluteoffsets[0];
        }

        if (LastUpdate != double.NaN)
            offset[1] += RateOffset * (timestamp - LastUpdate) / TimeFactor;
			

        offset[1] += CounterOffset;

        CounterOffset = offset[1];
        RateOffset = offset[0];
        LastUpdate = timestamp;

        //If not applied always return zeros. 
        if (!Applied)
        {
            for (int i = 0; i < offset.Length; i++)
            {
                offset[i] = 0;
            }
        }

        return offset;
    }


	public void SetRateCounter (double scale,double absoluteOffset,double relativeOffset){

		rescale = new double[1];
		rescale [0] = scale;
		relativeoffsets = new double[1];
		relativeoffsets [0] = relativeOffset;
		absoluteoffsets = new double[1];
		absoluteoffsets [0] = absoluteOffset;
		Type = Manipulation.DataType.RateCounter;
	}

}
