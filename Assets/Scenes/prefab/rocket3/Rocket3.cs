using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*Летит на цель с упреждением, постепенно набирая скорость, маневренность зависит от скорости*/
public class Rocket3 : MonoBehaviour
{
    int i = 0;
    public float speed; /*max speed*/
    public float currentSpeed = 0;
    public float deltaSpeed;
    float rotareSpeedCoeff;
    public GameObject target;
    public float maxRoteteSpeed;
    public float currentRoteteSpeed = 0;
    public float angleBetween;
    Quaternion prevRotation;
    public float actualRotateSpeed;
    float interp;

    public float acceleration;
    public float acceleration2;
    public Vector3 acceleration2Vector;

    public Vector3 bigVector;

    public LineRenderer lineRenderer;

    public float flyTimeToTarget;
    public Vector3 preemptivePosition;
    Vector3 directionToPreemptive;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("TargetTag");
        lineRenderer.positionCount = 2;
        rotareSpeedCoeff = maxRoteteSpeed / speed;
    }

    void FixedUpdate()
    {
        i++;
        if (i > 15000 || (transform.position - target.transform.position).magnitude < 3/* || (prevDistance - distance > 1 && i > 100)*/)
        {
            Destroy(gameObject);
        }

        float prevSpeed = currentSpeed;
        currentSpeed += deltaSpeed;
        if (currentSpeed > speed)
        {
            currentSpeed = speed;
        }
        acceleration = (currentSpeed - prevSpeed) / Time.fixedDeltaTime;

        currentRoteteSpeed = rotareSpeedCoeff * currentSpeed;
        /*currentRoteteSpeed = maxRoteteSpeed;*/

        Math3d.LinearAcceleration(out acceleration2Vector, transform.position, 3);
        acceleration2 = acceleration2Vector.magnitude;

        if (target)
        {
            prevRotation = transform.rotation;
            Vector3 selfDirection = transform.forward;
            preemptivePosition = Shared.CalculateAim(target.transform.position, target.GetComponent<target>().speed, transform.position, currentSpeed, Vector3.zero, Vector3.zero);
            directionToPreemptive = (preemptivePosition - transform.position).normalized;
            angleBetween = Vector3.Angle(selfDirection, directionToPreemptive);

            if (currentRoteteSpeed * Time.fixedDeltaTime > angleBetween)
            {
                transform.rotation = Quaternion.LookRotation(directionToPreemptive);
            }
            else
            {
                interp = (currentRoteteSpeed * Time.fixedDeltaTime) / angleBetween;
                Vector3 calcfDirection = Vector3.Slerp(selfDirection, directionToPreemptive, interp);
                transform.rotation = Quaternion.LookRotation(calcfDirection);
            }

            transform.Translate(0, 0, Time.fixedDeltaTime * currentSpeed);
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
