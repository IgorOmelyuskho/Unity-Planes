﻿

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOperate : MonoBehaviour
{
    [Tooltip("Mouse wheel rolling control lens please enter, the speed of the back")]
    [Range(0.1f, 2f)] public float scrollSpeed = 1f;
    [Tooltip("Right mouse button control lens X axis rotation speed")]
    [Range(0.1f, 2f)] public float rotateXSpeed = 1f;
    [Tooltip("Right mouse button control lens Y axis rotation speed")]
    [Range(0.1f, 2f)] public float rotateYSpeed = 1f;
    [Tooltip("Mouse wheel press, lens translation speed")]
    [Range(0.1f, 2f)] public float moveSpeed = 1f;
    [Tooltip("The keyboard controls how fast the camera moves")]
    [Range(0.1f, 15f)] public float keyMoveSpeed = 4f;

    //Whether the lens control operation is performed
    public bool operate = true;

    //Whether keyboard control lens operation is performed
    public bool isKeyOperate = true;

    //Whether currently in rotation
    private bool isRotate = false;

    //Is currently in panning
    private bool isMove = false;

    //Camera transform component cache
    private Transform m_transform;

    //The initial position of the camera at the beginning of the operation
    private Vector3 traStart;

    //The initial position of the mouse as the camera begins to operate
    private Vector3 mouseStart;

    //Is the camera facing down
    private bool isDown = false;

    private bool zoom = false;


    public GameObject target;
    public bool attachToTarget = false;

    public GameObject controlObject;
    public bool attachToControlObject = false;
    public bool lookAtControlObjectForward = true;
    public bool lookAtControlObjectCenter = false;
    public bool useOffset = true;

    public GameObject antiAircraft;
    public bool attachToAntiAircraft = true;
    public bool lookAtAntiAircraftForward = true;

    public bool lookAtTarget = false;

    public AnimationCurve fieldOfViewAnimationCurve;
    public AnimationCurve shiftAnimationCurve;

    Vector3 q;
    Vector3 w;
    Vector3 e;
    Vector3 a;
    Vector3 s;
    Vector3 d;
    public Vector3 offsetPosition = new Vector3(0, 0, 0);

    private float minFov = 1f;
    private float maxFov = 60f;
    float fov;
    public float rotateSpeedCoeff;

    float xCamRotate = 0.0f;
    float yCamRotate = 0.0f;

    public float dst = 0;

    public Vector3 correctOnParallax;

    // Start is called before the first frame update
    void Start()
    {
        fov = maxFov;
        m_transform = transform;
        Screen.fullScreen = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (operate)
        {
            if (Input.GetMouseButtonDown(1))
            {
                // Выполнять код, если правая кнопка мыши была только что нажата
                zoom = !zoom;
                if (zoom)
                {
                    fov = Mathf.Clamp(30, minFov, maxFov);
                } else
                {
                    fov = maxFov;
                }
                Camera.main.fieldOfView = fov;
            }
            //When in the rotation state, and the right mouse button is released, then exit the rotation state
            if (isRotate && Input.GetMouseButtonUp(1))
            {
                isRotate = false;
            }
            //When it is in the translation state, and the mouse wheel is released, it will exit the translation state
            if (isMove && Input.GetMouseButtonUp(2))
            {
                isMove = false;
            }

            //Whether it's in a rotational state
            if (isRotate)
            {
                //Gets the offset of the mouse on the screen
                Vector3 offset = Input.mousePosition - mouseStart;
                rotateSpeedCoeff = Shared.LineaRInterpolate(maxFov, 1f, minFov, 0.05f, Camera.main.fieldOfView);

                // whether the lens is facing down
                if (isDown)
                {
                    // the final rotation Angle = initial Angle + offset, 0.3f coefficient makes the rotation speed normal when rotateYSpeed, rotateXSpeed is 1
                    m_transform.rotation = Quaternion.Euler(traStart + new Vector3(offset.y * 0.3f * rotateYSpeed * rotateSpeedCoeff, -offset.x * 0.3f * rotateXSpeed * rotateSpeedCoeff, 0));
                }
                else
                {
                    // final rotation Angle = initial Angle + offset
                    m_transform.rotation = Quaternion.Euler(traStart + new Vector3(-offset.y * 0.3f * rotateYSpeed * rotateSpeedCoeff, offset.x * 0.3f * rotateXSpeed * rotateSpeedCoeff, 0));
                }
            }
            // press the right mouse button to enter the rotation state
            else if (Input.GetMouseButtonDown(1))
            {
                // enter the rotation state
                isRotate = true;
                // record the initial position of the mouse in order to calculate the offset
                mouseStart = Input.mousePosition;
                // record the initial mouse Angle
                traStart = m_transform.rotation.eulerAngles;
                // to determine whether the lens is facing down (the Y-axis is <0 according to the position of the object facing up),-0.0001f is a special case when x rotates 90
                isDown = m_transform.up.y < -0.0001f ? true : false;
            }

            // whether it is in the translation state
            if (isMove)
            {
                // mouse offset on the screen
                Vector3 offset = Input.mousePosition - mouseStart;
                // final position = initial position + offset
                m_transform.position = traStart + m_transform.up * -offset.y * 0.1f * moveSpeed + m_transform.right * -offset.x * 0.1f * moveSpeed;
            }
            // click the mouse wheel to enter translation mode
            else if (Input.GetMouseButtonDown(2))
            {
                // translation begins
                isMove = true;
                // record the initial position of the mouse
                mouseStart = Input.mousePosition;
                // record the initial position of the camera
                traStart = m_transform.position;
            }

            // how much did the roller roll
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            // scroll to scroll or not
            if (scroll != 0)
            {
                // position = current position + scroll amount
                /*m_transform.position += m_transform.forward * scroll * 1000f * Time.deltaTime * scrollSpeed;*/
                fov = Camera.main.fieldOfView;
                fov += Input.GetAxis("Mouse ScrollWheel") * -20.0f;
                fov = Mathf.Clamp(fov, minFov, maxFov);
                Camera.main.fieldOfView = fov;
            }

            // simulate the unity editor operation: right click, the keyboard can control the lens movement
            if (isKeyOperate)
            {
                float speed = keyMoveSpeed;
                // press LeftShift to make speed *2
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    speed = 3f * speed;
                }

                if (Input.GetKey(KeyCode.RightShift))
                {
                    speed = 0.1f * speed;
                }

                Vector3 deltaPosition = new Vector3(0, 0, 0);

                // press W on the keyboard to move the camera forward
                if (Input.GetKey(KeyCode.W))
                {
                    w = m_transform.forward * Time.deltaTime * 10f * speed;
                    deltaPosition += w;
                }
                // press the S key on the keyboard to back up the camera
                if (Input.GetKey(KeyCode.S))
                {
                    s = m_transform.forward * Time.deltaTime * 10f * speed;
                    deltaPosition -= s;
                }
                // press A on the keyboard and the camera will turn left
                if (Input.GetKey(KeyCode.A))
                {
                    a = m_transform.right * Time.deltaTime * 10f * speed;
                    deltaPosition -= a;
                }
                // press D on the keyboard to turn the camera to the right
                if (Input.GetKey(KeyCode.D))
                {
                    d = m_transform.right * Time.deltaTime * 10f * speed;
                    deltaPosition += d;
                }
                if (Input.GetKey(KeyCode.E))
                {
                    e = m_transform.up * Time.deltaTime * 10f * speed;
                    deltaPosition += e;
                }
                if (Input.GetKey(KeyCode.Q))
                {
                    q = m_transform.up * Time.deltaTime * 10f * speed;
                    deltaPosition -= q;
                }

                //-------------------------------------------------------------------------------------------------

                customCamLogic(deltaPosition);
            }
        }
    }

    void customCamLogic(Vector3 deltaPosition)
    {
        correctOnParallax = Vector3.zero;
        float fwd = 25f;
        float up = 8f;
        Vector3 camOffset;

        try
        {
            camOffset = controlObject.transform.forward * fwd - Vector3.up * up; // if rocket destroy
        }
        catch
        {
            return;
        }

        if (Input.GetMouseButton(1))
        {
            offsetPosition += deltaPosition;
        }

        if (attachToTarget == true)
        {
            m_transform.position = target.transform.position + offsetPosition;
        }
        else if (attachToControlObject == true)
        {
            Vector3 directionCircleInWorldPosition = controlObject.GetComponent<controlObject>().directionCircleInWorldPosition;
            
            if (useOffset)
                m_transform.position = controlObject.transform.position + offsetPosition;
            else
            {
                camOffset = (directionCircleInWorldPosition - controlObject.transform.position).normalized * fwd - Vector3.up * up;
                m_transform.position = controlObject.transform.position - camOffset;
            }

            Camera.main.fieldOfView = fov;
            GameObject target = controlObject.GetComponent<controlObject>().target;
            if (Input.GetMouseButton(2) && target)
            {
                Vector3 direction = target.transform.position - controlObject.transform.position;
                Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
                m_transform.position = (controlObject.transform.position - direction) + perpendicular * direction.magnitude * shiftAnimationCurve.Evaluate(direction.magnitude);
                m_transform.LookAt((target.transform.position + controlObject.transform.position) / 2);
                Camera.main.fieldOfView = fieldOfViewAnimationCurve.Evaluate(direction.magnitude);
            }
            else if (lookAtControlObjectForward && !Input.GetKey(KeyCode.Space))
            {
                m_transform.LookAt(directionCircleInWorldPosition);
            }
            else if (lookAtControlObjectCenter && !Input.GetKey(KeyCode.Space))
            {
                m_transform.LookAt(controlObject.transform.position);
            }
            else
            {
                rotateAroundControlObject(camOffset.magnitude);
            }
        }
        else if (attachToAntiAircraft == true)
        {
            Vector3 antiAircraftfwd = antiAircraft.transform.forward;
            Vector3 antiAircraftUp = antiAircraft.transform.up;
            camOffset = new Vector3(0, 4, 0);
            m_transform.position = antiAircraft.transform.position + camOffset;
            if (lookAtAntiAircraftForward)
            {
                m_transform.LookAt(antiAircraft.transform.forward * 1000000);
            }

            if (useOffset)
                m_transform.position = antiAircraft.transform.position + offsetPosition;
            else
                m_transform.position = antiAircraft.transform.position - camOffset;
        }
        else if (lookAtTarget == true)
        {
            m_transform.LookAt(target.transform.position);
        }
        else
        {
            m_transform.position += deltaPosition;
        }

        if (useOffset)
            correctOnParallax = offsetPosition;
        else
            correctOnParallax = -camOffset;

        dst = (transform.position - controlObject.transform.position).magnitude;
    }

    // look at controlObject center
    void rotateAroundControlObject(float distance)
    {
        float rotationSpeed = 2.0f;

        if (Input.GetButtonDown("Space"))
        {
            xCamRotate = transform.eulerAngles.y;
            yCamRotate = transform.eulerAngles.x;
        }

        xCamRotate += Input.GetAxis("Mouse X") * rotationSpeed;
        yCamRotate -= Input.GetAxis("Mouse Y") * rotationSpeed;

        Quaternion rotation = Quaternion.Euler(yCamRotate, xCamRotate, 0);

        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
        Vector3 position = rotation * negDistance + controlObject.transform.position;

        transform.rotation = rotation;
        transform.position = position;
    }
}


