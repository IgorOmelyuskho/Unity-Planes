using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet2 : MonoBehaviour
{
    int i = 0;
    public GameObject target;
    public Vector3 speed;
    public float speedMagnitude;
    public float initBulletSpeed = 1150.0f;
    public float distWhenBulletHitTarget = 5f;
    public int iterationCt = 4;

    void Start()
    {
        //target = GameObject.FindGameObjectWithTag("TargetTag");
    }

    void FixedUpdate()
    {
        i++;
        foreach (GameObject hitWithBulletObject in Shared.hitWithBulletObjects)
        {
            if ((transform.position - hitWithBulletObject.transform.position).magnitude < initBulletSpeed * Time.deltaTime)
            {
                for (var j = 0; j < iterationCt; j++)
                {
                    Vector3 buulletPosition = transform.position + (speedMagnitude / iterationCt) * speed.normalized * j * Time.fixedDeltaTime;
                    if ((buulletPosition - hitWithBulletObject.transform.position).magnitude < distWhenBulletHitTarget)
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }

        if (i > 1000)
        {
            Destroy(gameObject);
        }

        //speed.y -= Shared.gravity * Time.fixedDeltaTime;
        speedMagnitude = speed.magnitude;
        //transform.Translate(Time.fixedDeltaTime * speed); // not work
        transform.position += Time.fixedDeltaTime * speed;
    }
}
