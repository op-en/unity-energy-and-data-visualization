using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DataManipulator : DataModifier
{


    public List<Manipulation> Manipulations;
    [Header("Debug")]
    public int selected = 0;




	public static DataManipulator GetSeriesByName(string name){


		DataManipulator[] series = FindObjectsOfType(typeof(DataManipulator)) as DataManipulator[];
		foreach (DataManipulator serie in series) {
			if (serie.transform.name == name || serie.NodeName == name)
				return serie;
		}

		return null;

	}

    override public void UpdateAllTargets(DataPoint Data)
    {
        base.UpdateAllTargets(ApplyModifiers(Data));
    }

    public DataPoint ApplyModifiers(DataPoint point)
    {
		point = base.ApplyModifiers (point);

        if (Manipulations.Count == 0)
            return point;

        DataPoint newpoint = point.Clone();

        foreach (Manipulation manipulation in Manipulations)
        {
            manipulation.AddOffsets(point, newpoint);
        }

        return newpoint;
    }

    public void Activate() {

        if (selected < Manipulations.Count) {
            double now = GameTime.GetInstance().time;
            Manipulations[selected].Activate(now);


        }
    }

    public void Deactivate()
    {
        if (selected < Manipulations.Count)
            Manipulations[selected].DeActivate(GameTime.GetInstance().time);
    }

	public void Add(Manipulation manipulation)
	{
		Manipulations.Add (manipulation);




	}

	public void Activate(int id)
	{
		if (id > Manipulations.Count - 1)
			return;

		double now = GameTime.GetInstance ().time;

		Manipulations [id].Activate (now);


		//Already active so we update LastValue. 
		if (Manipulations [id].Type == Manipulation.DataType.RateCounter){
			print("Interpolating new point");
			CreateCounterRateInterpolation (now, Manipulations [id].TimeFactor);
		}
		else {

			LastData.Timestamp = now;
			UpdateAllTargets (LastData);
		}


	}

	public void Deactivate(int id)
	{
		if (id > Manipulations.Count - 1)
			return;

		double now = GameTime.GetInstance ().time;

		if (Manipulations [id].Type == Manipulation.DataType.RateCounter){
			CreateCounterRateInterpolation (now, Manipulations [id].TimeFactor);
		}
		else {

			LastData.Timestamp = now;
			UpdateAllTargets (LastData);
		}
			

		Manipulations [id].DeActivate (now);



	}


}
