using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public static class PositionOptimizer2D 
{
    private static float _width = 0.1f;
    // Minimum number of similar positions needed to confirm a position
    private static int _groupCountThreshold = 5;

    // Dictionary to hold grouped positions
    private static Dictionary<Vector2, List<Vector2>> groupedPositions = new Dictionary<Vector2, List<Vector2>>();
    

    // Method to update the position based on grouping
    public static Vector2 UpdatePosition(Vector2 currentPosition,float width)
    {
        bool foundGroup = false;
        bool groupCountValid = false;
        _width = width;

        foreach (KeyValuePair<Vector2, List<Vector2>> pair in groupedPositions)
        {
            Vector2 groupCenter = pair.Key;
            List<Vector2> group = pair.Value;
            groupCountValid = group.Count >= _groupCountThreshold;

            // Determine if the current position is similar to the group center
            if (IsSimilar(currentPosition, groupCenter))
            {
                group.Add(currentPosition);
                foundGroup = true;
                if (groupCountValid)
                {
                    // Calculate the average position
                    Vector2 averagePosition = GetPositionAverage(group);
                    // Clear the groups
                    groupedPositions.Clear();
                    return averagePosition;
                }
                else
                {
                    return Vector2.zero;
                }
            }
        }

        // Limit the length of the dictionary
        if (groupCountValid)
        {
            groupedPositions.Clear();
        }
        else
        {
            // If no similar group is found, create a new group
            {
                List<Vector2> newGroup = new List<Vector2>();
                newGroup.Add(currentPosition);
                groupedPositions.Add(currentPosition, newGroup);
            }
        }
        return currentPosition;
    }
    

    // Check if two positions are similar based on a threshold
    private static bool IsSimilar(Vector2 pos1, Vector2 pos2)
    {
        return Vector2.Distance(pos1, pos2) < _width;
    }

    // Calculate the average position of a group of positions
    private static Vector2 GetPositionAverage(List<Vector2> group)
    {
        Vector2 sum = Vector2.zero;
        foreach (Vector2 pos in group)
        {
            sum += pos;
        }
        return sum / group.Count;
    }

}
