using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class AngleCoords
{
    public AngleCoords(int angle, int iterationCnt)
    {
        this.angle = angle;
        coordsArr = new Vector2[iterationCnt];
    }
    public int angle;
    public Vector2[] coordsArr;
}

public class NearestAnglIndexAndCoordIndex
{
    public NearestAnglIndexAndCoordIndex(int angleIndex, int angleIndex2, float interpolateDegAngle, float flyTime)
    {
        this.angleIndex = angleIndex; // nearest index
        this.angleIndex2 = angleIndex2; // second nearest index
        this.interpolateDegAngle = interpolateDegAngle;
        this.flyTime = flyTime;
    }
    public int angleIndex;
    public int angleIndex2;
    public float interpolateDegAngle;
    public float flyTime;
}

public class NearestPointResult : IComparable
{
    public NearestPointResult(int angleIndex, float sqrMinDistance, int pointIndex)
    {
        this.angleIndex = angleIndex;
        this.sqrMinDistance = sqrMinDistance;
        this.pointIndex = pointIndex;
    }
    public float sqrMinDistance;
    public int pointIndex;
    public int angleIndex;

    public int CompareTo(object o)
    {
        NearestPointResult p = o as NearestPointResult;
        if (p != null)
            return this.sqrMinDistance.CompareTo(p.sqrMinDistance);
        else
            throw new System.Exception("Невозможно сравнить два объекта");
    }
}

public class Shared
{
    static public float gravity = 9.8f;
    /*static public float gravity = 0f;*/
    static public readonly int iterationCnt = 1500;
    static public float speed = 500;
    //static public float decelerationCoeff = 0.01f; // mass, drag coefficient, area, air density, ...
    public const int deltaAngle = 10;
    static public AngleCoords[] angleArr;
    static public List<GameObject> hitWithBulletOrRocketObjects = new List<GameObject>();
    static public GameObject player;
    static public GameObject lastLauncheInControlObjdRocket;

    static Shared()
    {
        int angleArrLength = 0;
        for (var i = -90; i <= 90; i += Shared.deltaAngle)
        {
            angleArrLength++;
        }
        Shared.angleArr = new AngleCoords[angleArrLength];

        int angleArrArrIndex = 0;
        for (var i = -90; i <= 90; i += Shared.deltaAngle)
        {
            Shared.angleArr[angleArrArrIndex] = CalcForAngle(i);
            angleArrArrIndex++;
        }

        hitWithBulletOrRocketObjects = GameObject.FindGameObjectsWithTag("hitWithBulletOrRocket").OfType<GameObject>().ToList();
    }

    public static float GetSignedAngle(Quaternion A, Quaternion B, Vector3 axis)
    {
        float angle = 0f;
        Vector3 angleAxis = Vector3.zero;
        (B * Quaternion.Inverse(A)).ToAngleAxis(out angle, out angleAxis);
        if (Vector3.Angle(axis, angleAxis) > 90f)
        {
            angle = -angle;
        }
        return Mathf.DeltaAngle(0f, angle);
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }


    static public float PDController(float p, float d, float pCoeff, float dCoeff)
    {
        return p * pCoeff + d * dCoeff;
    }

    public class PIDController
    {
        public float aCoeff;
        public float pCoeff;
        public float dCoeff;
        public float iCoeff;
        public float error_sum;
        public float error_sumMax;
        public float p_old;

        public PIDController(float aCoeff, float pCoeff, float dCoeff, float iCoeff, float error_sumMax)
        {
            this.aCoeff = aCoeff;
            this.pCoeff = pCoeff;
            this.dCoeff = dCoeff;
            this.iCoeff = iCoeff;
            this.error_sumMax = error_sumMax;
        }

        public void Update(float aCoeff, float pCoeff, float dCoeff, float iCoeff, float error_sumMax)
        {
            this.aCoeff = aCoeff;
            this.pCoeff = pCoeff;
            this.dCoeff = dCoeff;
            this.iCoeff = iCoeff;
            this.error_sumMax = error_sumMax;
        }

        public float Calculate(float p, float d)
        {
            error_sum += Time.fixedDeltaTime * p;

            //Clamp the sum 
            error_sum = Mathf.Clamp(error_sum, -error_sumMax, error_sumMax);

            float a = (p - p_old) / Time.fixedDeltaTime;

            p_old = p;

            return a * aCoeff + p * pCoeff + d * dCoeff + error_sum * iCoeff;
        }
    }


    // aim without gravity and wind drag
    // selfSpeed - for rocket use Vector3.zero
    static public Vector3 CalculateAim(Vector3 targetPosition, Vector3 targetSpeed, Vector3 selfPosition, float bulletOrRocketSpeed, Vector3 selfSpeed, Vector3 targetAcceleration)
    {
        Vector3 targetingPosition = targetPosition;
        for (int i = 0; i < 10; i++)
        {
            float dist = (selfPosition - targetingPosition).magnitude;
            float timeToTarget = dist / bulletOrRocketSpeed;
            targetingPosition = targetPosition + (targetSpeed - selfSpeed) * timeToTarget + targetAcceleration * Mathf.Pow(timeToTarget, 2) / 2;
            if (
                float.IsNaN(targetingPosition.x) ||
                float.IsNaN(targetingPosition.y) ||
                float.IsNaN(targetingPosition.z) ||
                targetingPosition.x > 5000000 ||
                targetingPosition.y > 5000000 ||
                targetingPosition.z > 5000000
               )
            {
                return targetPosition + targetSpeed * 100000;
            }
        }

        return targetingPosition;
    }

    //static public vector3 calculateaim2(vector3 targetposition, vector3 targetspeed, vector3 selfposition, float bulletorrocketspeed)
    //{

    //}

    static public float LineaRInterpolate(float x0, float y0, float x1, float y1, float x)
    {
        if (Mathf.Abs(x1 - x0) < 0.000000000001)
        {
            return y0;
        }

        return y0 + ((y1 - y0) / (x1 - x0)) * (x - x0);
    }

    public static NearestAnglIndexAndCoordIndex FindNearestIndex(float xCoord, float yCoord)
    {
        float timeStart = Time.realtimeSinceStartup;
        NearestAnglIndexAndCoordIndex result = null;
        Vector2 coordsVector = new Vector2(xCoord, yCoord);
        int angleIndex1;
        int angleIndex2;
        float dist1;
        float dist2;
        NearestPointResult[] nearestPointsArr = new NearestPointResult[angleArr.Length];

        for (var i = 0; i < angleArr.Length; i++)
        {
            nearestPointsArr[i] = NearestPoint(i, coordsVector);
        }

        Array.Sort(nearestPointsArr);
        angleIndex1 = nearestPointsArr[0].angleIndex;
        angleIndex2 = nearestPointsArr[1].angleIndex;
        dist1 = nearestPointsArr[0].sqrMinDistance;
        dist2 = nearestPointsArr[1].sqrMinDistance;

        float flyTime = nearestPointsArr[0].pointIndex * Time.fixedDeltaTime;
        result = WhenExit(angleIndex1, angleIndex2, coordsVector, dist1, dist2, flyTime);

        float timeEnd = Time.realtimeSinceStartup;
        //Debug.Log((timeEnd - timeStart) * 1000);
        return result;
    }

    static NearestAnglIndexAndCoordIndex WhenExit(int index1, int index2, Vector2 coordsVector, float dist1, float dist2, float flyTime)
    {
        NearestPoint(index2, coordsVector);
        NearestPoint(index1, coordsVector);
        float angle1 = index1 * (180 / (angleArr.Length - 1));
        float angle2 = index2 * (180 / (angleArr.Length - 1));
        float delteAngle = angle1 - angle2;
        if (dist1 > dist2)
        {
            float div = Mathf.Sqrt(dist1 / dist2);
            float part = delteAngle / (div + 1);
            float interpolateDegAngle = angle1 - (delteAngle - part);
            return new NearestAnglIndexAndCoordIndex(index2, index1, interpolateDegAngle, flyTime);
        }
        else
        {
            float div = Mathf.Sqrt(dist2 / dist1);
            float part = delteAngle / (div + 1);
            float interpolateDegAngle = angle1 - part;
            return new NearestAnglIndexAndCoordIndex(index1, index2, interpolateDegAngle, flyTime);
        }
    }

    static NearestPointResult NearestPoint(int angleIndex, Vector2 coordsVector)
    {
        int pointIndex = 0;
        float sqrMinDistance = 10000000f;
        bool exit = false;
        int index1 = 0;
        int index2 = angleArr[angleIndex].coordsArr.Length - 1;
        float sqtDist1 = Vector2.SqrMagnitude(coordsVector - angleArr[angleIndex].coordsArr[index1]);
        float sqtDist2 = Vector2.SqrMagnitude(coordsVector - angleArr[angleIndex].coordsArr[index2]);
        while (exit == false)
        {
            if (sqtDist1 < sqtDist2)
            {
                index2 = (index2 + index1) / 2;
                sqtDist2 = Vector2.SqrMagnitude(coordsVector - angleArr[angleIndex].coordsArr[index2]);
            }
            else
            {
                index1 = (index2 + index1) / 2;
                sqtDist1 = Vector2.SqrMagnitude(coordsVector - angleArr[angleIndex].coordsArr[index1]);
            }
            if (Mathf.Abs(index2 - index1) == 1)
            {
                exit = true;
                sqtDist2 = Vector2.SqrMagnitude(coordsVector - angleArr[angleIndex].coordsArr[index2]);
                sqtDist1 = Vector2.SqrMagnitude(coordsVector - angleArr[angleIndex].coordsArr[index1]);
                if (sqtDist2 > sqtDist1)
                {
                    sqrMinDistance = sqtDist1;
                    pointIndex = index1;
                }
                else
                {
                    sqrMinDistance = sqtDist2;
                    pointIndex = index2;
                }
            }
        }

        return new NearestPointResult(angleIndex, sqrMinDistance, pointIndex);
    }

    static NearestPointResult GetPointByIndex(int angleIndex, int pointIndex, Vector2 coordsVector)
    {
        float sqrMinDistance = Vector2.SqrMagnitude(coordsVector - angleArr[angleIndex].coordsArr[pointIndex]);
        return new NearestPointResult(angleIndex, sqrMinDistance, pointIndex);

    }

    public static AngleCoords CalcForAngle(int degAngle)
    {
        AngleCoords result = new AngleCoords(degAngle, Shared.iterationCnt);
        float angle = degAngle * Mathf.Deg2Rad;
        float xCoord = 0;
        float yCoord = 0;
        float xSpeed = Shared.speed * Mathf.Cos(angle);
        float ySpeed = Shared.speed * Mathf.Sin(angle);
        for (var i = 0; i < Shared.iterationCnt; i++)
        {
            result.coordsArr[i] = new Vector2(xCoord, yCoord);
            ySpeed -= Shared.gravity * Time.fixedDeltaTime;
            xCoord += xSpeed * Time.fixedDeltaTime;
            yCoord += ySpeed * Time.fixedDeltaTime;
        }
        return result;
    }

    public static Vector2[] CalcAnyAngle(float degAngle)
    {
        Vector2[] res = new Vector2[Shared.iterationCnt];
        /*print(degAngle);*/
        /*0* - 90* - 180*  to  -90* - 0* - 90**/
        degAngle = Shared.LineaRInterpolate(0, -90, 90, 0, degAngle);
        float angle = degAngle * Mathf.Deg2Rad;
        float xCoord = 0;
        float yCoord = 0;
        float xSpeed = Shared.speed * Mathf.Cos(angle);
        float ySpeed = Shared.speed * Mathf.Sin(angle);
        for (var i = 0; i < Shared.iterationCnt; i++)
        {
            res[i] = new Vector2(xCoord, yCoord);
            ySpeed -= Shared.gravity * Time.fixedDeltaTime;
            xCoord += xSpeed * Time.fixedDeltaTime;
            yCoord += ySpeed * Time.fixedDeltaTime;
        }
        return res;
    }

    /// <summary>
    /// Find some projected angle measure off some forward around some axis.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="forward"></param>
    /// <param name="axis"></param>
    /// <returns>Angle in degrees</returns>
    public static float AngleOffAroundAxis(Vector3 v, Vector3 forward, Vector3 axis, bool clockwise = false)
    {
        Vector3 right;
        if (clockwise)
        {
            right = Vector3.Cross(forward, axis);
            forward = Vector3.Cross(axis, right);
        }
        else
        {
            right = Vector3.Cross(axis, forward);
            forward = Vector3.Cross(right, axis);
        }
        return Mathf.Atan2(Vector3.Dot(v, right), Vector3.Dot(v, forward)) * Mathf.Rad2Deg;
    }
}


