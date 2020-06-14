using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet2 : MonoBehaviour
{
    int i = 0;
    public GameObject target;
    public Vector3 speed;
    public float speedMagnitude;
    public float initBulletSpeed = 0f;
    public float distWhenBulletHitTarget = 5f;
    public int iterationCt = 4;
    public GameObject owner;
    public float hpHit = 10f;

    void Start()
    {
        //target = GameObject.FindGameObjectWithTag("TargetTag");
        step();
    }

    void FixedUpdate()
    {
        step();
    }

    void step()
    {
        i++;
        foreach (GameObject hitWithBulletOrRocketObject in Shared.hitWithBulletOrRocketObjects)
        {
            if (hitWithBulletOrRocketObject == owner) continue;

            if ((transform.position - hitWithBulletOrRocketObject.transform.position).magnitude < initBulletSpeed * Time.deltaTime)
            {
                for (var j = 0; j < iterationCt; j++)
                {
                    Vector3 buulletPosition = transform.position + (speedMagnitude / iterationCt) * speed.normalized * j * Time.fixedDeltaTime;
                    if ((buulletPosition - hitWithBulletOrRocketObject.transform.position).magnitude < distWhenBulletHitTarget)
                    {
                        Destroy(gameObject);
                        hitWithBulletOrRocketObject.GetComponent<controlObject>().hp -= hpHit;
                        break;
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
