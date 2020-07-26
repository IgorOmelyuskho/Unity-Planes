using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class btnsScript : MonoBehaviour
{
    public void RemoveRocketAndBoolet()
    {
        GameObject[] rockets = GameObject.FindGameObjectsWithTag("rocketTag");
        foreach (GameObject rocket in rockets)
        {
            Destroy(rocket);
        }

        GameObject[] bullets = GameObject.FindGameObjectsWithTag("bulletTag");
        foreach (GameObject bullet in bullets)
        {
            Destroy(bullet);
        }
    }
}
