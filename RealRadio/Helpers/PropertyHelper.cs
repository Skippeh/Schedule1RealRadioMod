using System.Runtime.CompilerServices;
using ScheduleOne.Property;
using UnityEngine;

namespace RealRadio.Helpers;

public static class PropertyHelper
{
    public static double DistanceFromPropertyBoundsSquared(this Property property, Vector3 position)
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
