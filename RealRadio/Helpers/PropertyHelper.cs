using System.Runtime.CompilerServices;
using ScheduleOne.Property;
using UnityEngine;

namespace RealRadio.Helpers;

public static class PropertyHelper
{
    /// <summary>
    /// Returns the distance between a position and the bounds of a property.
    /// </summary>
    /// <param name="minAcceptableDistanceSquared">
    /// Minimum acceptable distance to the property.
    /// Used as an optimisation to stop checking distances once a acceptably small distance is found.
    /// </param>
    /// <returns></returns>
    public static double DistanceFromPropertyBoundsSquared(this Property property, Vector3 position, double? minAcceptableDistanceSquared)
    {
        BoxCollider[] colliders = property.propertyBoundsColliders;
        double minDistance = double.MaxValue;

        foreach (var boxCollider in colliders)
        {
            if (!boxCollider.enabled || !boxCollider.gameObject.activeSelf)
                continue;

            double dist = DistanceFromColliderSquared(position, boxCollider);

            if (dist < minDistance)
                minDistance = dist;

            if (minDistance <= minAcceptableDistanceSquared)
                return minDistance;
        }

        return minDistance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double DistanceFromColliderSquared(Vector3 position, BoxCollider box)
    {
        Vector3 closestPoint = box.ClosestPoint(position);
        return (position - closestPoint).sqrMagnitude;
    }
}
