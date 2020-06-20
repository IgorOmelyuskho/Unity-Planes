

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOperate : MonoBehaviour
{
    [Tooltip("Mouse wheel rolling control lens please enter, the speed of the back")]
    [Range(0.5f, 2f)] public float scrollSpeed = 1f;
    [Tooltip("Right mouse button control lens X axis rotation speed")]
    [Range(0.5f, 2f)] public float rotateXSpeed = 1f;
    [Tooltip("Right mouse button control lens Y axis rotation speed")]
    [Range(0.5f, 2f)] public float rotateYSpeed = 1f;
    [Tooltip("Mouse wheel press, lens translation speed")]
    [Range(0.5f, 2f)] public float moveSpeed = 1f;
    [Tooltip("The keyboard controls how fast the camera moves")]
    [Range(0.5f, 15f)] public float keyMoveSpeed = 4f;

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


    public GameObject target;
    public bool attachToTarget = false;

    public GameObject controlObject;
    public bool attachToControlObject = false;
    public bool lookAtControlObjectForward = true;
    public bool lookAtControlObjectCenter = false;
    public bool controlObjectOffset = true;

    public GameObject antiAircraft;
    public bool attachToAntiAircraft = true;
    public bool lookAtAntiAircraftForward = true;

    Vector3 q;
    Vector3 w;
    Vector3 e;
    Vector3 a;
    Vector3 s;
    Vector3 d;
    public Vector3 offsetPosition = new Vector3(0, 0, 0);

    private float minFov = 1f;
    private float maxFov = 60f;
    public float rotateSpeedCoeff;

    float xCamRotate = 0.0f;
    float yCamRotate = 0.0f;


    public float dst = 0;

    // Start is called before the first frame update
    void Start()
    {
        m_transform = transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (operate)
        {
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
                float fov = Camera.main.fieldOfView;
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
        float fwd = 8f;
        float up = 1.5f;
        Vector3 camOffset = controlObject.transform.forward * fwd - Vector3.up * up;

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
            if (controlObjectOffset)
            {
                m_transform.position = controlObject.transform.position + offsetPosition;
            }
            else
            {
                m_transform.position = controlObject.transform.position - camOffset;
            }

            if (lookAtControlObjectForward && !Input.GetKey(KeyCode.Space))
            {
                m_transform.LookAt(controlObject.transform.forward * 1000000);
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
            m_transform.position = antiAircraft.transform.position + new Vector3(0, 1, 0);
            if (lookAtAntiAircraftForward)
            {
                m_transform.LookAt(antiAircraft.transform.forward * 1000000);
            }
        }
        else
        {
            m_transform.position += deltaPosition;
        }

        dst = (transform.position - controlObject.transform.position).magnitude;
    }

    // look at controlObject center
    void rotateAroundControlObject(float distance)
    {
        float rotationSpeed = 120.0f;

        if (Input.GetButtonDown("Space"))
        {
            xCamRotate = transform.eulerAngles.y;
            yCamRotate = transform.eulerAngles.x;
        }

        xCamRotate += Input.GetAxis("Mouse X") * rotationSpeed * 0.1f;
        yCamRotate -= Input.GetAxis("Mouse Y") * rotationSpeed * 0.1f;

        Quaternion rotation = Quaternion.Euler(yCamRotate, xCamRotate, 0);

        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
        Vector3 position = rotation * negDistance + controlObject.transform.position;

        transform.rotation = rotation;
        transform.position = position;
    }
}


