using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class antiaircraft : MonoBehaviour
{
    public GameObject target;
    public Bullet bullet;
    int i;
    int j;
    public LineRenderer lineRenderer;
    public float flyTimeToTarget;
    Vector3 aimPosition;
    public bool lookOnCameraFwd = false;
    public float initBulletSpeed = 0; // change in Inspector
    public Vector3 fwd;
    public Vector3 speed = new Vector3(0, 0, 0);
    public float verticalAimAngle;
    public float x;
    public float y;

    void Start()
    {
        lineRenderer.positionCount = 2;
    }

    void FixedUpdate()
    {
        i++;
        j++;
        if (i % 100 == 0)
        {
            j = 0;
        }

        if (target)
        {
            Vector3 targetingPosition = target.transform.position;
            Vector3 targetSpeed = Vector3.zero;
            Vector3 targetAcceleration = Vector3.zero;
            Vector3 targetJerk = Vector3.zero;
            if (target)
            {
                try
                {
                    targetSpeed = target.GetComponent<target>().calculatedSpeed;
                    targetAcceleration = Vector3.zero;
                }
                catch
                {
                    targetSpeed = target.GetComponent<controlObject>().rb.velocity;
                    targetAcceleration = target.GetComponent<controlObject>().actualAcceleration;
                    targetJerk = target.GetComponent<controlObject>().actualJerk;
                }
            }

     
            for (int i = 0; i < 30; i++)
            {
                x = Mathf.Sqrt(Mathf.Pow(targetingPosition.x - transform.position.x, 2) + Mathf.Pow(targetingPosition.z - transform.position.z, 2));
                y = targetingPosition.y - transform.position.y;
                NearestAnglIndexAndCoordIndex nearest = Shared.FindNearestIndex(x, y);
                verticalAimAngle = nearest.interpolateDegAngle;
                flyTimeToTarget = nearest.flyTime;

                float yAimCoord = x * Mathf.Tan((verticalAimAngle - 90) * Mathf.Deg2Rad);
                targetingPosition = target.transform.position +
                    targetSpeed * flyTimeToTarget +
                    targetAcceleration * Mathf.Pow(flyTimeToTarget, 2) / 2 +
                    targetJerk * Mathf.Pow(flyTimeToTarget, 3) / 6;
                aimPosition = new Vector3(targetingPosition.x, yAimCoord + transform.position.y, targetingPosition.z);
            }

            //aimPosition = Shared.CalculateAim(target.transform.position, target.GetComponent<target>().calculatedSpeed, transform.position, initBulletSpeed, speed);
            //aimPosition = target.transform.position;
        }

        if (lookOnCameraFwd == true)
        {
            transform.LookAt(Camera.main.transform.forward * 100000);
        }
        else
        {
            transform.LookAt(aimPosition);
        }

        var angle = 0f;
        var rotationY = Quaternion.AngleAxis(Random.Range(-angle, angle), transform.up);
        var rotationX = Quaternion.AngleAxis(Random.Range(-angle, angle), transform.right);
        transform.rotation *= rotationX * rotationY;
        fwd = transform.forward;


        if (Input.GetMouseButton(0) || j < 10)// 50  100 // GetMouseButton GetMouseButtonDown
        {
            Bullet bulletClone = Instantiate(bullet, transform.position, transform.rotation);
            bulletClone.speed = initBulletSpeed * fwd + speed;
            bulletClone.timeToDestroy = flyTimeToTarget;
            //bulletClone.speed = initBulletSpeed * new Vector3(0, 0, 0);
        }
/*        if (i % 30 == 0)
        {
            Bullet bulletClone = Instantiate(bullet, new Vector3(transform.position.x, transform.position.y, transform.position.z), transform.rotation);
            bulletClone.speed = initBulletSpeed * transform.forward;
        }*/

        //transform.position += Time.fixedDeltaTime * speed;
        lineRendererMethod();
    }

    void lineRendererMethod()
    {
        lineRenderer.SetPosition(0, target.transform.position);
        lineRenderer.SetPosition(1, aimPosition);

        //lineRenderer.SetPosition(0, transform.position);
        //lineRenderer.SetPosition(1, aimPosition);

        //lineRenderer.SetPosition(0, transform.position);
        //lineRenderer.SetPosition(1, fwd * 10000000);
    }
}
