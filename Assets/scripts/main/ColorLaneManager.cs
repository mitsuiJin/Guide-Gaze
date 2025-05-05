using UnityEngine;
using System.Collections.Generic;

public class ColorLaneManager : MonoBehaviour
{
    public List<LineRenderer> colorLanes = new List<LineRenderer>();

    public List<List<Vector2>> GetAllLanePoints()
    {
        List<List<Vector2>> allLanePoints = new List<List<Vector2>>();

        foreach (var lane in colorLanes)
        {
            int count = lane.positionCount;
            Vector3[] positions = new Vector3[count];
            lane.GetPositions(positions);

            List<Vector2> lanePoints2D = new List<Vector2>();
            foreach (var pos in positions)
            {
                lanePoints2D.Add(new Vector2(pos.x, pos.y)); // XY만
            }
            allLanePoints.Add(lanePoints2D);
        }
        return allLanePoints;
    }
}
