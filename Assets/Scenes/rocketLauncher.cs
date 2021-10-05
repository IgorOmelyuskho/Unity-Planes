using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rocketLauncher : MonoBehaviour
{
    public GameObject rocket;
    public GameObject rocket2;
    public GameObject rocket3;

    public GameObject controlRocket;

    public float speed;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //{
        //    GameObject rocketClone = Instantiate(rocket, new Vector3(transform.position.x, transform.position.y, transform.position.z), transform.rotation);
        //    GameObject rocketClone2 = Instantiate(rocket2, new Vector3(transform.position.x, transform.position.y, transform.position.z + 0), transform.rotation);
        //    GameObject rocketClone3 = Instantiate(rocket3, new Vector3(transform.position.x, transform.position.y, transform.position.z + 0), transform.rotation);
        //}

        if (Input.GetKeyDown(KeyCode.X) && Shared.player)
        {
            Vector3 pos = Shared.player.transform.position - 1500 * Shared.player.transform.forward;
            GameObject rocketClone = Instantiate(controlRocket, pos, Shared.player.transform.rotation);
            rocketClone.GetComponent<controlObject>().isLaunchedRocket = true;
            rocketClone.GetComponent<controlObject>().rb.velocity = Shared.player.GetComponent<Rigidbody>().velocity;
            rocketClone.GetComponent<controlObject>().target = Shared.player;
            //rocketClone.GetComponent<controlObject>().rocketOwner = gameObject;
            Shared.player.GetComponent<controlObject>().addObjToInfoNearObjList(rocketClone);
            Shared.hitWithBulletOrRocketObjects.Add(rocketClone);

            Shared.lastLauncheInControlObjdRocket = rocketClone;

            Shared.player.GetComponent<controlObject>().hp = 100;
        }
    }

    void FixedUpdate()
    {
        /*transform.Translate(0, 0, speed);*/
    }
}
