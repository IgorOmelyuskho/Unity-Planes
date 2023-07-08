using System.Collections;
using UnityEngine;

public class rocketLauncher : MonoBehaviour
{
    public GameObject rocket;
    public GameObject rocket2;
    public GameObject rocket3;

    public GameObject controlRocket;
    public GameObject controlRocket2;

    public float speed;

    public bool lookAtPlayer = false;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(CallFunctionWithDelay(3f, controlRocket, controlRocket2));
    }
    
    private IEnumerator CallFunctionWithDelay(float delay, GameObject controlRocket, GameObject controlRocket2)
    {
        //while (true)
        {
            yield return new WaitForSeconds(delay);
            launchRocket(controlRocket, Color.blue);
            //launchRocket(controlRocket2, false);
        }
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
            launchRocket(controlRocket, Color.blue);
            //launchRocket(controlRocket2, Color.green);
            //launchRocket(controlRocket2);

            Shared.player.GetComponent<controlObject>().hp = 100;
        }
    }

    void FixedUpdate()
    {
        if (lookAtPlayer)
        {
            transform.LookAt(Shared.player.transform);
        }
        /*transform.Translate(0, 0, speed);*/
    }

    void launchRocket(GameObject rocket, Color trailColor)
    {
        GameObject rocketClone = Instantiate(rocket, new Vector3(transform.position.x, transform.position.y, transform.position.z) - transform.up * 1.0f, transform.rotation);
        rocketClone.GetComponent<controlObject>().isLaunchedRocket = true;
        rocketClone.GetComponent<controlObject>().rb.velocity = Vector3.zero;
        rocketClone.GetComponent<controlObject>().target = Shared.player;
        //rocketClone.GetComponent<controlObject>().rocketOwner = gameObject;
        Shared.player.GetComponent<controlObject>().addObjToInfoNearObjList(rocketClone);
        Shared.hitWithBulletOrRocketObjects.Add(rocketClone);
        
        TrailRenderer trailRenderer = rocketClone.transform.Find("for-trail-renderer").GetComponent<TrailRenderer>();
        trailRenderer.startColor = trailColor;

        Shared.lastLauncheInControlObjdRocket = rocketClone;
    }
}
