using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class antiaircraft : MonoBehaviour
{
    public GameObject target;
    public Bullet bullet;
    int i;
    public LineRenderer lineRenderer;
    public float flyTimeToTarget;
    Vector3 aimPosition;
    public bool lookOnCameraFwd = false;
    public float initBulletSpeed = 500.0f;
    public Vector3 fwd;
    public Vector3 speed = new Vector3(0, 0, 0);
    public float verticalAimAngle;
    public float x;
    public float y;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("TargetTag");
        lineRenderer.positionCount = 2;
    }

    void FixedUpdate()
    {
        i++;
        if (target)
        {
            Vector3 targetingPosition = target.transform.position;
            Vector3 targetSpeed = target.GetComponent<target>().calculatedSpeed;

            for (int i = 0; i < 10; i++)
            {
                x = Mathf.Sqrt(Mathf.Pow(targetingPosition.x - transform.position.x, 2) + Mathf.Pow(targetingPosition.z - transform.position.z, 2));
                y = targetingPosition.y - transform.position.y;
                NearestAnglIndexAndCoordIndex nearest = Shared.FindNearestIndex(x, y);
                verticalAimAngle = nearest.interpolateDegAngle;
                flyTimeToTarget = nearest.flyTime;

                float yAimCoord = x * Mathf.Tan((verticalAimAngle - 90) * Mathf.Deg2Rad);
                targetingPosition = target.transform.position + targetSpeed * flyTimeToTarget;
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
        fwd = transform.forward;


        if (Input.GetMouseButton(0)) // GetMouseButton GetMouseButtonDown
        {
            Bullet bulletClone = Instantiate(bullet, new Vector3(transform.position.x, transform.position.y, transform.position.z), transform.rotation);
            bulletClone.speed = initBulletSpeed * new Vector3(fwd.x, fwd.y, fwd.z) + speed;
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
