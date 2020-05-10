using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    int i = 0;
    public GameObject target;
    public Vector3 speed;
    public float speedMagnitude;
    void Start()
    {
        target = GameObject.FindGameObjectWithTag("TargetTag");
    }

    void FixedUpdate()
    {
        i++;
        if (i > 1000 || (transform.position - target.transform.position).magnitude < 5)
        {
            Destroy(gameObject);
        }

        //speed.y -= Shared.gravity * Time.fixedDeltaTime;
        speedMagnitude = speed.magnitude;
        //transform.Translate(Time.fixedDeltaTime * speed); // not work
        transform.position += Time.fixedDeltaTime * speed;
    }
}
