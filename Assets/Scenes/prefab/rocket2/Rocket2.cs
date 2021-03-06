﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*Летит на цель*/
public class Rocket2 : MonoBehaviour
{
    int i = 0;
    public float speed;
    public GameObject target;
    public float maxRoteteSpeed;
    public float angleBetween;
    Quaternion prevRotation;
    public float actualRotateSpeed;
    public float interp;
    public float actualAcceleration;
    public float actualSpeed;
    Vector3 prevPosition;
    public float prevSpeed;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("TargetTag");
    }

    void FixedUpdate()
    {
        i++;
        if (i > 15000 || (transform.position - target.transform.position).magnitude < 3/* || (prevDistance - distance > 1 && i > 100)*/)
        {
            Destroy(gameObject);
        }

        if (target)
        {
            Vector3 selfDirection = transform.forward;
            Vector3 directionForTarget = (target.transform.position - transform.position).normalized;
            angleBetween = Vector3.Angle(selfDirection, directionForTarget);

            if (maxRoteteSpeed * Time.fixedDeltaTime > angleBetween)
            {
                transform.rotation = Quaternion.LookRotation(directionForTarget);
            }
            else
            {
                interp = (maxRoteteSpeed * Time.fixedDeltaTime) / angleBetween;
                Vector3 calcfDirection = Vector3.Slerp(selfDirection, directionForTarget, interp);
                transform.rotation = Quaternion.LookRotation(calcfDirection);
            }

            transform.Translate(0, 0, Time.fixedDeltaTime * speed);

            actualRotateSpeed = Quaternion.Angle(transform.rotation, prevRotation) / Time.fixedDeltaTime;
            actualSpeed = (transform.position - prevPosition).magnitude / Time.fixedDeltaTime;
            /*actualAcceleration = (actualSpeed - prevSpeed) / Time.fixedDeltaTime;*/
            /*actualAcceleration = actualSpeed * Time.fixedDeltaTime;*/
            /*actualAcceleration = Math3d.ClosestPointsOnTwoLines*/

            prevSpeed = (transform.position - prevPosition).magnitude / Time.fixedDeltaTime;
            prevPosition = transform.position;
            prevRotation = transform.rotation;
        }
    }
}
