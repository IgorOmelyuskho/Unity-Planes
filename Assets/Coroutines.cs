using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coroutines : MonoBehaviour
{
    private IEnumerator coroutine;

    // Start is called before the first frame update
    void Start()
    {
        coroutine = RemoveHitWithBulletOrRocketObjects();
        StartCoroutine(coroutine);
    }

    private IEnumerator RemoveHitWithBulletOrRocketObjects()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();

            List<GameObject> objListForRemove = new List<GameObject>();

            foreach (GameObject hitWithBulletOrRocketObject in Shared.hitWithBulletOrRocketObjects)
            {
                if (!hitWithBulletOrRocketObject)
                {
                    objListForRemove.Add(hitWithBulletOrRocketObject);
                }
            }

            Shared.hitWithBulletOrRocketObjects.RemoveAll(objListForRemove.Contains);
        }
    }
}
