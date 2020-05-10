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
            aimPosition = Shared.CalculateAim(target, transform.position, initBulletSpeed);
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
            bulletClone.speed = initBulletSpeed * new Vector3(fwd.x, fwd.y, fwd.z);
            //bulletClone.speed = initBulletSpeed * new Vector3(0, 0, 0);
        }
/*        if (i % 30 == 0)
        {
            Bullet bulletClone = Instantiate(bullet, new Vector3(transform.position.x, transform.position.y, transform.position.z), transform.rotation);
            bulletClone.speed = initBulletSpeed * transform.forward;
        }*/

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
