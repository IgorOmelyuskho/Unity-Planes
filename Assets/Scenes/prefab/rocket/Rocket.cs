using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*Летит на цель с упреждением*/
public class Rocket : MonoBehaviour
{
    int i = 0;
    public float speed;
    public GameObject target;
    public float maxRoteteSpeed;
    public float angleBetween;
    Quaternion prevRotation;
    public float actualRotateSpeed;
    float interp;

    public LineRenderer lineRenderer;

    Vector3 directionToPreemptive;
    public Vector3 preemptivePosition;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("TargetTag");
        lineRenderer.positionCount = 2;
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
            prevRotation = transform.rotation;
            Vector3 selfDirection = transform.forward;

            preemptivePosition = Shared.CalculateAim(target.transform.position, target.GetComponent<target>().speed, transform.position, speed, Vector3.zero);
            directionToPreemptive = (preemptivePosition - transform.position).normalized;
            angleBetween = Vector3.Angle(selfDirection, directionToPreemptive);

            if (maxRoteteSpeed * Time.fixedDeltaTime > angleBetween)
            {
                transform.rotation = Quaternion.LookRotation(directionToPreemptive);
            }
            else
            {
                interp = (maxRoteteSpeed * Time.fixedDeltaTime) / angleBetween;
                Vector3 calcfDirection = Vector3.Slerp(selfDirection, directionToPreemptive, interp);
                transform.rotation = Quaternion.LookRotation(calcfDirection);
            }

            transform.Translate(0, 0, Time.fixedDeltaTime * speed);
            actualRotateSpeed = Quaternion.Angle(transform.rotation, prevRotation) / Time.fixedDeltaTime;
        }

        //lineRendererMethod();
    }

    void lineRendererMethod()
    {
        lineRenderer.SetPosition(0, preemptivePosition);
        lineRenderer.SetPosition(1, transform.position);
    }
}
