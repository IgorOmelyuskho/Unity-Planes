using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class target : MonoBehaviour
{
    int i;
    Vector3 rotationMask = new Vector3(0, 1, 0); //which axes to rotate around
    public float rotationSpeed = 15.0f; //degrees per second
    public Transform rotateAroundObject;
    public Vector3 calculatedSpeed;
    private Vector3 prevPosition;

    public Vector3 speed = new Vector3(0, 0, 10);
   
    void Start()
    {
/*        transform.LookAt(target.transform.position);*/
     /*   transform.Rotate(90, 0, 0, Space.Self);*/
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        i++;
        prevPosition = transform.position;
        Move();
        calculatedSpeed = (transform.position - prevPosition) / Time.fixedDeltaTime;
    }

    void LateUpdate()
    {
        /*Camera.main.transform.LookAt(target.transform);*/
    }

    void OnMouseDown() /*under object*/
    {
    }

    void Move()
    {
        if (rotateAroundObject)
        {
            transform.RotateAround(rotateAroundObject.transform.position, rotationMask, rotationSpeed * Time.deltaTime);
        }

        //if (i % 1000 == 0)
        //{
        //    speed = -speed;
        //}
        //transform.Translate(speed * Time.fixedDeltaTime);
    }
}
