using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DistanceText : MonoBehaviour
{
    public Camera camera1;
    public Text textLabel;

    void Start()
    {
        textLabel.text = "INIT TEXT";
    }
    void LateUpdate()
    {
        Vector3 textPos = Camera.main.WorldToScreenPoint(this.transform.position);
        textLabel.transform.position = textPos;

        if (camera1)
        {
            var dist = Vector3.Distance(camera1.transform.position, transform.position);
            textLabel.text = dist.ToString("0.0");
        }
    }
}
