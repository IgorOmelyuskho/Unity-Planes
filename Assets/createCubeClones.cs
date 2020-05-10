using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class createCubeClones : MonoBehaviour
{
    public GameObject cubeForClone;

    // Start is called before the first frame update
    void Start()
    {
       float dist = 250;
       int countByOneSide = 100;
       for (var x = -countByOneSide; x <= countByOneSide; x++)
        {
            for (var z = -countByOneSide; z <= countByOneSide; z++)
            {
                GameObject cubeClone = Instantiate(cubeForClone, new Vector3(x * dist, 0, z * dist), transform.rotation);
                cubeClone.transform.parent = gameObject.transform;
            }
        } 
    }
}
